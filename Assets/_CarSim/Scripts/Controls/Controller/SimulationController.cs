using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimulationController : MonoBehaviour
{
    [Header("Core Components")]
    public Rigidbody rb;
    private IVehicleInput vehicleInput; 

    [Header("Vehicle Systems")]
    public ChassisData chassisData;    
    public SteeringData steeringData;  
    public AntiRollBar antiRollBar = new AntiRollBar(); 

    [Header("Powertrain")]
    public PowerTrain powerTrain;
    public AutoController autoController;
    public Drivetrain drivetrain;
    public Aerodynamics aerodynamics;
    
    [Tooltip("If false, player must use the Clutch axis and Shift buttons manually.")]
    public bool isAutomatic = true;

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        vehicleInput = GetComponent<IVehicleInput>();

        if (vehicleInput == null)
        {
            Debug.LogError("No IVehicleInput found on the car! Please attach InputManager or an AI Controller.");
        }

        if (chassisData != null)
        {
            rb.mass = chassisData.totalMass;
            rb.centerOfMass = chassisData.centerOfMassOffset; 

            if (rb.inertiaTensor.magnitude < 10f) 
            {
                Vector3 size = chassisData.inertiaTensorBoxSize; 
                rb.inertiaTensor = new Vector3(
                    (1f/12f) * rb.mass * (size.y * size.y + size.z * size.z), 
                    (1f/12f) * rb.mass * (size.x * size.x + size.z * size.z), 
                    (1f/12f) * rb.mass * (size.x * size.x + size.y * size.y)  
                );
                rb.inertiaTensorRotation = Quaternion.identity;
            }
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
        if (corners.Length != 4 || vehicleInput == null) return;

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

        antiRollBar.ApplyAntiRollBars(corners, rb); 
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
        if (vehicleInput.ShiftUp) powerTrain.transmission.ShiftUp();
        if (vehicleInput.ShiftDown) powerTrain.transmission.ShiftDown();

        float baseSteer = vehicleInput.Steering * steeringData.maxSteerAngle; 
        for (int i = 0; i < 4; i++)
        {
            if (corners[i] != null && corners[i].isSteerable)
            {
                float ackermannModifier = (Mathf.Sign(baseSteer) == (i % 2 == 0 ? -1 : 1)) 
                    ? steeringData.ackermannInnerMultiplier 
                    : steeringData.ackermannOuterMultiplier;
                    
                corners[i].ackermannSteeringAngle = baseSteer * ackermannModifier;
            }
        }
    }

    private void RunPhysicsSubStep(float dt)
    {
        if (isAutomatic)
        {
            autoController.UpdateController(dt);
        }
        else
        {
            powerTrain.clutch.engagement = Mathf.Clamp01(1f - vehicleInput.Clutch);
        }

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
            corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity,
            corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity
        );

        float transOutputRPM = transOutputRadSec * (30f / Mathf.PI);
        float actualTransRPM = transOutputRPM * powerTrain.transmission.GetTotalRatio();

        powerTrain.UpdatePhysics(vehicleInput.Throttle, actualTransRPM, reflectedLoad, reflectedInertia, dt);

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

        Vector3 totalVirtualForce = stepTotalForce + (Physics.gravity * rb.mass);
        vChassis.linearVelocity += (totalVirtualForce / rb.mass) * dt;
        vChassis.position += vChassis.linearVelocity * dt;

        float maxSafeTorque = rb.mass * chassisData.maxSafeTorqueMultiplier; 
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

        corner.contact.EvaluateContact(
            this.transform.root, mountPos, mountUp, 
            corner.suspension.suspData.restLength, 
            corner.suspension.suspData.maxTravel, 
            corner.wheel.wheelData.radius, 
            trackMask
        );

        float compressionVelocity = Vector3.Dot(mountVel, -mountUp);

        float suspForceMagnitude = corner.suspension.CalculateForce(corner.contact.isGrounded, corner.contact.hitDistance, compressionVelocity);
        Vector3 suspensionForceWorld = mountUp * suspForceMagnitude;
        Vector3 gripForceWorld = Vector3.zero; 

        if (corner.contact.isGrounded)
        {
            Vector3 contactRadius = corner.contact.contactPoint - vChassis.position;
            Vector3 contactVelWorld = vChassis.linearVelocity + Vector3.Cross(vChassis.angularVelocity, contactRadius);
            
            Quaternion wheelRot = vChassis.rotation * Quaternion.Euler(0, corner.ackermannSteeringAngle, 0);
            
            Vector3 contactVelPlanar = Vector3.ProjectOnPlane(contactVelWorld, corner.contact.contactNormal);
            Vector3 contactVelLocal = Quaternion.Inverse(wheelRot) * contactVelPlanar;

            corner.wheel.CalculateSlips(contactVelLocal);
            float brakeTorque = corner.brake.CalculateBrakeTorque(vehicleInput.Brake, corner.wheel.longitudinalSlip);
            
            float effectiveFrictionLoad = Mathf.Clamp(suspForceMagnitude, 0f, (rb.mass * 9.81f));
            
            Vector2 gripForceLocal = corner.tire.CalculateGripForces(effectiveFrictionLoad, corner.wheel.longitudinalSlip, corner.wheel.slipAngle);
            
            Vector3 gripDirLong = Vector3.ProjectOnPlane(wheelRot * Vector3.forward, corner.contact.contactNormal).normalized;
            Vector3 gripDirLat = Vector3.ProjectOnPlane(wheelRot * Vector3.right, corner.contact.contactNormal).normalized;
            
            gripForceWorld = (gripDirLong * gripForceLocal.x) + (gripDirLat * gripForceLocal.y); 

            float tireGripTorque = gripForceLocal.x * corner.wheel.wheelData.radius; 
            corner.wheel.UpdatePhysics(driveTorque, brakeTorque, tireGripTorque, dt);
        }
        else
        {
            corner.wheel.UpdatePhysics(driveTorque, corner.brake.CalculateBrakeTorque(vehicleInput.Brake, 0f), 0f, dt);
        }

        Vector3 totalCornerForce = suspensionForceWorld + gripForceWorld;
        stepForce += totalCornerForce;
        
        Vector3 suspTorque = Vector3.Cross(radiusFromCoM, suspensionForceWorld);
        Vector3 gripTorque = Vector3.Cross(corner.contact.contactPoint - vChassis.position, gripForceWorld);
        
        stepTorque += (suspTorque + gripTorque);
    }

    private void ApplyAerodynamics()
    {
        Vector3 aeroForcesLocal = aerodynamics.CalculateAerodynamicForces(rb.linearVelocity, transform);
        Vector3 aeroForcesWorld = transform.TransformDirection(aeroForcesLocal);
        Vector3 centerOfPressureWorld = transform.TransformPoint(aerodynamics.aeroData.centerOfPressureOffset);
        rb.AddForceAtPosition(aeroForcesWorld, centerOfPressureWorld);
    }
}