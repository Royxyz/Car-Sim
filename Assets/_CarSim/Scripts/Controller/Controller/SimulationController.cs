using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(InputManager))]
public class SimulationController : MonoBehaviour
{
    [Header("Core Components")]
    public Rigidbody rb;
    private InputManager inputManager;

    [Header("Vehicle Systems")]
    public PowerTrain powerTrain;
    public AutoController autoController;
    public Drivetrain drivetrain;
    public Aerodynamics aerodynamics;

    [Header("Corners (0:FL, 1:FR, 2:RL, 3:RR)")]
    public WheelAssembly[] corners = new WheelAssembly[4];

    [Header("Simulation Settings")]
    [Range(1, 30)]
    public int subSteps = 15;
    public LayerMask trackMask = ~0; 

    private struct VirtualChassis
    {
        public Vector3 position;       
        public Quaternion rotation;    
        public Vector3 linearVelocity; 
        public Vector3 angularVelocity;
    }

    private VirtualChassis vChassis;
    private Vector3[] localMountPositions = new Vector3[4];
    private Vector3[] localMountUps = new Vector3[4];

    private Vector3 totalAccumulatedForce;
    private Vector3 totalAccumulatedTorque;

    private struct CornerState
    {
        public bool isGrounded;
        public float hitDistance;
        public Vector3 contactPoint;
        public Vector3 contactNormal;
    }
    
    private CornerState[] cornerStates = new CornerState[4];
    private RaycastHit[] hitBuffer = new RaycastHit[10];

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputManager = GetComponent<InputManager>();

        rb.centerOfMass = new Vector3(0f, -0.4f, 0f); 

        if (rb.inertiaTensor.magnitude < 10f) 
        {
            float mass = rb.mass > 100f ? rb.mass : 1500f;
            Vector3 carSize = new Vector3(2.8f, 1.0f, 4.5f); 
            rb.inertiaTensor = new Vector3(
                (1f/12f) * mass * (carSize.y * carSize.y + carSize.z * carSize.z), 
                (1f/12f) * mass * (carSize.x * carSize.x + carSize.z * carSize.z), 
                (1f/12f) * mass * (carSize.x * carSize.x + carSize.y * carSize.y)  
            );
            rb.inertiaTensorRotation = Quaternion.identity;
        }

