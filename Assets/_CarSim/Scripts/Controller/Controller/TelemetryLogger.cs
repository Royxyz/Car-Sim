using UnityEngine;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class TelemetryLogger : MonoBehaviour
{
    [Header("Settings")]
    public float recordDuration = 15f;
    public string fileName = "CarTelemetry.csv";

    private SimulationController sim;
    private float timer = 0f;
    private bool isRecording = true;
    private StringBuilder csv;

    private void Start()
    {
        sim = GetComponent<SimulationController>();
        csv = new StringBuilder();

        // Build the CSV Header
        csv.AppendLine("Time,PosY,AngularVel_Mag,FL_Grounded,FL_SuspLen,FL_NormLoad,FL_LongSlip,FL_SlipAngle,FR_Grounded,FR_SuspLen,FR_NormLoad,FR_LongSlip,FR_SlipAngle,RL_Grounded,RL_SuspLen,RL_NormLoad,RL_LongSlip,RL_SlipAngle,RR_Grounded,RR_SuspLen,RR_NormLoad,RR_LongSlip,RR_SlipAngle");
    }

    private void FixedUpdate()
    {
        if (!isRecording || sim == null) return;

        timer += Time.fixedDeltaTime;

        // 1. Chassis Data
        float posY = transform.position.y;
        float angVel = sim.rb.angularVelocity.magnitude;
        string line = $"{Time.time:F3},{posY:F3},{angVel:F3}";

        // 2. Corner Data
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
        Debug.Log($"<color=cyan><b>[Telemetry]</b> 15-second diagnostic saved successfully to: {path}</color>");
    }
}
