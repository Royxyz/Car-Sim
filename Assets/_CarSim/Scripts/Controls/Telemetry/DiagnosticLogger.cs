using UnityEngine;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class DiagnosticLogger : MonoBehaviour
{
    private SimulationController sim;
    private StringBuilder csvRows;
    
    private bool isLogging = false;
    private bool hasFinished = false;
    private float logTimer = 0f;
    private const float MAX_LOG_TIME = 30f;

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        csvRows = new StringBuilder();
    }

    private void Start()
    {
        // Setup CSV Headers specifically targeted at straight-line acceleration instability
        string header = "Time_s,Speed_Kmh,YawRate_deg,Pitch_deg,Roll_deg,EngineRPM,Gear,NetTorque";
        string[] corners = { "FL", "FR", "RL", "RR" };
        
        foreach (string c in corners)
        {
            // We need to see exactly what the tires and suspension are doing frame-by-frame
            header += $",{c}_Load_N,{c}_Travel_m,{c}_SlipLong,{c}_SlipAngle_deg,{c}_DynamicToe_deg,{c}_ForceLong_N,{c}_ForceLat_N";
        }
        
        csvRows.AppendLine(header);
        Debug.Log("<color=yellow><b>[Diagnostic Logger]</b> Armed. Waiting for engine to rev above idle...</color>");
    }

    private void FixedUpdate()
    {
        if (hasFinished) return;

        // Trigger logging the moment the engine revs 100 RPM above idle
        if (!isLogging && sim.powerTrain != null && sim.powerTrain.engineRPM > (sim.powerTrain.engine._engineData.idleRPM + 100f))
        {
            isLogging = true;
            Debug.Log("<color=green><b>[Diagnostic Logger]</b> Engine rev detected! Recording 30s telemetry...</color>");
        }

        if (isLogging)
        {
            logTimer += Time.fixedDeltaTime;
            RecordFrameData();

            if (logTimer >= MAX_LOG_TIME)
            {
                SaveTelemetryData();
            }
        }
    }

    private void RecordFrameData()
    {
        float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        float yawRate = sim.rb.angularVelocity.y * Mathf.Rad2Deg;
        
        Vector3 euler = sim.rb.rotation.eulerAngles;
        float pitch = euler.x > 180 ? euler.x - 360 : euler.x;
        float roll = euler.z > 180 ? euler.z - 360 : euler.z;

        csvRows.Append(logTimer.ToString("F3")).Append(",")
               .Append(speedKmh.ToString("F1")).Append(",")
               .Append(yawRate.ToString("F3")).Append(",")
               .Append(pitch.ToString("F3")).Append(",")
               .Append(roll.ToString("F3")).Append(",")
               .Append(sim.powerTrain.engineRPM.ToString("F0")).Append(",")
               .Append(sim.powerTrain.transmission.currentGear).Append(",")
               .Append(sim.powerTrain.currentNetTorque.ToString("F1"));

        for (int i = 0; i < 4; i++)
        {
            WheelAssembly corner = sim.corners[i];
            
            float travel = corner.suspension.suspData.targetRideHeight - corner.suspension.currentLength;
            float slipAngleDeg = corner.wheel.slipAngle * Mathf.Rad2Deg;
            
            // Calculate what the dynamic toe angle actually is at this exact frame
            float dynamicToeAngle = travel * corner.suspension.suspData.bumpSteerPerMeter;
            if (i == 1 || i == 3) dynamicToeAngle = -dynamicToeAngle; 
            float totalToeDeg = (corner.ackermannSteeringAngle + dynamicToeAngle) * Mathf.Rad2Deg;

            // Re-calculate the exact forces generated this frame
            Vector2 gripForces = corner.tire.CalculateGripForces(
                corner.suspension.currentNormalLoad, 
                corner.wheel.longitudinalSlip, 
                corner.wheel.slipAngle, 
                corner.wheel.forwardSpeed, 
                corner.wheel.wheelLinearSpeed, 
                0f
            );

            csvRows.Append(",")
                   .Append(corner.suspension.currentNormalLoad.ToString("F0")).Append(",")
                   .Append(travel.ToString("F4")).Append(",")
                   .Append(corner.wheel.longitudinalSlip.ToString("F4")).Append(",")
                   .Append(slipAngleDeg.ToString("F4")).Append(",")
                   .Append(totalToeDeg.ToString("F4")).Append(",")
                   .Append(gripForces.x.ToString("F0")).Append(",")
                   .Append(gripForces.y.ToString("F0"));
        }
        
        csvRows.AppendLine();
    }

    private void SaveTelemetryData()
    {
        isLogging = false;
        hasFinished = true;

        string dirPath = Path.Combine(Application.dataPath, "_DiagnosticLogs");
        if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = Path.Combine(dirPath, $"Instability_Log_{timestamp}.csv");

        File.WriteAllText(path, csvRows.ToString());
        Debug.Log($"<color=cyan><b>[Diagnostic Logger]</b> 30s Recording Complete. Saved to: {path}</color>");
        
        // Pause the editor automatically so you don't have to scramble
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPaused = true;
        #endif
    }
}