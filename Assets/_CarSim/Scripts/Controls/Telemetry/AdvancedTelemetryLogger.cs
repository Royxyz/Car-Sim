using UnityEngine;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class AdvancedTelemetryLogger : MonoBehaviour
{
    [Header("Dependencies")]
    public DynamicTestDirector director; // MUST BE ASSIGNED IN INSPECTOR

    [Header("File Settings")]
    public string saveDirectory = "_CarSim/Telemetry";
    
    private SimulationController sim;
    private StringBuilder csvRows;
    private bool isLogging = false;
    private bool hasSaved = false; // Prevents re-triggering after the test ends

    // --- Peak Stat Tracking ---
    private Vector3 lastVelocity;
    private float[] lastSuspensionTravel = new float[4];
    private float totalDistanceTraveled = 0f;
    private Vector3 lastPosition;

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        csvRows = new StringBuilder();
    }

    private void StartLogging()
    {
        csvRows.Clear();
        
        // Injected "TestPhase" right after Time_s
        string header = "Time_s,TestPhase,Distance_m,PosX,PosY,PosZ,Speed_Kmh,Accel_Long_G,Accel_Lat_G,Raw_Steer,Raw_Throttle,Raw_Brake,Clutch,Gear,RPM,Torque_Nm,Boost_Bar,Pitch,Roll,YawRate,Aero_DownN,Aero_DragN";
        
        string[] corners = { "FL", "FR", "RL", "RR" };
        foreach (string c in corners)
        {
            header += $",{c}_Load_N,{c}_Travel_m,{c}_DamperVel_ms,{c}_Slip,{c}_SlipDelta,{c}_SlipAng_deg,{c}_LatDelta,{c}_WheelRPM,{c}_LongForce_N,{c}_LatForce_N,{c}_ActBrakeTrq_Nm,{c}_ABS_Active";
        }
        
        csvRows.AppendLine(header);
        
        lastVelocity = sim.rb.linearVelocity;
        lastPosition = sim.rb.position;
        for(int i = 0; i < 4; i++) lastSuspensionTravel[i] = 0f;
        totalDistanceTraveled = 0f;
        
        isLogging = true;
        Debug.Log("<color=green><b>[Telemetry]</b> MoTeC Logging Auto-Started.</color>");
    }

    private void FixedUpdate()
    {
        if (director == null) return;

        // 1. AUTO-START LOGGING
        // If we haven't saved yet, aren't logging, and the Director has started the test
        if (!isLogging && !hasSaved && director.currentPhase != TestPhase.Idle && director.currentPhase != TestPhase.Finished)
        {
            StartLogging();
        }

        // 2. AUTO-STOP LOGGING
        // Triggered by the new boolean you added to the Director
        if (isLogging && director.stopLogging)
        {
            StopLoggingAndSave();
            return; // Exit out of this frame so we don't log after stopping
        }

        // 3. CONTINUOUS LOGGING EXECUTION
        if (!isLogging) return;

        float dt = Time.fixedDeltaTime;

        // Spatial & Chassis Dynamics
        Vector3 currentPos = sim.rb.position;
        totalDistanceTraveled += Vector3.Distance(lastPosition, currentPos);
        lastPosition = currentPos;

        Vector3 currentVel = sim.rb.linearVelocity;
        Vector3 localAccel = sim.transform.InverseTransformDirection((currentVel - lastVelocity) / dt);
        float longG = localAccel.z / 9.81f;
        float latG = localAccel.x / 9.81f;
        lastVelocity = currentVel;

        float speedKmh = currentVel.magnitude * 3.6f;
        
        Vector3 euler = sim.rb.rotation.eulerAngles;
        float pitch = euler.x > 180 ? euler.x - 360 : euler.x;
        float roll = euler.z > 180 ? euler.z - 360 : euler.z;
        float yawRate = sim.rb.angularVelocity.y * Mathf.Rad2Deg;

        // Powertrain & Aero
        float rpm = sim.powerTrain.engineRPM;
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        Vector3 aero = sim.aerodynamics.CalculateAerodynamicForces(currentVel, sim.transform);

        var input = sim.GetComponent<IVehicleInput>();

        // Cast the TestPhase enum to an integer so it graphs cleanly in Excel/MoTeC (Idle=0, Launch=1, Brake=2, etc.)
        int currentPhaseInt = (int)director.currentPhase;

        csvRows.Append(Time.time.ToString("F3")).Append(",")
               .Append(currentPhaseInt).Append(",") // <--- NEW DATA POINT
               .Append(totalDistanceTraveled.ToString("F1")).Append(",")
               .Append(currentPos.x.ToString("F2")).Append(",")
               .Append(currentPos.y.ToString("F2")).Append(",")
               .Append(currentPos.z.ToString("F2")).Append(",")
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
               .Append(Mathf.Abs(aero.y).ToString("F0")).Append(",")
               .Append(Mathf.Abs(aero.z).ToString("F0"));

        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            
            float load = corner.suspension.currentNormalLoad;
            float travel = corner.suspension.suspData.targetRideHeight - corner.suspension.currentLength; 
            float damperVel = (travel - lastSuspensionTravel[i]) / dt;
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
        hasSaved = true; // Lock it so it doesn't accidentally restart
        
        string dirPath = Path.Combine(Application.dataPath, saveDirectory);
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        int run = 1;
        string path;
        do { path = Path.Combine(dirPath, $"Telemetry_Run_{run:D3}.csv"); run++; } while (File.Exists(path));

        File.WriteAllText(path, csvRows.ToString());
        Debug.Log($"<color=cyan><b>[Telemetry]</b> MoTeC-Grade Data Saved to: {path}</color>");
        
        // Optional: Pause the Unity Editor automatically so you know the test finished
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = true;
        #endif
    }
}