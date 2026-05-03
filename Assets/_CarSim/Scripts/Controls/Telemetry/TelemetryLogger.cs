using UnityEngine;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController), typeof(InputManager))]
public class TelemetryLogger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How long to record telemetry before saving to CSV.")]
    public float recordDuration = 30f; 
    public string fileName = "CarTelemetry.csv";

    private SimulationController sim;
    private InputManager inputManager;
    private float timer = 0f;
    private bool isRecording = true;
    private StringBuilder csv;

    private void Start()
    {
        sim = GetComponent<SimulationController>();
        inputManager = GetComponent<InputManager>();
        csv = new StringBuilder();

        // Build the CSV Header - Now includes Inputs and Rotation
        csv.AppendLine("Time,Steer,Throttle,Brake,Clutch,PosY,RotX,RotY,RotZ,AngularVel_Mag,FL_Grounded,FL_SuspLen,FL_NormLoad,FL_LongSlip,FL_SlipAngle,FR_Grounded,FR_SuspLen,FR_NormLoad,FR_LongSlip,FR_SlipAngle,RL_Grounded,RL_SuspLen,RL_NormLoad,RL_LongSlip,RL_SlipAngle,RR_Grounded,RR_SuspLen,RR_NormLoad,RR_LongSlip,RR_SlipAngle");
    }

    private void FixedUpdate()
    {
        if (!isRecording || sim == null || inputManager == null) return;

        timer += Time.fixedDeltaTime;

        // 1. Driver Inputs
        float steer = inputManager.steeringInput;
        float throttle = inputManager.throttleInput;
        float brake = inputManager.brakeInput;
        float clutch = sim.powerTrain.clutch.engagement;

        // 2. Chassis Data & Rotation
        float posY = transform.position.y;
        Vector3 rot = sim.rb.rotation.eulerAngles; // Euler angles for Pitch, Yaw, Roll
        float angVel = sim.rb.angularVelocity.magnitude;
        
        // Start building the row
        string line = $"{Time.time:F3},{steer:F3},{throttle:F3},{brake:F3},{clutch:F3},{posY:F3},{rot.x:F3},{rot.y:F3},{rot.z:F3},{angVel:F3}";

        // 3. Corner Data
        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            if (corner != null && corner.suspension != null && corner.wheel != null)
            {
                bool grounded = corner.suspension.isGrounded;
                float len = corner.suspension.currentLength;
                float load = corner.suspension.currentNormalLoad;
                float longSlip = corner.wheel.longitudinalSlip;
                float slipAngle = corner.wheel.slipAngle;

                line += $",{grounded},{len:F3},{load:F1},{longSlip:F3},{slipAngle:F3}";
            }
            else
            {
                // Fallback if a corner is missing
                line += ",False,0,0,0,0";
            }
        }

        csv.AppendLine(line);

        if (timer >= recordDuration)
        {
            SaveTelemetry();
        }
    }

    private void SaveTelemetry()
    {
        isRecording = false;
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(path, csv.ToString());
        Debug.Log($"<color=cyan><b>[Telemetry]</b> {recordDuration}-second diagnostic saved successfully to: {path}</color>");
    }
}