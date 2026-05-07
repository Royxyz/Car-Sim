using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimulationController : MonoBehaviour
{
    [Header("Core Components")]
    public Rigidbody rb;
    private IVehicleInput vehicleInput;
    private Transform cachedRoot;

    [Header("Sub-Systems")]
    public VirtualDynamics vDynamics = new VirtualDynamics();
    public ChassisManager chassis = new ChassisManager();
    public PowertrainManager powertrain = new PowertrainManager();
    public Aerodynamics aerodynamics;

    [Header("Simulation Settings")]
    [Range(1, 30)] public int subSteps = 15;
    public bool isAutomatic = true;
    public bool useSmartReverseAssist = false;
    public PowerTrain powerTrain => powertrain.powerTrain;
    public Drivetrain drivetrain => powertrain.drivetrain;
    public AutoController autoController => powertrain.autoController;
    public WheelAssembly[] corners => chassis.corners;
    public ChassisData chassisData => chassis.chassisData;
    public SteeringData steeringData => chassis.steeringData;
    public AntiRollBar antiRollBar => chassis.antiRollBar;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        cachedRoot = transform.root;
        vehicleInput = GetComponent<IVehicleInput>();

        if (chassis.chassisData != null)
        {
            rb.mass = chassis.chassisData.totalMass;
            rb.centerOfMass = chassis.chassisData.centerOfMassOffset;

            if (rb.inertiaTensor.magnitude < 10f)
            {
                Vector3 size = chassis.chassisData.inertiaTensorBoxSize;
                rb.inertiaTensor = new Vector3(
                    (1f / 12f) * rb.mass * (size.y * size.y + size.z * size.z),
                    (1f / 12f) * rb.mass * (size.x * size.x + size.z * size.z),
                    (1f / 12f) * rb.mass * (size.x * size.x + size.y * size.y)
                );
                rb.inertiaTensorRotation = Quaternion.identity;
            }
        }

        chassis.Initialize(rb, transform);
        powertrain.Initialize();
    }

    private void FixedUpdate()
    {
        if (chassis.corners.Length != 4 || vehicleInput == null) return;

        float frameDt = Time.fixedDeltaTime;
        float subDt = frameDt / subSteps;

        HandleTransmissionInputs();
        float activeThrottle = vehicleInput.Throttle;
        float activeBrake = vehicleInput.Brake;
        float activeHandbrake = vehicleInput.Handbrake;
        float targetSteer = vehicleInput.Steering * chassis.steeringData.maxSteerAngle;

        if (isAutomatic && powertrain.powerTrain.transmission.currentGear == -1 && useSmartReverseAssist)
        {
            activeThrottle = vehicleInput.Brake;
            activeBrake = vehicleInput.Throttle;
        }

        if (!isAutomatic) powertrain.powerTrain.clutch.engagement = Mathf.Clamp01(1f - vehicleInput.Clutch);

        vDynamics.SyncFromRigidbody(rb);

        for (int step = 0; step < subSteps; step++)
        {
            vDynamics.ResetStepAccumulators();
            float stepFraction = (step + 1f) / subSteps;

            float[] driveTorques = powertrain.ProcessTorqueRouting(chassis.corners, activeThrottle, activeBrake, isAutomatic, subDt);
            
            chassis.ProcessSubStep(vDynamics, cachedRoot, targetSteer, stepFraction, driveTorques, activeBrake, activeHandbrake, subDt);

            float maxSafeTorque = rb.mass * chassis.chassisData.maxSafeTorqueMultiplier;
            vDynamics.IntegrateStep(subDt, rb, maxSafeTorque);
        }

        vDynamics.SyncToRigidbody(rb);

        chassis.UpdateSteeringInterpolation(targetSteer);
        

        ApplyAerodynamics();
    }

    private void Update()
    {
        for (int i = 0; i < 4; i++)
        {
            if (chassis.corners[i] != null) chassis.corners[i].UpdateVisuals();
        }
    }

    private void HandleTransmissionInputs()
    {
        if (vehicleInput.ShiftUp) powertrain.powerTrain.transmission.ShiftUp();
        if (vehicleInput.ShiftDown) powertrain.powerTrain.transmission.ShiftDown();
    }

    private void ApplyAerodynamics()
    {
        Vector3 aeroForcesLocal = aerodynamics.CalculateAerodynamicForces(rb.linearVelocity, transform);
        Vector3 aeroForcesWorld = transform.TransformDirection(aeroForcesLocal);
        Vector3 centerOfPressureWorld = transform.TransformPoint(aerodynamics.aeroData.centerOfPressureOffset);
        rb.AddForceAtPosition(aeroForcesWorld, centerOfPressureWorld);
    }
}