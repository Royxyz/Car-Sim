using UnityEngine;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class AdvancedTelemetryLogger : MonoBehaviour
{
    [Header("File Settings")]
    public string saveDirectory = "_CarSim/Telemetry";
    
    private SimulationController sim;
    private AIDriverStressTest aiDriver; 
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
    private float peakDownforce = 0f;

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        aiDriver = GetComponent<AIDriverStressTest>();
        csvRows = new StringBuilder();
    }

    public void StartLogging()
    {
        csvRows.Clear();
        
        // Massive Header Construction
        string header = "Time,TestStage,SpeedKmh,Accel_Long_G,Accel_Lat_G,Steer,Throttle,Brake,Clutch,Gear,RPM,Torque_Nm,Boost_Bar,Pitch,Roll,YawRate,Aero_Downforce_N,Aero_Drag_N";
        
        string[] corners = { "FL", "FR", "RL", "RR" };
        foreach (string c in corners)
        {
            header += $",{c}_Load_N,{c}_Travel_m,{c}_Slip,{c}_SlipAng_deg,{c}_RPM,{c}_LongForce_N,{c}_LatForce_N,{c}_BrakeTrq_Nm,{c}_ABS_Active";
        }
        
        csvRows.AppendLine(header);
        
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
        if (longG < peakBrakeG) peakBrakeG = longG; 
        if (Mathf.Abs(latG) > peakLatG) peakLatG = Mathf.Abs(latG);
        if (speedKmh > topSpeedKmh) topSpeedKmh = speedKmh;
        
        float rpm = sim.powerTrain.engineRPM;
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        if (rpm > peakRPM) peakRPM = rpm;
        if (boost > peakBoost) peakBoost = boost;

        // 2. Body Attitude & Powertrain
        Vector3 eulerAngles = sim.rb.rotation.eulerAngles;
        float pitch = eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x;
        float roll = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
        float yawRate = sim.rb.angularVelocity.y * Mathf.Rad2Deg;

        int gear = sim.powerTrain.transmission.currentGear;
        float netTorque = sim.powerTrain.currentNetTorque;
        float clutchEng = sim.powerTrain.clutch.engagement;
        
        // Calculate Aerodynamics (Using the new Flight-Sim Logic)
        Vector3 aeroForcesLocal = sim.aerodynamics.CalculateAerodynamicForces(sim.rb.linearVelocity, sim.transform);
        float downforceN = Mathf.Abs(aeroForcesLocal.y);
        float dragN = Mathf.Abs(aeroForcesLocal.z);
        if (downforceN > peakDownforce) peakDownforce = downforceN;

        string currentStage = aiDriver != null ? aiDriver.currentState.ToString() : "Manual";

        // Start Row
        string line = $"{Time.time:F3},{currentStage},{speedKmh:F1},{longG:F2},{latG:F2},{sim.GetComponent<IVehicleInput>().Steering:F2},{sim.GetComponent<IVehicleInput>().Throttle:F2},{sim.GetComponent<IVehicleInput>().Brake:F2},{clutchEng:F2},{gear},{rpm:F0},{netTorque:F1},{boost:F2},{pitch:F2},{roll:F2},{yawRate:F2},{downforceN:F0},{dragN:F0}";

        // 3. Corner Data (High-Resolution Extraction)
        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            
            // Suspension & Wheel Kinematics
            float load = corner.suspension.currentNormalLoad;
            float travel = corner.suspension.suspData.restLength - corner.suspension.currentLength; 
            float slip = corner.wheel.longitudinalSlip;
            float slipAngle = corner.wheel.slipAngle * Mathf.Rad2Deg;
            float wheelRpm = corner.wheel.angularVelocity * (30f / Mathf.PI);

            // Re-evaluate Pacejka to extract exact forces at this millisecond
            Vector2 gripForces = corner.tire.CalculateGripForces(load, corner.wheel.longitudinalSlip, corner.wheel.slipAngle);
            float longForceFx = gripForces.x;
            float latForceFy = gripForces.y;

            // Brake specific data
            float brakeTorque = corner.brake.currentAppliedTorque;
            int absActive = corner.brake.isABSDriveActive ? 1 : 0;

            // Append to row
            line += $",{load:F0},{travel:F3},{slip:F3},{slipAngle:F2},{wheelRpm:F0},{longForceFx:F0},{latForceFy:F0},{brakeTorque:F0},{absActive}";
        }

        csvRows.AppendLine(line);
    }

    public void StopLoggingAndSave(AIDriverStressTest aiDriverRef)
    {
        isLogging = false;

        // Construct the Benchmark Summary Header
        StringBuilder finalOutput = new StringBuilder();
        finalOutput.AppendLine("===== VEHICLE DYNAMICS BENCHMARK REPORT =====");
        finalOutput.AppendLine($"Test Weight: {sim.rb.mass} kg");
        finalOutput.AppendLine($"0-100 km/h Time: {aiDriverRef.timeTo100Kmh:F2} sec");
        finalOutput.AppendLine($"Braking Distance (100-0): {aiDriverRef.brakingDistance:F1} meters");
        finalOutput.AppendLine($"Peak Acceleration: {peakAccelG:F2} G");
        finalOutput.AppendLine($"Peak Braking: {Mathf.Abs(peakBrakeG):F2} G");
        finalOutput.AppendLine($"Peak Lateral Grip: {peakLatG:F2} G");
        finalOutput.AppendLine($"Peak Downforce Generated: {peakDownforce:F0} N (approx {(peakDownforce/9.81f):F0} kg)");
        finalOutput.AppendLine($"Peak Engine Speed: {peakRPM:F0} RPM");
        finalOutput.AppendLine($"Peak Manifold Pressure: {peakBoost:F2} Bar");
        finalOutput.AppendLine($"Top Speed Reached: {topSpeedKmh:F1} km/h");
        finalOutput.AppendLine("=============================================\n");
        
        finalOutput.Append(csvRows.ToString());

        // Manage Directories and Run Numbers
        string dirPath = Path.Combine(Application.dataPath, saveDirectory);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        int runNumber = 1;
        string filePath;
        do
        {
            filePath = Path.Combine(dirPath, $"Run_{runNumber:D3}.csv");
            runNumber++;
        } while (File.Exists(filePath));

        File.WriteAllText(filePath, finalOutput.ToString());
        
        Debug.Log($"<color=cyan><b>[Telemetry]</b> Granular Benchmark Saved to: {filePath}</color>");
    }
}