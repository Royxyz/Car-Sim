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
    public AntiRollBarData antiRollBarData => chassis.antiRollBarData;

    //Telemetry Properties
    public float TotalDownforce  {get; private set;}
    public float TotalDragForce  {get; private set;}

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 

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

        float frontRideHeight = (chassis.corners[0].suspension.currentLength + chassis.corners[1].suspension.currentLength) * 0.5f;
        float rearRideHeight = (chassis.corners[2].suspension.currentLength + chassis.corners[3].suspension.currentLength) * 0.5f;

        AeroForces aero = aerodynamics.CalculateForces(rb.linearVelocity, transform, frontRideHeight, rearRideHeight);

        TotalDownforce = aero.frontDownforce + aero.rearDownforce;
        TotalDragForce = Vector3.Magnitude(aero.totalDragWorld);

        float frontZ = transform.InverseTransformPoint(chassis.corners[0].suspensionMountPoint.position).z;
        float rearZ = transform.InverseTransformPoint(chassis.corners[2].suspensionMountPoint.position).z;


        for (int step = 0; step < subSteps; step++)
        {
            vDynamics.ResetStepAccumulators();
            float stepFraction = (step + 1f) / subSteps;

            Vector3 frontAxleWorld = vDynamics.position + (vDynamics.rotation * new Vector3(0, 0, frontZ));
            Vector3 rearAxleWorld = vDynamics.position + (vDynamics.rotation * new Vector3(0, 0, rearZ));
            Vector3 aeroCenterWorld = vDynamics.position + (vDynamics.rotation * aerodynamics.aeroData.aeroCenterOffset);

            Vector3 dynamicDragDir = -vDynamics.linearVelocity.normalized;
            Vector3 dynamicSideforceWorld = vDynamics.rotation * new Vector3(-aero.totalSideforceWorld.magnitude * Mathf.Sign(Vector3.Dot(vDynamics.linearVelocity, vDynamics.rotation * Vector3.right)), 0, 0);

            vDynamics.AddForceAtPosition(-transform.up * aero.frontDownforce, frontAxleWorld);
            vDynamics.AddForceAtPosition(-transform.up * aero.rearDownforce, rearAxleWorld);
            vDynamics.AddForceAtPosition(aero.totalDragWorld + aero.totalSideforceWorld, aeroCenterWorld);

            float[] driveTorques = powertrain.ProcessTorqueRouting(chassis.corners, activeThrottle, activeBrake, isAutomatic, subDt);
            
            chassis.ProcessSubStep(vDynamics, cachedRoot, targetSteer, stepFraction, driveTorques, activeBrake, activeHandbrake, subDt);

            float maxSafeTorque = rb.mass * chassis.chassisData.maxSafeTorqueMultiplier;
            vDynamics.IntegrateStep(subDt, rb, maxSafeTorque);
        }

        vDynamics.SyncToRigidbody(rb);
        chassis.UpdateSteeringInterpolation(targetSteer);
        
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
}