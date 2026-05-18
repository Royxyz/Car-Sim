using UnityEngine;
using UnityEngine.InputSystem;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class PlayerTelemetryLogger : MonoBehaviour
{
    [Header("Logging Settings")]
    public string saveDirectory = "_CarSim/PlayerTelemetry";
    
    [Tooltip("Maximum recording time in seconds before it auto-saves.")]
    public float maxRecordingTime = 120f;
    
    [Tooltip("How many times per second to write data. 10-20Hz is usually plenty for track analysis.")]
    public float sampleRateHz = 10f;

    [Header("Controls")]
    [Tooltip("Bind your auxiliary button from the Input Action map here.")]
    public InputActionReference toggleRecordAction;

    private SimulationController sim;
    private StringBuilder csvRows;
    private bool isLogging = false;
    
    private float currentRecordTime = 0f;
    private float timeSinceLastLog = 0f;
    
    // Physics Tracking Over Time
    private Vector3 lastVelocity;
    private float[] lastSuspensionTravel = new float[4];
    private float totalDistanceTraveled = 0f;
    private Vector3 lastPosition;
    string[] corners = { "FL", "FR", "RL", "RR" };

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        csvRows = new StringBuilder();
    }

    private void OnEnable()
    {
        if (toggleRecordAction != null)
        {
            toggleRecordAction.action.Enable();
            toggleRecordAction.action.performed += OnToggleRecord;
        }
    }

    private void OnDisable()
    {
        if (toggleRecordAction != null)
        {
            toggleRecordAction.action.performed -= OnToggleRecord;
            toggleRecordAction.action.Disable();
        }
    }

    private void OnToggleRecord(InputAction.CallbackContext ctx)
    {
        if (isLogging) StopLoggingAndSave();
        else StartLogging();
    }

    private void StartLogging()
    {
        csvRows.Clear();
        
        string header = "Time_s,Distance_m,PosX,PosY,PosZ,Speed_Kmh,Accel_Long_G,Accel_Lat_G,Raw_Steer,Raw_Throttle,Raw_Brake,Clutch,Gear,RPM,Torque_Nm,Boost_Bar,Pitch,Roll,YawRate,Aero_DownN,Aero_DragN";
        
        foreach (string c in corners)
        {
            header += $",{c}_Load_N,{c}_Travel_m,{c}_DamperVel_ms,{c}_Slip,{c}_SlipDelta,{c}_SlipAng_deg,{c}_LatDelta,{c}_WheelRPM,{c}_LongForce_N,{c}_LatForce_N,{c}_ActBrakeTrq_Nm,{c}_ABS_Active";
        }
        
        csvRows.AppendLine(header);
        
        lastVelocity = sim.rb.linearVelocity;
        lastPosition = sim.rb.position;
        for (int i = 0; i < 4; i++) lastSuspensionTravel[i] = 0f;
        
        totalDistanceTraveled = 0f;
        currentRecordTime = 0f;
        timeSinceLastLog = 0f;
        
        isLogging = true;
        Debug.Log("<color=green><b>[Telemetry]</b> Recording Started.</color>");
    }

    private void FixedUpdate()
    {
        if (!isLogging) return;

        float dt = Time.fixedDeltaTime;
        currentRecordTime += dt;
        timeSinceLastLog += dt;

        // Track distance continuously even if we aren't writing to the row this frame
        Vector3 currentPos = sim.rb.position;
        totalDistanceTraveled += Vector3.Distance(lastPosition, currentPos);
        lastPosition = currentPos;

        if (timeSinceLastLog >= (1f / sampleRateHz))
        {
            LogDataRow(timeSinceLastLog);
            timeSinceLastLog = 0f;
        }

        if (currentRecordTime >= maxRecordingTime)
        {
            Debug.Log("<color=yellow><b>[Telemetry]</b> Max recording time reached.</color>");
            StopLoggingAndSave();
        }
    }

    private void LogDataRow(float timeDelta)
    {
        Vector3 currentVel = sim.rb.linearVelocity;
        // Calculate acceleration based on the actual sample window, not fixedDeltaTime
        Vector3 localAccel = sim.transform.InverseTransformDirection((currentVel - lastVelocity) / timeDelta);
        float longG = localAccel.z / 9.81f;
        float latG = localAccel.x / 9.81f;
        lastVelocity = currentVel;

        float speedKmh = currentVel.magnitude * 3.6f;
        
        Vector3 euler = sim.rb.rotation.eulerAngles;
        float pitch = euler.x > 180 ? euler.x - 360 : euler.x;
        float roll = euler.z > 180 ? euler.z - 360 : euler.z;
        float yawRate = sim.rb.angularVelocity.y * Mathf.Rad2Deg;

        float rpm = sim.powerTrain.engineRPM;
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        

        var input = sim.GetComponent<IVehicleInput>();

        csvRows.Append(currentRecordTime.ToString("F3")).Append(",")
               .Append(totalDistanceTraveled.ToString("F1")).Append(",")
               .Append(sim.rb.position.x.ToString("F2")).Append(",")
               .Append(sim.rb.position.y.ToString("F2")).Append(",")
               .Append(sim.rb.position.z.ToString("F2")).Append(",")
               .Append(speedKmh.ToString("F1")).Append(",")
               .Append(longG.ToString("F2")).Append(",")
               .Append(latG.ToString("F2")).Append(",")
               .Append(input.Steering.ToString("F2")).Append(",")
               .Append(input.Throttle.ToString("F2")).Append(",")
               .Append(input.Brake.ToString("F2")).Append(",")
               .Append(sim.powerTrain.clutch.engagement.ToString("F2")).Append(",")
               .Append(sim.powerTrain.transmission.currentGear).Append(",")
               .Append(rpm.ToString("F0")).Append(",")
               .Append(sim.powerTrain.currentNetTorque.ToString("F1")).Append(",")
               .Append(boost.ToString("F2")).Append(",")
               .Append(pitch.ToString("F2")).Append(",")
               .Append(roll.ToString("F2")).Append(",")
               .Append(yawRate.ToString("F2")).Append(",")
               .Append(Mathf.Abs(sim.TotalDownforce).ToString("F0")).Append(",")
               .Append(Mathf.Abs(sim.TotalDragForce).ToString("F0"));

        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            
            float load = corner.suspension.currentNormalLoad;
            float travel = corner.suspension.suspData.targetRideHeight - corner.suspension.currentLength; 
            
            // Average damper velocity over the sample window
            float damperVel = (travel - lastSuspensionTravel[i]) / timeDelta;
            lastSuspensionTravel[i] = travel;

            float slip = corner.wheel.longitudinalSlip;
            float slipAngle = corner.wheel.slipAngle * Mathf.Rad2Deg;
            
            float optimalLongSlip = 1f / corner.tire.tireData.longB;
            float optimalLatSlip = (1f / corner.tire.tireData.latB) * Mathf.Rad2Deg;
            
            float slipDelta = Mathf.Abs(slip) - optimalLongSlip;
            float latDelta = Mathf.Abs(slipAngle) - optimalLatSlip;

            Vector2 gripForces = corner.tire.CalculateGripForces(load, slip, corner.wheel.slipAngle, corner.wheel.forwardSpeed, corner.wheel.wheelLinearSpeed, 0f);

            csvRows.Append(",")
                   .Append(load.ToString("F0")).Append(",")
                   .Append(travel.ToString("F4")).Append(",")
                   .Append(damperVel.ToString("F3")).Append(",")
                   .Append(slip.ToString("F3")).Append(",")
                   .Append(slipDelta.ToString("F3")).Append(",")
                   .Append(slipAngle.ToString("F2")).Append(",")
                   .Append(latDelta.ToString("F2")).Append(",")
                   .Append((corner.wheel.angularVelocity * (30f/Mathf.PI)).ToString("F0")).Append(",")
                   .Append(gripForces.x.ToString("F0")).Append(",")
                   .Append(gripForces.y.ToString("F0")).Append(",")
                   .Append(corner.brake.currentAppliedTorque.ToString("F0")).Append(",")
                   .Append(corner.brake.isABSDriveActive ? "1" : "0");
        }
        csvRows.AppendLine();
    }

    private void StopLoggingAndSave()
    {
        isLogging = false;
        
        string dirPath = Path.Combine(Application.dataPath, saveDirectory);
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(dirPath, $"PlayerTelemetry_{timestamp}.csv");

        File.WriteAllText(path, csvRows.ToString());
        Debug.Log($"<color=cyan><b>[Telemetry]</b> Data Saved: {path}</color>");
    }
}