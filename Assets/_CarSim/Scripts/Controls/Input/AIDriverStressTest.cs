using UnityEngine;

public class AIDriverStressTest : MonoBehaviour, IVehicleInput
{
    [Header("Test Configuration")]
    public float targetLaunchSpeedKmh = 110f;
    public float slalomSpeedKmh = 60f;
    public float slalomDuration = 5f;
    public float slalomFrequency = 3f;

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

    private enum TestState { Idle, Launching, PanicBraking, ReLaunching, Slalom, Finished }
    private TestState currentState = TestState.Idle;
    
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
        Brake = 1f; // Hold brakes at idle
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
                Throttle = 1f; Brake = 0f; Steering = 0f;

                // Track 0-100 Time
                if (!reached100 && currentSpeedKmh >= 100f)
                {
                    timeTo100Kmh = Time.time - launchStartTime;
                    reached100 = true;
                    Debug.Log($"<color=cyan><b>[Benchmark]</b> 0-100 km/h: {timeTo100Kmh:F2} seconds</color>");
                }

                // Push slightly past 100 before braking
                if (currentSpeedKmh >= targetLaunchSpeedKmh)
                {
                    Debug.Log("<color=yellow><b>[Benchmark]</b> Stage 2 - 100-0 PANIC BRAKING</color>");
                    brakingStartPosition = carRb.position;
                    currentState = TestState.PanicBraking;
                }
                break;

            case TestState.PanicBraking:
                Throttle = 0f; Brake = 1f; Steering = 0f; // Slam brakes

                if (currentSpeedKmh <= 1f) // Essentially stopped
                {
                    brakingDistance = Vector3.Distance(brakingStartPosition, carRb.position);
                    Debug.Log($"<color=cyan><b>[Benchmark]</b> Braking Distance: {brakingDistance:F1} meters</color>");
                    
                    Debug.Log("<color=orange><b>[Benchmark]</b> Stage 3 - RE-LAUNCH TO SLALOM</color>");
                    currentState = TestState.ReLaunching;
                }
                break;

            case TestState.ReLaunching:
                Throttle = 0.8f; Brake = 0f; Steering = 0f;

                if (currentSpeedKmh >= slalomSpeedKmh)
                {
                    Debug.Log("<color=orange><b>[Benchmark]</b> Stage 4 - LATERAL SLALOM SWEEP</color>");
                    currentState = TestState.Slalom;
                    stateTimer = 0f;
                }
                break;

            case TestState.Slalom:
                Throttle = 0.35f; Brake = 0f; // Modulate to maintain speed
                stateTimer += Time.deltaTime;
                
                // Aggressive sine wave steering
                Steering = Mathf.Sin(stateTimer * slalomFrequency);

                if (stateTimer >= slalomDuration)
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

    private void ResetInputs()
    {
        Steering = 0f; Throttle = 0f; Brake = 0f; Clutch = 0f;
        ShiftUp = false; ShiftDown = false;
    }
}