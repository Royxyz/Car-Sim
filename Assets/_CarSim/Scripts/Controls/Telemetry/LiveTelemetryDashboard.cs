using UnityEngine;

[RequireComponent(typeof(SimulationController))]
public class LiveTelemetryDashboard : MonoBehaviour
{
    private SimulationController sim;
    private IVehicleInput inputs;

    // For G-Force tracking
    private Vector3 lastVelocity;
    private float currentLongG = 0f;
    private float currentLatG = 0f;

    [Header("Dashboard Settings")]
    public int guiScale = 2; // Increase if playing on a 4K monitor
    public bool showWheelData = true;

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        inputs = GetComponent<IVehicleInput>();
    }

    private void FixedUpdate()
    {
        if (sim == null || sim.rb == null) return;

        // Calculate G-Forces
        float dt = Time.fixedDeltaTime;
        Vector3 currentVel = sim.rb.linearVelocity;
        Vector3 localAccel = sim.transform.InverseTransformDirection((currentVel - lastVelocity) / dt);
        
        // Smooth the G-force slightly so the numbers are readable
        currentLongG = Mathf.Lerp(currentLongG, localAccel.z / 9.81f, dt * 10f);
        currentLatG = Mathf.Lerp(currentLatG, localAccel.x / 9.81f, dt * 10f);
        
        lastVelocity = currentVel;
    }

    private void OnGUI()
    {
        if (sim == null || inputs == null) return;

        // Scale the UI for higher resolution screens
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1));

        // Background Box
        float boxWidth = showWheelData ? 450 : 250;
        GUI.Box(new Rect(10, 10, boxWidth, 320), "<b>VEHICLE TELEMETRY</b>");

        // --- Column 1: Core Powertrain & Inputs ---
        GUILayout.BeginArea(new Rect(20, 40, 220, 300));
        
        float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        DrawRow("Speed:", $"{speedKmh:F1} km/h", Color.white);
        
        int gear = sim.powerTrain.transmission.currentGear;
        string gearString = gear == -1 ? "R" : gear == 0 ? "N" : gear.ToString();
        DrawRow("Gear:", gearString, Color.cyan);
        
        DrawRow("RPM:", $"{sim.powerTrain.engineRPM:F0}", Color.yellow);
        
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        DrawRow("Manifold (Bar):", $"{boost:F2}", boost > 1.0f ? Color.red : Color.white);

        GUILayout.Space(10);
        DrawRow("Throttle:", $"{inputs.Throttle * 100:F0}%", Color.green);
        DrawRow("Brake:", $"{inputs.Brake * 100:F0}%", Color.red);
        DrawRow("Clutch:", $"{inputs.Clutch * 100:F0}%", Color.gray);
        DrawRow("Steer Angle:", $"{inputs.Steering * sim.steeringData.maxSteerAngle:F1}°", Color.white);

        GUILayout.Space(10);
        DrawRow("Long G:", $"{currentLongG:F2} G", Mathf.Abs(currentLongG) > 0.8f ? Color.yellow : Color.white);
        DrawRow("Lat G:", $"{currentLatG:F2} G", Mathf.Abs(currentLatG) > 0.8f ? Color.yellow : Color.white);

        GUILayout.EndArea();

        // --- Column 2: Wheel Dynamics (Optional) ---
        if (showWheelData && sim.corners.Length == 4)
        {
            GUILayout.BeginArea(new Rect(250, 40, 180, 300));
            
            GUILayout.Label("<b>TIRE SLIP (Long)</b>");
            DrawRow("FL Slip:", $"{sim.corners[0].wheel.longitudinalSlip:F3}", GetSlipColor(sim.corners[0].wheel.longitudinalSlip));
            DrawRow("FR Slip:", $"{sim.corners[1].wheel.longitudinalSlip:F3}", GetSlipColor(sim.corners[1].wheel.longitudinalSlip));
            DrawRow("RL Slip:", $"{sim.corners[2].wheel.longitudinalSlip:F3}", GetSlipColor(sim.corners[2].wheel.longitudinalSlip));
            DrawRow("RR Slip:", $"{sim.corners[3].wheel.longitudinalSlip:F3}", GetSlipColor(sim.corners[3].wheel.longitudinalSlip));

            GUILayout.Space(10);
            GUILayout.Label("<b>SUSPENSION LOAD</b>");
            DrawRow("FL Load:", $"{sim.corners[0].suspension.currentNormalLoad:F0} N", Color.white);
            DrawRow("FR Load:", $"{sim.corners[1].suspension.currentNormalLoad:F0} N", Color.white);
            DrawRow("RL Load:", $"{sim.corners[2].suspension.currentNormalLoad:F0} N", Color.white);
            DrawRow("RR Load:", $"{sim.corners[3].suspension.currentNormalLoad:F0} N", Color.white);

            GUILayout.EndArea();
        }
    }

    // Helper to draw clean rows with colored values
    private void DrawRow(string label, string value, Color valueColor)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(100));
        GUI.contentColor = valueColor;
        GUILayout.Label(value);
        GUI.contentColor = Color.white; // Reset
        GUILayout.EndHorizontal();
    }

    // Helper to turn text red/yellow when tires lose traction
    private Color GetSlipColor(float slip)
    {
        float absSlip = Mathf.Abs(slip);
        if (absSlip > 0.15f) return Color.red;    // Hard wheelspin or locking
        if (absSlip > 0.08f) return Color.yellow; // Peak Pacejka grip
        return Color.green;                       // Rolling smoothly
    }
}