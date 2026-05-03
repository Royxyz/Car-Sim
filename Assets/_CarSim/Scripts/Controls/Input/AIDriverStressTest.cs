using UnityEngine;

public class AIDriverStressTest : MonoBehaviour, IVehicleInput
{
    [Header("Test Configuration")]
    public float targetLaunchSpeedKmh = 120f; 
    public float slalomSpeedKmh = 80f; 
    public float slalomDuration = 6f;
    public float slalomFrequency = 2.5f;
    public float topSpeedRunDuration = 8f; 

    [Header("AI Control Parameters")]
    [Tooltip("Proportional gain for high-speed straight-line steering correction. Too high = wobbles. Too low = drifts.")]
    public float steeringKp = 0.03f; 
    private float targetHeading;

    [Header("Dependencies")]
    public AdvancedTelemetryLogger telemetryLogger;
    public Rigidbody carRb;

    // --- IVehicleInput Implementation ---
    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Clutch { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    public enum TestState { Idle, Launching, PanicBraking, ReLaunching, Slalom, TopSpeedRun, Finished }
    public TestState currentState { get; private set; } = TestState.Idle;
    
    // --- Benchmark Tracking ---
    private float stateTimer = 0f;
    private float launchStartTime = 0f;
    private Vector3 brakingStartPosition;
    
    public float timeTo100Kmh { get; private set; } = 0f;
    public float brakingDistance { get; private set; } = 0f;
    private bool reached100 = false;

    private float currentSpeedKmh => carRb.linearVelocity.magnitude * 3.6f;
    private CarController controls;

    private void Awake() { controls = new CarController(); }
    private void OnEnable() { controls.Driving.Enable(); }
    private void OnDisable() { controls.Driving.Disable(); }

    private void Start()
    {
        ResetInputs();
        Brake = 1f; 
    }

    private void Update()
    {
        if (currentState == TestState.Idle && controls.Driving.Throttle.ReadValue<float>() > 0.5f)
        {
            StartStressTest();
        }

        RunStateMachine();
    }

    private void StartStressTest()
    {
        Debug.Log("<color=green><b>[Benchmark]</b> Test Initiated: Stage 1 - 0-100 LAUNCH</color>");
        
        // Lock in the starting heading for the Kp controller
        targetHeading = carRb.rotation.eulerAngles.y;
        
        currentState = TestState.Launching;
        launchStartTime = Time.time;
        
        if (telemetryLogger != null) telemetryLogger.StartLogging();
    }

    private void RunStateMachine()
    {
        switch (currentState)
        {
            case TestState.Idle:
                break;

            case TestState.Launching:
                Throttle = 1f; Brake = 0f; 
                MaintainHeading(); // Replaces Steering = 0f;

                if (!reached100 && currentSpeedKmh >= 100f)
                {
                    timeTo100Kmh = Time.time - launchStartTime;
                    reached100 = true;
                    Debug.Log($"<color=cyan><b>[Benchmark]</b> 0-100 km/h: {timeTo100Kmh:F2} seconds</color>");
                }

                if (currentSpeedKmh >= targetLaunchSpeedKmh)
                {
                    Debug.Log("<color=yellow><b>[Benchmark]</b> Stage 2 - 100-0 PANIC BRAKING</color>");
                    brakingStartPosition = carRb.position;
                    currentState = TestState.PanicBraking;
                }
                break;

            case TestState.PanicBraking:
                Throttle = 0f; Brake = 1f; 
                MaintainHeading(); 

                if (currentSpeedKmh <= 2f) 
                {
                    brakingDistance = Vector3.Distance(brakingStartPosition, carRb.position);
                    Debug.Log($"<color=cyan><b>[Benchmark]</b> Braking Distance: {brakingDistance:F1} meters</color>");
                    
                    Debug.Log("<color=orange><b>[Benchmark]</b> Stage 3 - RE-LAUNCH TO SLALOM</color>");
                    currentState = TestState.ReLaunching;
                }
                break;

            case TestState.ReLaunching:
                Throttle = 0.8f; Brake = 0f; 
                MaintainHeading();

                if (currentSpeedKmh >= slalomSpeedKmh)
                {
                    Debug.Log("<color=orange><b>[Benchmark]</b> Stage 4 - LATERAL SLALOM SWEEP</color>");
                    currentState = TestState.Slalom;
                    stateTimer = 0f;
                }
                break;

            case TestState.Slalom:
                Throttle = 0.6f; Brake = 0f; 
                stateTimer += Time.deltaTime;
                
                Steering = Mathf.Sin(stateTimer * slalomFrequency);

                if (stateTimer >= slalomDuration)
                {
                    Debug.Log("<color=magenta><b>[Benchmark]</b> Stage 5 - TOP SPEED AERO RUN</color>");
                    // Update target heading so the AI goes straight from wherever the slalom spit it out
                    targetHeading = carRb.rotation.eulerAngles.y; 
                    currentState = TestState.TopSpeedRun;
                    stateTimer = 0f;
                }
                break;

            case TestState.TopSpeedRun:
                Throttle = 1f; Brake = 0f; 
                MaintainHeading();
                stateTimer += Time.deltaTime;

                if (stateTimer >= topSpeedRunDuration)
                {
                    Debug.Log("<color=red><b>[Benchmark]</b> Test Complete. Generating Report.</color>");
                    currentState = TestState.Finished;
                    
                    if (telemetryLogger != null) telemetryLogger.StopLoggingAndSave(this);
                }
                break;

            case TestState.Finished:
                Throttle = 0f; Brake = 1f; Steering = 0f;
                break;
        }
    }

    // The Kp Feedback Loop
    private void MaintainHeading()
    {
        float currentHeading = carRb.rotation.eulerAngles.y;
        
        // DeltaAngle automatically handles the 360 to 0 degree wrap-around
        float error = Mathf.DeltaAngle(currentHeading, targetHeading);
        
        // Calculate proportional steering input and clamp it between -1 (Left) and 1 (Right)
        Steering = Mathf.Clamp(error * steeringKp, -1f, 1f);
    }

    private void ResetInputs()
    {
        Steering = 0f; Throttle = 0f; Brake = 0f; Clutch = 0f;
        ShiftUp = false; ShiftDown = false;
    }
}