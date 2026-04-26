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
    public AntiRollBar antiRollBar;

    [Header("Corners (0:FL, 1:FR, 2:RL, 3:RR)")]
    public WheelAssembly[] corners = new WheelAssembly[4];

    [Header("Simulation Settings")]
    [Range(1, 30)]
    public int subSteps = 15;
    
    private struct CornerState
    {
        public bool isGrounded;
        public float hitDistance;
        public Vector3 contactPoint;
        public Vector3 contactNormal;
        public Vector3 accumulatedForce; 
        public Vector3 accumulatedSuspForce;
        public Vector3 accumulatedGripForce;
        
    }
    
    private CornerState[] cornerStates = new CornerState[4];

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        inputManager = GetComponent<InputManager>();

        // [NEW FIX] - Calculate a realistic Inertia Tensor if no colliders exist
        if (rb.inertiaTensor.magnitude < 10f) 
        {
            float mass = rb.mass > 100f ? rb.mass : 1500f;
            Vector3 carSize = new Vector3(1.8f, 1.2f, 4.5f); // Avg car width, height, length
            
            rb.inertiaTensor = new Vector3(
                (1f/12f) * mass * (carSize.y * carSize.y + carSize.z * carSize.z), // Pitch
                (1f/12f) * mass * (carSize.x * carSize.x + carSize.z * carSize.z), // Yaw
                (1f/12f) * mass * (carSize.x * carSize.x + carSize.y * carSize.y)  // Roll
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
            if (corners[i] != null) corners[i].Initialize();
        }
    }

    private void FixedUpdate()
    {
        if (corners.Length != 4) return;

        float frameDt = Time.fixedDeltaTime;
        float subDt = frameDt / subSteps;

        HandleDriverInputs();

        PerformRaycasts();

        antiRollBar.ApplyAntiRollBars(corners, rb);

        for (int i = 0; i < 4; i++) cornerStates[i].accumulatedForce = Vector3.zero;

        for (int step = 0; step < subSteps; step++)
        {
            RunPhysicsSubStep(subDt);
        }

        ApplyAccumulatedForces();
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

    private void PerformRaycasts()
    {
        for (int i = 0; i < 4; i++)
        {
            WheelAssembly corner = corners[i];
            Transform mount = corner.suspensionMountPoint;
            float maxRayLength = corner.suspension.suspData.restLength + corner.suspension.suspData.maxTravel + corner.wheel.wheelData.radius;

            if (Physics.Raycast(mount.position, -mount.up, out RaycastHit hit, maxRayLength))
            {
                cornerStates[i].isGrounded = true;
                cornerStates[i].hitDistance = hit.distance - corner.wheel.wheelData.radius; 
                cornerStates[i].contactPoint = hit.point;
                cornerStates[i].contactNormal = hit.normal;
            }
            else
            {
                cornerStates[i].isGrounded = false;
                cornerStates[i].hitDistance = corner.suspension.suspData.restLength + corner.suspension.suspData.maxTravel;
                cornerStates[i].contactPoint = mount.position - (mount.up * maxRayLength);
                cornerStates[i].contactNormal = Vector3.up;
            }
        }
    }

    private void RunPhysicsSubStep(float dt)
    {
        autoController.UpdateController(dt);

        float totalLoadTorque = 0f;
        for(int i = 0; i < 4; i++) 
        {
            totalLoadTorque += Mathf.Abs(corners[i].tire.CalculateGripForces(corners[i].suspension.currentNormalLoad, corners[i].wheel.longitudinalSlip, corners[i].wheel.slipAngle).x) * corners[i].wheel.wheelData.radius;
        }
        
        float reflectedLoad = drivetrain.GetTotalReflectedLoad(totalLoadTorque/4f, totalLoadTorque/4f, totalLoadTorque/4f, totalLoadTorque/4f);
        float reflectedInertia = drivetrain.GetTotalReflectedInertia(corners[0].wheel.wheelData.inertia, corners[1].wheel.wheelData.inertia, corners[2].wheel.wheelData.inertia, corners[3].wheel.wheelData.inertia);

        powerTrain.UpdatePhysics(inputManager.throttleInput, reflectedLoad, reflectedInertia, dt);

        float transOutputTorque = powerTrain.GetWheelTorque();
        float[] wheelDriveTorques = drivetrain.RouteTorque(
            transOutputTorque, 
            corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity, 
            corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity
        );

        for (int i = 0; i < 4; i++)
        {
            ProcessCornerPhysics(i, wheelDriveTorques[i], dt);
        }
    }

    private void ProcessCornerPhysics(int index, float driveTorque, float dt)
    {
        WheelAssembly corner = corners[index];
        CornerState state = cornerStates[index];

        // [NEW FIX] - Calculate True Compression Velocity
        Vector3 mountVelocity = rb.GetPointVelocity(corner.suspensionMountPoint.position);
        // Dot product: if the chassis point is moving DOWN towards the wheel, velocity is positive (Bump)
        float compressionVelocity = Vector3.Dot(mountVelocity, -corner.suspensionMountPoint.up);

        // A. Suspension
        float suspForceMagnitude = corner.suspension.CalculateForce(state.isGrounded, state.hitDistance, compressionVelocity);
        Vector3 suspensionForce = corner.suspensionMountPoint.up * suspForceMagnitude;

        Vector3 gripForceWorld = Vector3.zero; 

        if (state.isGrounded)
        {
            Vector3 contactVelWorld = rb.GetPointVelocity(state.contactPoint);
            
            Quaternion wheelRot = corner.suspensionMountPoint.rotation * Quaternion.Euler(0, corner.ackermannSteeringAngle, 0);
            Vector3 contactVelLocal = Quaternion.Inverse(wheelRot) * contactVelWorld;

            corner.wheel.CalculateSlips(contactVelLocal);
          
            float brakeTorque = corner.brake.CalculateBrakeTorque(inputManager.brakeInput, corner.wheel.longitudinalSlip);
            
            Vector2 gripForceLocal = corner.tire.CalculateGripForces(suspForceMagnitude, corner.wheel.longitudinalSlip, corner.wheel.slipAngle);
            
            
            Vector3 gripDirLong = wheelRot * Vector3.forward;
            Vector3 gripDirLat = wheelRot * Vector3.right;
            gripForceWorld = (gripDirLong * gripForceLocal.x) + (gripDirLat * gripForceLocal.y); 


            float tireGripTorque = gripForceLocal.x * corner.wheel.wheelData.radius; 
            corner.wheel.UpdatePhysics(driveTorque, brakeTorque, tireGripTorque, dt);
        }
        else
        {
            corner.wheel.UpdatePhysics(driveTorque, corner.brake.CalculateBrakeTorque(inputManager.brakeInput, 0f), 0f, dt);
        }

        cornerStates[index].accumulatedSuspForce += suspensionForce;
        cornerStates[index].accumulatedGripForce += gripForceWorld;
    }

    private void ApplyAccumulatedForces()
    {
        for (int i = 0; i < 4; i++)
        {
            Vector3 avgSusp = cornerStates[i].accumulatedSuspForce / subSteps;
            Vector3 avgGrip = cornerStates[i].accumulatedGripForce / subSteps;  
            
            if (avgGrip.magnitude > 0.01f && avgSusp.magnitude > 0.01f)
            {
                rb.AddForceAtPosition(avgSusp, corners[i].suspensionMountPoint.position);
                rb.AddForceAtPosition(avgGrip, cornerStates[i].contactPoint);
            }
        }
    }

    private void ApplyAerodynamics()
    {

        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 aeroForcesLocal = aerodynamics.CalculateAerodynamicForces(localVelocity);

        Vector3 aeroForcesWorld = transform.TransformDirection(aeroForcesLocal);
        rb.AddForce(aeroForcesWorld);
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || corners == null) return;

        for (int i = 0; i < 4; i++)
        {
            if (corners[i] == null || corners[i].suspensionMountPoint == null) continue;

            Transform mount = corners[i].suspensionMountPoint;
            CornerState state = cornerStates[i];

            Gizmos.color = state.isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(mount.position, state.contactPoint);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(state.contactPoint, state.contactNormal * 0.2f);

            if (state.isGrounded)
            {

                Quaternion wheelRot = mount.rotation * Quaternion.Euler(0, corners[i].ackermannSteeringAngle, 0);
                Vector3 forwardDir = wheelRot * Vector3.forward;
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(state.contactPoint, forwardDir * 0.5f);


                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(state.contactPoint, (state.accumulatedForce / subSteps) * 0.001f); 
            }
        }

        // Draw Center of Mass
        if (rb != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.TransformPoint(rb.centerOfMass), 0.1f);
        }
    }
}