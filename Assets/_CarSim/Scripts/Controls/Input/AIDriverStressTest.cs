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
    [Tooltip("Proportional gain for high-speed straight-line steering correction.")]
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
                // 1. Calculate required steering first
                MaintainHeading(); 
                
                // 2. Traction Control: Lift throttle if fighting torque steer
                Throttle = 1f - (Mathf.Abs(Steering) * 0.7f); 
                Brake = 0f; 

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
                MaintainHeading(); 
                
             
                Brake = Mathf.Clamp01(1f - Mathf.Abs(Steering));
                Throttle = 0f; 

                if (currentSpeedKmh <= 2f) 
                {
                    brakingDistance = Vector3.Distance(brakingStartPosition, carRb.position);
                    Debug.Log($"<color=cyan><b>[Benchmark]</b> Braking Distance: {brakingDistance:F1} meters</color>");
                    
                    Debug.Log("<color=orange><b>[Benchmark]</b> Stage 3 - RE-LAUNCH TO SLALOM</color>");
                    currentState = TestState.ReLaunching;
                }
                break;

            case TestState.ReLaunching:
                MaintainHeading();
                Throttle = 0.8f - (Mathf.Abs(Steering) * 0.5f); 
                Brake = 0f; 

                if (currentSpeedKmh >= slalomSpeedKmh)
                {
                    Debug.Log("<color=orange><b>[Benchmark]</b> Stage 4 - LATERAL SLALOM SWEEP</color>");
                    currentState = TestState.Slalom;
                    stateTimer = 0f;
                }
                break;

            case TestState.Slalom:
                stateTimer += Time.deltaTime;
                
                // 4. Smooth analog steering input
                Steering = Mathf.Sin(stateTimer * slalomFrequency);
                
                // 5. Power Oversteer Management: Lift throttle smoothly at peak steering angles
                Throttle = Mathf.Lerp(1.0f, 0.1f, Mathf.Abs(Steering));
                Brake = 0f; 

                if (stateTimer >= slalomDuration)
                {
                    Debug.Log("<color=magenta><b>[Benchmark]</b> Stage 5 - TOP SPEED AERO RUN</color>");
                    targetHeading = carRb.rotation.eulerAngles.y; 
                    currentState = TestState.TopSpeedRun;
                    stateTimer = 0f;
                }
                break;

            case TestState.TopSpeedRun:
                MaintainHeading();
                
                // Prevent violent high-speed overcorrection
                Throttle = 1f - (Mathf.Abs(Steering) * 0.4f); 
                Brake = 0f; 
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

    private void MaintainHeading()
    {
        float currentHeading = carRb.rotation.eulerAngles.y;
        float error = Mathf.DeltaAngle(currentHeading, targetHeading);
        Steering = Mathf.Clamp(error * steeringKp, -1f, 1f);
    }

    private void ResetInputs()
    {
        Steering = 0f; Throttle = 0f; Brake = 0f; Clutch = 0f;
        ShiftUp = false; ShiftDown = false;
    }
}