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

        csv.AppendLine("Time,Steer,Throttle,Brake,Clutch,PosY,RotX,RotY,RotZ,AngularVel_Mag,FL_Grounded,FL_SuspLen,FL_NormLoad,FL_LongSlip,FL_SlipAngle,FR_Grounded,FR_SuspLen,FR_NormLoad,FR_LongSlip,FR_SlipAngle,RL_Grounded,RL_SuspLen,RL_NormLoad,RL_LongSlip,RL_SlipAngle,RR_Grounded,RR_SuspLen,RR_NormLoad,RR_LongSlip,RR_SlipAngle");
    }

    private void FixedUpdate()
    {
        if (!isRecording || sim == null || inputManager == null) return;

        timer += Time.fixedDeltaTime;

        // FIX 4: Use StringBuilder.Append instead of string interpolation
        csv.Append(Time.time.ToString("F3")).Append(",")
           .Append(inputManager.steeringInput.ToString("F3")).Append(",")
           .Append(inputManager.throttleInput.ToString("F3")).Append(",")
           .Append(inputManager.brakeInput.ToString("F3")).Append(",")
           .Append(sim.powerTrain.clutch.engagement.ToString("F3")).Append(",")
           .Append(transform.position.y.ToString("F3")).Append(",")
           .Append(sim.rb.rotation.eulerAngles.x.ToString("F3")).Append(",")
           .Append(sim.rb.rotation.eulerAngles.y.ToString("F3")).Append(",")
           .Append(sim.rb.rotation.eulerAngles.z.ToString("F3")).Append(",")
           .Append(sim.rb.angularVelocity.magnitude.ToString("F3"));

        for (int i = 0; i < 4; i++)
        {
            var corner = sim.corners[i];
            if (corner != null && corner.suspension != null && corner.wheel != null)
            {
                csv.Append(",").Append(corner.suspension.isGrounded)
                   .Append(",").Append(corner.suspension.currentLength.ToString("F3"))
                   .Append(",").Append(corner.suspension.currentNormalLoad.ToString("F1"))
                   .Append(",").Append(corner.wheel.longitudinalSlip.ToString("F3"))
                   .Append(",").Append(corner.wheel.slipAngle.ToString("F3"));
            }
            else
            {
                csv.Append(",False,0,0,0,0");
            }
        }

        csv.AppendLine();

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