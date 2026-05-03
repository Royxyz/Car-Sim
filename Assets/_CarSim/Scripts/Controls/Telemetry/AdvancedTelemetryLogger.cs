using UnityEngine;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class AdvancedTelemetryLogger : MonoBehaviour
{
    public string fileName = "Benchmark_Report.csv";
    
    private SimulationController sim;
    private IVehicleInput inputs;
    private StringBuilder csvRows;
    private bool isLogging = false;

    // --- Peak Stat Tracking ---
    private Vector3 lastVelocity;
    private float peakAccelG = 0f;
    private float peakBrakeG = 0f;
    private float peakLatG = 0f;
    private float peakBoost = 0f;
    private float peakRPM = 0f;
    private float topSpeedKmh = 0f;

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        inputs = GetComponent<IVehicleInput>();
        csvRows = new StringBuilder();
    }

    public void StartLogging()
    {
        csvRows.Clear();
        csvRows.AppendLine("Time,SpeedKmh,Accel_Long_G,Accel_Lat_G,Steer,Throttle,Brake,Gear,RPM,Boost_Bar,Pitch,Roll,YawRate," +
                           "FL_Load,FL_Slip,FL_SlipAngle,FL_Travel," +
                           "FR_Load,FR_Slip,FR_SlipAngle,FR_Travel," +
                           "RL_Load,RL_Slip,RL_SlipAngle,RL_Travel," +
                           "RR_Load,RR_Slip,RR_SlipAngle,RR_Travel");
        
        lastVelocity = sim.rb.linearVelocity;
        isLogging = true;
    }

    private void FixedUpdate()
    {
        if (!isLogging) return;

        float dt = Time.fixedDeltaTime;

        // 1. Core Chassis & G-Forces
        Vector3 currentVel = sim.rb.linearVelocity;
        Vector3 localAccel = sim.transform.InverseTransformDirection((currentVel - lastVelocity) / dt);
        float longG = localAccel.z / 9.81f;
        float latG = localAccel.x / 9.81f;
        lastVelocity = currentVel;

        float speedKmh = currentVel.magnitude * 3.6f;
        
        // --- Track Benchmark Peaks ---
        if (longG > peakAccelG) peakAccelG = longG;
        if (longG < peakBrakeG) peakBrakeG = longG; // Braking is negative long G
        if (Mathf.Abs(latG) > peakLatG) peakLatG = Mathf.Abs(latG);
        if (speedKmh > topSpeedKmh) topSpeedKmh = speedKmh;
        
        float rpm = sim.powerTrain.engineRPM;
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        if (rpm > peakRPM) peakRPM = rpm;
        if (boost > peakBoost) peakBoost = boost;

        // 2. Body Attitude
        Vector3 eulerAngles = sim.rb.rotation.eulerAngles;
        float pitch = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
        float roll = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
        float yawRate = sim.rb.angularVelocity.y * Mathf.Rad2Deg;

        int gear = sim.powerTrain.transmission.currentGear;

        // Start Row
        string line = $"{Time.time:F3},{speedKmh:F1},{longG:F2},{latG:F2},{inputs.Steering:F2},{inputs.Throttle:F2},{inputs.Brake:F2},{gear},{rpm:F0},{boost:F2},{pitch:F2},{roll:F2},{yawRate:F2}";

        // 3. Corner Data
        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            float load = corner.suspension.currentNormalLoad;
            float slip = corner.wheel.longitudinalSlip;
            float slipAngle = corner.wheel.slipAngle * Mathf.Rad2Deg;
            float travel = corner.suspension.suspData.restLength - corner.suspension.currentLength; 

            line += $",{load:F0},{slip:F3},{slipAngle:F2},{travel:F3}";
        }

        csvRows.AppendLine(line);
    }

    public void StopLoggingAndSave(AIDriverStressTest aiDriver)
    {
        isLogging = false;

        // Construct the Benchmark Summary Header
        StringBuilder finalOutput = new StringBuilder();
        finalOutput.AppendLine("===== VEHICLE DYNAMICS BENCHMARK REPORT =====");
        finalOutput.AppendLine($"Test Weight: {sim.rb.mass} kg");
        finalOutput.AppendLine($"0-100 km/h Time: {aiDriver.timeTo100Kmh:F2} sec");
        finalOutput.AppendLine($"Braking Distance (100-0): {aiDriver.brakingDistance:F1} meters");
        finalOutput.AppendLine($"Peak Acceleration: {peakAccelG:F2} G");
        finalOutput.AppendLine($"Peak Braking: {Mathf.Abs(peakBrakeG):F2} G");
        finalOutput.AppendLine($"Peak Lateral Grip: {peakLatG:F2} G");
        finalOutput.AppendLine($"Peak Engine Speed: {peakRPM:F0} RPM");
        finalOutput.AppendLine($"Peak Manifold Pressure: {peakBoost:F2} Bar");
        finalOutput.AppendLine($"Top Speed Reached: {topSpeedKmh:F1} km/h");
        finalOutput.AppendLine("=============================================\n");
        
        // Append the raw telemetry rows below the report
        finalOutput.Append(csvRows.ToString());

        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(path, finalOutput.ToString());
        
        Debug.Log($"<color=cyan><b>[Telemetry]</b> Benchmark Saved to: {path}</color>\n{finalOutput.ToString().Substring(0, 500)}...");
    }
}