        InitializeSystems();
    }

    private void InitializeSystems()
    {
        powerTrain.Initialize();
        autoController.Initialize(powerTrain, autoController.logicData);

        for (int i = 0; i < 4; i++)
        {
            if (corners[i] != null) 
            {
                corners[i].Initialize();
                localMountPositions[i] = transform.InverseTransformPoint(corners[i].suspensionMountPoint.position) - rb.centerOfMass;
                localMountUps[i] = transform.InverseTransformDirection(corners[i].suspensionMountPoint.up);
            }
        }
    }

    private void FixedUpdate()
    {
        if (corners.Length != 4) return;

        float frameDt = Time.fixedDeltaTime;
        float subDt = frameDt / subSteps;

        HandleDriverInputs();

        vChassis.position = rb.position + rb.rotation * rb.centerOfMass;
        vChassis.rotation = rb.rotation;
        vChassis.linearVelocity = rb.linearVelocity;
        vChassis.angularVelocity = rb.angularVelocity;

        totalAccumulatedForce = Vector3.zero;
        totalAccumulatedTorque = Vector3.zero;

        for (int step = 0; step < subSteps; step++)
        {
            RunPhysicsSubStep(subDt);
        }

        rb.AddForce(totalAccumulatedForce / subSteps, ForceMode.Force);
        rb.AddTorque(totalAccumulatedTorque / subSteps, ForceMode.Force);
        
        ApplyAerodynamics();
    }

    private void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (corners[i] != null) corners[i].UpdateVisuals();
        }
    }

    private void HandleDriverInputs()
    {
        if (inputManager.shiftUpTriggered) powerTrain.transmission.ShiftUp();
        if (inputManager.shiftDownTriggered) powerTrain.transmission.ShiftDown();

        float baseSteer = inputManager.steeringInput * 35f; 
        for (int i = 0; i < 4; i++)
        {
            if (corners[i] != null && corners[i].isSteerable)
            {
                float ackermannModifier = (Mathf.Sign(baseSteer) == (i % 2 == 0 ? -1 : 1)) ? 1.15f : 0.85f;
                corners[i].ackermannSteeringAngle = baseSteer * ackermannModifier;
            }
        }
    }

    private void RunPhysicsSubStep(float dt)
    {
        autoController.UpdateController(dt);

        Vector3 stepTotalForce = Vector3.zero;
        Vector3 stepTotalTorque = Vector3.zero;

        float totalLoadTorque = 0f;
        for(int i = 0; i < 4; i++) 
        {
            totalLoadTorque += Mathf.Abs(corners[i].tire.CalculateGripForces(corners[i].suspension.currentNormalLoad, corners[i].wheel.longitudinalSlip, corners[i].wheel.slipAngle).x) * corners[i].wheel.wheelData.radius;
        }
        
        float reflectedLoad = drivetrain.GetTotalReflectedLoad(totalLoadTorque/4f, totalLoadTorque/4f, totalLoadTorque/4f, totalLoadTorque/4f);
        float reflectedInertia = drivetrain.GetTotalReflectedInertia(corners[0].wheel.wheelData.inertia, corners[1].wheel.wheelData.inertia, corners[2].wheel.wheelData.inertia, corners[3].wheel.wheelData.inertia);
        

        float transOutputRadSec = drivetrain.CalculateInputSpeed(
            corners[0].wheel.angularVelocity,
            corners[1].wheel.angularVelocity,
            corners[2].wheel.angularVelocity,
            corners[3].wheel.angularVelocity
        );


        float transOutputRPM = transOutputRadSec * (30f / Mathf.PI);
        float actualTransRPM = transOutputRPM * powerTrain.transmission.GetTotalRatio();


        powerTrain.UpdatePhysics(inputManager.throttleInput, actualTransRPM, reflectedLoad, reflectedInertia, dt);


        float transOutputTorque = powerTrain.GetWheelTorque();
        float[] wheelDriveTorques = drivetrain.RouteTorque(transOutputTorque, corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity, corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity);

        for (int i = 0; i < 4; i++)
        {
            Vector3 virtualMountWorldPos = vChassis.position + (vChassis.rotation * localMountPositions[i]);
            Vector3 virtualMountUp = vChassis.rotation * localMountUps[i];
            
            Vector3 radiusFromCoM = virtualMountWorldPos - vChassis.position;
            Vector3 virtualPointVel = vChassis.linearVelocity + Vector3.Cross(vChassis.angularVelocity, radiusFromCoM);

            ProcessCornerPhysics(i, virtualMountWorldPos, virtualMountUp, virtualPointVel, wheelDriveTorques[i], dt, ref stepTotalForce, ref stepTotalTorque, radiusFromCoM);
        }

        // VIRTUAL ANTI-ROLL BARS
        float arbFrontStiffness = 15000f; 
        float arbRearStiffness  = 12000f;

        float compFL = cornerStates[0].isGrounded ? corners[0].suspension.suspData.restLength - cornerStates[0].hitDistance : 0f;
        float compFR = cornerStates[1].isGrounded ? corners[1].suspension.suspData.restLength - cornerStates[1].hitDistance : 0f;
        float compRL = cornerStates[2].isGrounded ? corners[2].suspension.suspData.restLength - cornerStates[2].hitDistance : 0f;
        float compRR = cornerStates[3].isGrounded ? corners[3].suspension.suspData.restLength - cornerStates[3].hitDistance : 0f;

        float arbForceFront = (compFL - compFR) * arbFrontStiffness;
        float arbForceRear  = (compRL - compRR) * arbRearStiffness;

        Vector3 arbFL = (vChassis.rotation * localMountUps[0]) * arbForceFront;
        Vector3 arbFR = (vChassis.rotation * localMountUps[1]) * -arbForceFront;
        Vector3 arbRL = (vChassis.rotation * localMountUps[2]) * arbForceRear;
        Vector3 arbRR = (vChassis.rotation * localMountUps[3]) * -arbForceRear;

        stepTotalForce += (arbFL + arbFR + arbRL + arbRR);
        stepTotalTorque += Vector3.Cross(vChassis.rotation * localMountPositions[0], arbFL);
        stepTotalTorque += Vector3.Cross(vChassis.rotation * localMountPositions[1], arbFR);
        stepTotalTorque += Vector3.Cross(vChassis.rotation * localMountPositions[2], arbRL);
        stepTotalTorque += Vector3.Cross(vChassis.rotation * localMountPositions[3], arbRR);

        // Integrate Virtual Chassis 
        Vector3 totalVirtualForce = stepTotalForce + (Physics.gravity * rb.mass);
        vChassis.linearVelocity += (totalVirtualForce / rb.mass) * dt;
        vChassis.linearVelocity *= (1.0f - (0.5f * dt)); 
        vChassis.position += vChassis.linearVelocity * dt;

        // [THE FIX]: THE TORQUE CLAMP
        // This explicitly forbids the physics engine from instantly cartwheeling the car, 
        // no matter how catastrophically the car drops onto its suspension.
        float maxSafeTorque = rb.mass * 100f; 
        if (stepTotalTorque.magnitude > maxSafeTorque)
        {
            stepTotalTorque = stepTotalTorque.normalized * maxSafeTorque;
        }

        Vector3 localTorque = Quaternion.Inverse(vChassis.rotation) * stepTotalTorque;
        Vector3 localAngAccel = new Vector3(
            localTorque.x / rb.inertiaTensor.x,
            localTorque.y / rb.inertiaTensor.y,
            localTorque.z / rb.inertiaTensor.z
        );
        
        vChassis.angularVelocity += (vChassis.rotation * localAngAccel) * dt;
        vChassis.angularVelocity *= (1.0f - (3.0f * dt)); 

        Quaternion qVel = new Quaternion(vChassis.angularVelocity.x, vChassis.angularVelocity.y, vChassis.angularVelocity.z, 0f) * vChassis.rotation;
        vChassis.rotation.x += 0.5f * qVel.x * dt;
        vChassis.rotation.y += 0.5f * qVel.y * dt;
        vChassis.rotation.z += 0.5f * qVel.z * dt;
        vChassis.rotation.w += 0.5f * qVel.w * dt;
        vChassis.rotation.Normalize();

        totalAccumulatedForce += stepTotalForce;
        totalAccumulatedTorque += stepTotalTorque;
    }

    private void ProcessCornerPhysics(int index, Vector3 mountPos, Vector3 mountUp, Vector3 mountVel, float driveTorque, float dt, ref Vector3 stepForce, ref Vector3 stepTorque, Vector3 radiusFromCoM)
    {
        WheelAssembly corner = corners[index];
        float rayOffset = 1.0f; 
        Vector3 rayStartPos = mountPos + (mountUp * rayOffset);
        float maxRayLength = corner.suspension.suspData.restLength + corner.suspension.suspData.maxTravel + corner.wheel.wheelData.radius + rayOffset;

        int hitCount = Physics.RaycastNonAlloc(rayStartPos, -mountUp, hitBuffer, maxRayLength, trackMask);
        bool foundValidHit = false;
        RaycastHit validHit = default;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].collider.transform.root != this.transform.root)
            {
                if (hitBuffer[i].distance < closestDistance)
                {
                    closestDistance = hitBuffer[i].distance;
                    validHit = hitBuffer[i];
                    foundValidHit = true;
                }
            }
        }

        if (foundValidHit)
        {
            cornerStates[index].isGrounded = true;
            cornerStates[index].hitDistance = validHit.distance - corner.wheel.wheelData.radius - rayOffset; 
            cornerStates[index].contactPoint = validHit.point;
            cornerStates[index].contactNormal = validHit.normal;
        }
        else
        {
            cornerStates[index].isGrounded = false;
            cornerStates[index].hitDistance = corner.suspension.suspData.restLength + corner.suspension.suspData.maxTravel;
            cornerStates[index].contactPoint = mountPos - (mountUp * (maxRayLength - rayOffset));
            cornerStates[index].contactNormal = Vector3.up;
        }

        CornerState state = cornerStates[index];
        float compressionVelocity = Vector3.Dot(mountVel, -mountUp);

        float suspForceMagnitude = corner.suspension.CalculateForce(state.isGrounded, state.hitDistance, compressionVelocity);
        Vector3 suspensionForceWorld = mountUp * suspForceMagnitude;
        Vector3 gripForceWorld = Vector3.zero; 

        if (state.isGrounded)
        {
            Vector3 contactRadius = state.contactPoint - vChassis.position;
            Vector3 contactVelWorld = vChassis.linearVelocity + Vector3.Cross(vChassis.angularVelocity, contactRadius);
            
            Quaternion wheelRot = vChassis.rotation * Quaternion.Euler(0, corner.ackermannSteeringAngle, 0);
            
            Vector3 contactVelPlanar = Vector3.ProjectOnPlane(contactVelWorld, state.contactNormal);
            Vector3 contactVelLocal = Quaternion.Inverse(wheelRot) * contactVelPlanar;

            corner.wheel.CalculateSlips(contactVelLocal);
            float brakeTorque = corner.brake.CalculateBrakeTorque(inputManager.brakeInput, corner.wheel.longitudinalSlip);
            
            float maxFrictionLoad = (rb.mass * 9.81f); 
            float effectiveFrictionLoad = Mathf.Clamp(suspForceMagnitude, 0f, maxFrictionLoad);
            
            Vector2 gripForceLocal = corner.tire.CalculateGripForces(effectiveFrictionLoad, corner.wheel.longitudinalSlip, corner.wheel.slipAngle);
            
            Vector3 gripDirLong = Vector3.ProjectOnPlane(wheelRot * Vector3.forward, state.contactNormal).normalized;
            Vector3 gripDirLat = Vector3.ProjectOnPlane(wheelRot * Vector3.right, state.contactNormal).normalized;
            
            gripForceWorld = (gripDirLong * gripForceLocal.x) + (gripDirLat * gripForceLocal.y); 

            float tireGripTorque = gripForceLocal.x * corner.wheel.wheelData.radius; 
            corner.wheel.UpdatePhysics(driveTorque, brakeTorque, tireGripTorque, dt);
        }
        else
        {
            corner.wheel.UpdatePhysics(driveTorque, corner.brake.CalculateBrakeTorque(inputManager.brakeInput, 0f), 0f, dt);
        }

        Vector3 totalCornerForce = suspensionForceWorld + gripForceWorld;
        stepForce += totalCornerForce;
        
        Vector3 suspTorque = Vector3.Cross(radiusFromCoM, suspensionForceWorld);
        Vector3 gripTorque = Vector3.Cross(state.contactPoint - vChassis.position, gripForceWorld);
        
        stepTorque += (suspTorque + gripTorque);
    }

    private void ApplyAerodynamics()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 aeroForcesLocal = aerodynamics.CalculateAerodynamicForces(localVelocity);
        Vector3 aeroForcesWorld = transform.TransformDirection(aeroForcesLocal);
        rb.AddForce(aeroForcesWorld);
    }
}