using UnityEngine;

public enum TestPhase { Idle, Launch, Brake, Slalom, Skidpad, Finished }

public class DynamicTestDirector : MonoBehaviour
{
    [Header("Dependencies")]
    public SimulationController sim;
    
    [Header("Live Test State")]
    public TestPhase currentPhase = TestPhase.Idle;
    
    [Header("Benchmark Metrics")]
    public float peakSpeedKmh;
    public float brakingDistanceMeters;
    public float maxSustainedLateralG;
    public float timeTo100Kmh;
    public bool stopLogging = false;

    // Internal State Tracking
    private Vector3 phaseStartPosition;
    private float phaseTimer;
    private float launchHeading; // Used for the PID straight-line test
    private Vector3 lastVelocity;
    
    // Slalom State
    private float slalomFrequency = 0.05f; // How fast the sine wave oscillates
    private float slalomAmplitude = 12f;   // How wide the gates are

    // Skidpad State
    private Vector3 skidpadCenter;
    private const float SKIDPAD_RADIUS = 50f;
    private float flatlineTimer = 0f;
    

    public void StartBenchmark()
    {
        currentPhase = TestPhase.Launch;
        phaseStartPosition = sim.rb.position;
        launchHeading = sim.rb.rotation.eulerAngles.y;
        lastVelocity = sim.rb.linearVelocity;
        
        peakSpeedKmh = 0f;
        timeTo100Kmh = 0f;
        phaseTimer = 0f;
        
        Debug.Log("<color=green><b>[Director]</b> Test Commenced: Phase 1 (Launch & V-Max)</color>");
    }

    private void FixedUpdate()
    {
        if (currentPhase == TestPhase.Idle || currentPhase == TestPhase.Finished) return;

        phaseTimer += Time.fixedDeltaTime;
        float currentSpeedKmh = sim.rb.linearVelocity.magnitude * 3.6f;
        
        // Acceleration calculation for V-Max detection
        Vector3 currentVel = sim.rb.linearVelocity;
        float dt = Time.fixedDeltaTime;
        Vector3 localAccel = sim.transform.InverseTransformDirection((currentVel - lastVelocity) / dt);
        float longG = localAccel.z / 9.81f;
        lastVelocity = currentVel;

        // State Machine Logic
        switch (currentPhase)
        {
            case TestPhase.Launch:
                if (currentSpeedKmh >= 100f && timeTo100Kmh == 0f) timeTo100Kmh = phaseTimer;
                
                // Did we set a new top speed this frame?
                if ((currentSpeedKmh - peakSpeedKmh) > 0.1f) 
                {
                    peakSpeedKmh = currentSpeedKmh;
                    flatlineTimer = 0f;
                    //Debug.Log((currentSpeedKmh - peakSpeedKmh)); // Reset the timer because we are still accelerating!
                }
                else if (peakSpeedKmh > 100f) 
                {

                    flatlineTimer += dt;
                    Debug.Log(flatlineTimer);

                    // If 3 seconds pass without setting a new top speed record, we're done.
                    if (flatlineTimer > 2.0f) 
                    {
                        Debug.Log($"<color=cyan><b>[Director]</b> V-Max Confirmed: {peakSpeedKmh:F1} km/h. Initiating Panic Stop.</color>");
                        TransitionToPhase(TestPhase.Brake);
                    }
                }
                break;

            case TestPhase.Brake:
                // Exit Condition: Car comes to a complete halt
                if (currentSpeedKmh < 1.0f)
                {
                    brakingDistanceMeters = Vector3.Distance(phaseStartPosition, sim.rb.position);
                    Debug.Log($"<color=cyan><b>[Director]</b> Braking Complete. Distance: {brakingDistanceMeters:F1}m. Initiating Slalom.</color>");
                    TransitionToPhase(TestPhase.Slalom);
                }
                break;

            case TestPhase.Slalom:
                float distanceIntoSlalom = Vector3.Distance(phaseStartPosition, sim.rb.position);
                
                // Exit Condition: Car has driven 300 meters through the slalom
                if (distanceIntoSlalom > 300f)
                {
                    Debug.Log("<color=cyan><b>[Director]</b> Slalom Complete. Initiating Steady-State Skidpad.</color>");
                    TransitionToPhase(TestPhase.Skidpad);
                }
                break;

            case TestPhase.Skidpad:
                float latG = Mathf.Abs(localAccel.x / 9.81f);
                if (latG > maxSustainedLateralG) maxSustainedLateralG = latG;

                // Check if car has spun out (velocity dropped drastically while on skidpad)
                if (phaseTimer > 5f && currentSpeedKmh < 20f)
                {
                    Debug.Log($"<color=red><b>[Director]</b> Car Spun Out. Max Sustained G: {maxSustainedLateralG:F2}G. Test Concluded.</color>");
                    currentPhase = TestPhase.Finished;
                }
                else if (phaseTimer > 30f)
                {
                    Debug.Log($"<color=red><b>[Director]</b> Skidpad Complete. Max Sustained G: {maxSustainedLateralG:F2}G. Test Concluded.</color>");
                    currentPhase = TestPhase.Finished;
                    stopLogging = true;
                    
                }
                break;
        }
    }

