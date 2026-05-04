using UnityEngine;

[RequireComponent(typeof(SimulationController))]
public class LiveTelemetryDashboard : MonoBehaviour
{
    private SimulationController sim;
    private IVehicleInput inputs;

    // Smoothing for G-Forces to eliminate jitter
    private Vector3 lastVelocity;
    private float currentLongG;
    private float currentLatG;
    private float longGVelocity;
    private float latGVelocity;

    [Header("Dashboard Settings")]
    public int guiScale = 1; 
    public bool showWheelData = true;
    [Tooltip("Higher = smoother G-force readings, but slightly more delayed.")]
    public float gForceSmoothTime = 0.15f; 

    // Cached GUI styles
    private GUIStyle leftAlign;
    private GUIStyle rightAlign;
    private GUIStyle boldHeader;

    private void Awake()
    {
        sim = GetComponent<SimulationController>();
        inputs = GetComponent<IVehicleInput>();
    }

    private void FixedUpdate()
    {
        if (sim == null || sim.rb == null) return;

        float dt = Time.fixedDeltaTime;
        Vector3 currentVel = sim.rb.linearVelocity;
        Vector3 localAccel = sim.transform.InverseTransformDirection((currentVel - lastVelocity) / dt);

        // SmoothDamp entirely eliminates the harsh frame-to-frame physics jitter
        currentLongG = Mathf.SmoothDamp(currentLongG, localAccel.z / 9.81f, ref longGVelocity, gForceSmoothTime);
        currentLatG = Mathf.SmoothDamp(currentLatG, localAccel.x / 9.81f, ref latGVelocity, gForceSmoothTime);

        lastVelocity = currentVel;
    }

    private void OnGUI()
    {
        if (sim == null || inputs == null) return;

        InitStyles();

        // Scale the UI for higher resolution screens
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1));

        // Compact Layout Dimensions
        float width = 380f;
        float height = showWheelData ? 150f : 100f;
        
        GUILayout.BeginArea(new Rect(10, 10, width, height), GUI.skin.box);
        
        // --- HEADER ---
        GUILayout.BeginHorizontal();
        GUILayout.Label("<b>VEHICLE TELEMETRY</b>", boldHeader);
        int gear = sim.powerTrain.transmission.currentGear;
        string gearStr = gear == -1 ? "R" : gear == 0 ? "N" : gear.ToString();
        GUILayout.Label($"<b>GEAR: <color=cyan>{gearStr}</color></b>", rightAlign);
        GUILayout.EndHorizontal();

        // --- ROW 1: Engine State ---
        float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        float rpm = sim.powerTrain.engineRPM;
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        string boostColor = boost > 1.0f ? "red" : "white";
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Spd: {speedKmh,3:F0} km/h", leftAlign);
        GUILayout.Label($"RPM: <color=yellow>{rpm,4:F0}</color>", leftAlign);
        GUILayout.Label($"Boost: <color={boostColor}>{boost:F2}</color> Bar", leftAlign);
        GUILayout.EndHorizontal();

        // --- ROW 2: Driver Inputs ---
        GUILayout.BeginHorizontal();
        GUILayout.Label($"T:<color=green>{inputs.Throttle*100,3:F0}%</color>", leftAlign);
        GUILayout.Label($"B:<color=red>{inputs.Brake*100,3:F0}%</color>", leftAlign);
        GUILayout.Label($"C:<color=grey>{inputs.Clutch*100,3:F0}%</color>", leftAlign);
        GUILayout.Label($"Str: {inputs.Steering * sim.steeringData.maxSteerAngle,4:F1}°", leftAlign);
        GUILayout.EndHorizontal();

        // --- ROW 3: G-Forces ---
        string longColor = Mathf.Abs(currentLongG) > 0.8f ? "yellow" : "white";
        string latColor = Mathf.Abs(currentLatG) > 0.8f ? "yellow" : "white";
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Long G: <color={longColor}>{currentLongG,5:F2}</color>", leftAlign);
        GUILayout.Label($"Lat G: <color={latColor}>{currentLatG,5:F2}</color>", leftAlign);
        GUILayout.EndHorizontal();

        // --- ROW 4: Compact Wheel Data ---
        if (showWheelData && sim.corners.Length == 4)
        {
            GUILayout.Space(2);
            GUILayout.Label("<b>Tire Load (N)  |  Long. Slip</b>", boldHeader);
            
            DrawAxleRow("FL", sim.corners[0], "FR", sim.corners[1]);
            DrawAxleRow("RL", sim.corners[2], "RR", sim.corners[3]);
        }

        GUILayout.EndArea();
    }

    // Helper to draw a left/right wheel pair on a single line
    private void DrawAxleRow(string leftName, WheelAssembly leftCol, string rightName, WheelAssembly rightCol)
    {
        GUILayout.BeginHorizontal();
        
        string lSlipC = GetSlipColorHex(leftCol.wheel.longitudinalSlip);
        GUILayout.Label($"{leftName}: {leftCol.suspension.currentNormalLoad,5:F0} | <color={lSlipC}>{leftCol.wheel.longitudinalSlip,5:F2}</color>", leftAlign);
        
        string rSlipC = GetSlipColorHex(rightCol.wheel.longitudinalSlip);
        GUILayout.Label($"{rightName}: {rightCol.suspension.currentNormalLoad,5:F0} | <color={rSlipC}>{rightCol.wheel.longitudinalSlip,5:F2}</color>", leftAlign);

        GUILayout.EndHorizontal();
    }

    private string GetSlipColorHex(float slip)
    {
        float absSlip = Mathf.Abs(slip);
        if (absSlip > 0.15f) return "#FF4444"; // Red (Slipping/Locking)
        if (absSlip > 0.08f) return "#FFFF00"; // Yellow (Peak Grip)
        return "#00FF00";                      // Green (Stable)
    }

    private void InitStyles()
    {
        if (leftAlign == null)
        {
            leftAlign = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleLeft };
            rightAlign = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleRight };
            boldHeader = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleLeft };
        }
    }
}