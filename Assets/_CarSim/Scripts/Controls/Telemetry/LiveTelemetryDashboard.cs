using UnityEngine;

[RequireComponent(typeof(SimulationController))]
public class LiveTelemetryDashboard : MonoBehaviour
{
    private SimulationController sim;
    private IVehicleInput inputs;

    [Header("Dashboard Settings")]
    public int guiScale = 1;
    public bool showWheelData = true;
    public float gForceSmoothTime = 0.1f;

    // G-Force Smoothing
    private Vector3 lastVelocity;
    private float currentLongG;
    private float currentLatG;
    private float longGVelocity;
    private float latGVelocity;

    // Custom GUI Styles
    private GUIStyle windowStyle;
    private GUIStyle headerStyle;
    private GUIStyle labelStyle;
    private GUIStyle valueStyle;
    private Texture2D bgTexture;
    private Texture2D barBackground;
    private GUIStyle boxStyle;

    private bool stylesInitialized = false;

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

        currentLongG = Mathf.SmoothDamp(currentLongG, localAccel.z / 9.81f, ref longGVelocity, gForceSmoothTime);
        currentLatG = Mathf.SmoothDamp(currentLatG, localAccel.x / 9.81f, ref latGVelocity, gForceSmoothTime);

        lastVelocity = currentVel;
    }

    private void OnGUI()
    {
        if (sim == null || inputs == null) return;

        if (!stylesInitialized) InitStyles();

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(guiScale, guiScale, 1));

        GUILayout.BeginArea(new Rect(15, 15, 340, 600), windowStyle);

        
        DrawHeader();
        DrawPowertrain();
        DrawInputs();
        
        GUILayout.BeginHorizontal();
        DrawFrictionCircle();
        if (showWheelData && sim.corners.Length == 4) DrawTireGrid();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawHeader()
    {
        GUILayout.Label("VEHICLE TELEMETRY", headerStyle);
        DrawSeparator();
    }

    private void DrawPowertrain()
    {
        float speedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        float rpm = sim.powerTrain.engineRPM;
        float boost = sim.powerTrain.engine.currentManifoldPressure;
        int gear = sim.powerTrain.transmission.currentGear;
        string gearStr = gear == -1 ? "R" : gear == 0 ? "N" : gear.ToString();

        GUILayout.BeginHorizontal();
        GUILayout.Label("GEAR:", labelStyle, GUILayout.Width(50));
        GUILayout.Label(gearStr, valueStyle, GUILayout.Width(30));
        
        GUILayout.Label("SPEED:", labelStyle, GUILayout.Width(50));
        GUILayout.Label($"{speedKmh:000} km/h", valueStyle);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("RPM:", labelStyle, GUILayout.Width(50));
        GUILayout.Label($"{rpm:0000}", valueStyle, GUILayout.Width(60));
        
        GUILayout.Label("BOOST:", labelStyle, GUILayout.Width(50));
        GUI.contentColor = boost > 1.0f ? Color.yellow : Color.cyan;
        GUILayout.Label($"{boost:0.00} Bar", valueStyle);
        GUI.contentColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void DrawInputs()
    {
        GUILayout.Label("DRIVER INPUTS", headerStyle);
        
        DrawProgressBar("THR", inputs.Throttle, Color.green);
        DrawProgressBar("BRK", inputs.Brake, Color.red);
        DrawProgressBar("CLU", inputs.Clutch, new Color(0.2f, 0.6f, 1f)); // Blue
        
        float steerAngle = inputs.Steering * sim.steeringData.maxSteerAngle;
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("STR", labelStyle, GUILayout.Width(35));
        GUILayout.Label($"{steerAngle:+00.0;-00.0;00.0}°", valueStyle, GUILayout.Width(60));
        
        // Visual Steering Indicator
        Rect rect = GUILayoutUtility.GetRect(180, 10);
        GUI.DrawTexture(rect, barBackground);
        float steerCenter = rect.x + (rect.width / 2f);
        float steerX = steerCenter + (inputs.Steering * (rect.width / 2f));
        GUI.DrawTexture(new Rect(steerCenter, rect.y, 1, rect.height), Texture2D.whiteTexture); // Center tick
        GUI.DrawTexture(new Rect(steerX - 2, rect.y, 4, rect.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 0, 0);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
    }

    private void DrawFrictionCircle()
    {
        GUILayout.BeginVertical(GUILayout.Width(110));
        GUILayout.Label("G-FORCE", headerStyle);
        
        Rect rect = GUILayoutUtility.GetRect(100, 100);
        rect.width = 100; 
        GUI.DrawTexture(rect, barBackground);

        // Crosshairs
        GUI.DrawTexture(new Rect(rect.x + 50, rect.y, 1, 100), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1,1,1, 0.2f), 0, 0);
        GUI.DrawTexture(new Rect(rect.x, rect.y + 50, 100, 1), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1,1,1, 0.2f), 0, 0);

        // 2G Scale Outer Ring (Visual approximation)
        GUI.DrawTexture(new Rect(rect.x + 25, rect.y + 25, 50, 50), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1,1,1, 0.05f), 0, 0);

        // G-Force Dot
        float dotX = rect.x + 50 + (currentLatG * 25f); // Scaled so 2.0G hits the edge
        float dotY = rect.y + 50 - (currentLongG * 25f); 
        dotX = Mathf.Clamp(dotX, rect.x, rect.x + 100);
        dotY = Mathf.Clamp(dotY, rect.y, rect.y + 100);

        GUI.DrawTexture(new Rect(dotX - 3, dotY - 3, 6, 6), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.yellow, 0, 0);

        GUILayout.Label($"Long: {currentLongG:+0.00;-0.00}", labelStyle);
        GUILayout.Label($"Lat:   {currentLatG:+0.00;-0.00}", labelStyle);
        GUILayout.EndVertical();
    }

    private void DrawTireGrid()
    {
        GUILayout.BeginVertical();
        GUILayout.Label("TIRE DYNAMICS", headerStyle);
        
        GUILayout.BeginHorizontal();
        DrawTireBox("FL", sim.corners[0]);
        GUILayout.Space(5);
        DrawTireBox("FR", sim.corners[1]);
        GUILayout.EndHorizontal();
        
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        DrawTireBox("RL", sim.corners[2]);
        GUILayout.Space(5);
        DrawTireBox("RR", sim.corners[3]);
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }

    private void DrawTireBox(string label, WheelAssembly corner)
    {
        GUILayout.BeginVertical(boxStyle, GUILayout.Width(95), GUILayout.Height(55));
        
        float load = corner.suspension.currentNormalLoad;
        float longSlip = corner.wheel.longitudinalSlip;
        float latSlip = corner.wheel.slipAngle * Mathf.Rad2Deg;

        // Color coding based on grip saturation
        Color statusColor = Color.green;
        if (Mathf.Abs(longSlip) > 0.12f || Mathf.Abs(latSlip) > 6f) statusColor = Color.yellow;
        if (Mathf.Abs(longSlip) > 0.20f || Mathf.Abs(latSlip) > 10f) statusColor = new Color(1f, 0.2f, 0.2f); // Red
        if (!corner.contact.isGrounded) statusColor = Color.gray;

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b>{label}</b>", labelStyle);
        GUI.DrawTexture(new Rect(GUILayoutUtility.GetLastRect().x + 75, GUILayoutUtility.GetLastRect().y + 5, 8, 8), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, statusColor, 0, 0);
        GUILayout.EndHorizontal();

        GUILayout.Label($"Ld: {load:0000} N", labelStyle);
        GUILayout.Label($"Sl: {longSlip:+0.00} | {latSlip:+0.0}°", labelStyle);
        
        GUILayout.EndVertical();
    }

    private void DrawProgressBar(string label, float value, Color color)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle, GUILayout.Width(35));
        
        Rect rect = GUILayoutUtility.GetRect(200, 12);
        GUI.DrawTexture(rect, barBackground);
        
        if (value > 0.01f)
        {
            Rect fill = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height);
            GUI.DrawTexture(fill, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, color, 0, 0);
        }
        
        GUILayout.Label($"{(value * 100):000}%", valueStyle, GUILayout.Width(40));
        GUILayout.EndHorizontal();
        GUILayout.Space(2);
    }

    private void DrawSeparator()
    {
        GUILayout.Space(5);
        Rect rect = GUILayoutUtility.GetRect(310, 2);
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(1,1,1, 0.1f), 0, 0);
        GUILayout.Space(5);
    }

    private void InitStyles()
    {
        bgTexture = MakeTex(2, 2, new Color(0.08f, 0.08f, 0.09f, 0.90f));
        barBackground = MakeTex(2, 2, new Color(0.2f, 0.2f, 0.2f, 0.8f));

        windowStyle = new GUIStyle();
        windowStyle.normal.background = bgTexture;
        windowStyle.padding = new RectOffset(15, 15, 15, 15);

        headerStyle = new GUIStyle();
        headerStyle.fontSize = 12;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        headerStyle.margin = new RectOffset(0, 0, 0, 5);

        labelStyle = new GUIStyle();
        labelStyle.fontSize = 11;
        labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        labelStyle.richText = true;

        valueStyle = new GUIStyle();
        valueStyle.fontSize = 11;
        valueStyle.fontStyle = FontStyle.Bold;
        valueStyle.normal.textColor = Color.cyan;
        valueStyle.alignment = TextAnchor.MiddleRight;

        boxStyle = new GUIStyle();
        boxStyle.normal.background = barBackground;
        boxStyle.padding = new RectOffset(5, 5, 5, 5);


        stylesInitialized = true;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}