    private void TransitionToPhase(TestPhase newPhase)
    {
        currentPhase = newPhase;
        phaseStartPosition = sim.rb.position;
        phaseTimer = 0f;

        if (newPhase == TestPhase.Slalom)
        {
            launchHeading = sim.rb.rotation.eulerAngles.y;
        }
        else if (newPhase == TestPhase.Skidpad)
        {
            // Position the skidpad center exactly 50m to the left of the car's entry point
            skidpadCenter = sim.rb.position + (-sim.transform.right * SKIDPAD_RADIUS);
        }
    }

    // --- The Interface for the AI Driver ---

    /// <summary>
    /// The AI calls this to know what its macro-objective is right now.
    /// </summary>
    public TestPhase GetCurrentPhase() => currentPhase;

    /// <summary>
    /// The AI calls this to get its exact steering heading for the PID straight-line test.
    /// </summary>
    public float GetTargetLaunchHeading() => launchHeading;

    /// <summary>
    /// The AI calls this to get its spatial target for cornering tests.
    /// </summary>
    public Vector3 GetDynamicTargetPoint(float lookaheadDistance)
    {
        Vector3 forward = Quaternion.Euler(0, launchHeading, 0) * Vector3.forward;

        switch (currentPhase)
        {
            case TestPhase.Launch:
            case TestPhase.Brake:
                // Project a point infinitely far away on the launch heading
                return sim.rb.position + (forward * 1000f);

            case TestPhase.Slalom:
                // Mathematically generate a Sine Wave path
                float zDistance = Vector3.Distance(phaseStartPosition, sim.rb.position) + lookaheadDistance;
                float xOffset = Mathf.Sin(zDistance * slalomFrequency) * slalomAmplitude;
                Vector3 basePoint = phaseStartPosition + (forward * zDistance);
                Vector3 rightDir = Quaternion.Euler(0, launchHeading, 0) * Vector3.right;
                return basePoint + (rightDir * xOffset);

            case TestPhase.Skidpad:
                // Mathematically project a point around the circumference of the circle
                // We advance the angle based on the lookahead distance to lead the car around
                float angularLead = (lookaheadDistance / SKIDPAD_RADIUS); 
                
                // Calculate current angle relative to center
                Vector3 dirFromCenter = (sim.rb.position - skidpadCenter).normalized;
                float currentAngle = Mathf.Atan2(dirFromCenter.z, dirFromCenter.x);
                
                float targetAngle = currentAngle + angularLead; // Leads counter-clockwise
                
                float targetX = skidpadCenter.x + (Mathf.Cos(targetAngle) * SKIDPAD_RADIUS);
                float targetZ = skidpadCenter.z + (Mathf.Sin(targetAngle) * SKIDPAD_RADIUS);
                
                return new Vector3(targetX, sim.rb.position.y, targetZ);

            default:
                return sim.rb.position + (sim.transform.forward * 10f);
        }
    }
}