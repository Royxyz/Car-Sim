using UnityEngine;
using System.Reflection;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

public enum TestPhase { Idle, Launch, Brake, Slalom, Skidpad, TrackRun, Finished }

public class DynamicTestDirector : MonoBehaviour
{
    [Header("Dependencies")]
    public SimulationController sim;
    
    [Header("Live Test State")]
    public TestPhase currentPhase = TestPhase.Idle;

    [Header("Track Run Settings")]
    public string trackCsvFileName = "TrackData.csv";
    
    [Header("Dynamic Racing Line")]
    [Tooltip("How aggressively the car hugs the inside of a corner (Apex).")]
    public float apexAggression = 1.2f;
    [Tooltip("How much the car swings to the OUTSIDE before a corner to open up the entry.")]
    public float entrySwingAggression = 1.0f;
    [Tooltip("How fast the AI transitions across the width of the track.")]
    public float lineSmoothingSpeed = 1.5f;
    [Tooltip("Absolute minimum distance the target point can be from the physical track edge.")]
    public float wallSafetyMargin = 2.5f; 
    
    private float smoothedApexBias = 0.5f;
    
    // GC-Friendly Struct implementation
    private struct TrackWaypoint 
    {
        public Vector3 position;
        public Vector3 forward;
        public Vector3 leftEdge;
        public Vector3 rightEdge;
    }
    
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

    private TrackWaypoint[] trackWaypoints;
    private int currentWaypointIndex = 0;
    private int totalWaypoints = 0;
    

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
    private void Awake()
    {
        LoadTrackCSV();
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
                if (currentSpeedKmh < 6.0f)
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
        Vector3 forwardUniversal = Quaternion.Euler(0, launchHeading, 0) * Vector3.forward;

        switch (currentPhase)
        {
            case TestPhase.Launch:
            case TestPhase.Brake:
                // Project a point infinitely far away on the launch heading
                return sim.rb.position + (forwardUniversal * 1000f);

            case TestPhase.Slalom:
                // Mathematically generate a Sine Wave path
                float zDistance = Vector3.Distance(phaseStartPosition, sim.rb.position) + lookaheadDistance;
                float xOffset = Mathf.Sin(zDistance * slalomFrequency) * slalomAmplitude;
                Vector3 basePoint = phaseStartPosition + (forwardUniversal * zDistance);
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
            
            case TestPhase.TrackRun:
                if (totalWaypoints == 0) return sim.rb.position + (sim.transform.forward * lookaheadDistance);

                // 1. Fast closest waypoint search
                float closestDistSq = float.MaxValue;
                int closestIdx = currentWaypointIndex;
                int searchWindow = Mathf.Min(30, totalWaypoints);
                for (int i = 0; i < searchWindow; i++)
                {
                    int checkIdx = (currentWaypointIndex + i) % totalWaypoints;
                    float distSq = (trackWaypoints[checkIdx].position - sim.rb.position).sqrMagnitude;
                    if (distSq < closestDistSq)
                    {
                        closestDistSq = distSq;
                        closestIdx = checkIdx;
                    }
                }
                currentWaypointIndex = closestIdx;

                // 2. Walk forward to find the lookahead target (Continuous Interpolation)
                float accumulatedDist = 0f;
                int targetIdx = currentWaypointIndex;
                int prevIdx = currentWaypointIndex;
                float segmentDist = 0f;

                for (int i = 0; i < 50; i++) 
                {
                    int nextIdx = (targetIdx + 1) % totalWaypoints;
                    segmentDist = Vector3.Distance(trackWaypoints[targetIdx].position, trackWaypoints[nextIdx].position);
                    
                    if (accumulatedDist + segmentDist >= lookaheadDistance)
                    {
                        prevIdx = targetIdx;
                        targetIdx = nextIdx;
                        break;
                    }
                    
                    accumulatedDist += segmentDist;
                    targetIdx = nextIdx;
                }

                float remainingDist = lookaheadDistance - accumulatedDist;
                float t = segmentDist > 0f ? Mathf.Clamp01(remainingDist / segmentDist) : 0f;

                Vector3 center = Vector3.Lerp(trackWaypoints[prevIdx].position, trackWaypoints[targetIdx].position, t);
                Vector3 left = Vector3.Lerp(trackWaypoints[prevIdx].leftEdge, trackWaypoints[targetIdx].leftEdge, t);
                Vector3 right = Vector3.Lerp(trackWaypoints[prevIdx].rightEdge, trackWaypoints[targetIdx].rightEdge, t);
                Vector3 forward = Vector3.Lerp(trackWaypoints[prevIdx].forward, trackWaypoints[targetIdx].forward, t).normalized;

                // --- 3. DYNAMIC RACING LINE (ENTRY SWING & APEX HUGGING) ---
                
                // Curvature ahead of the target (Are we approaching a corner?)
                int futureIdx = (targetIdx + 15) % totalWaypoints; 
                Vector3 toFuture = (trackWaypoints[futureIdx].position - center).normalized;
                Vector3 rightDirTrack = Vector3.Cross(Vector3.up, forward).normalized;
                float targetCurve = Vector3.Dot(toFuture, rightDirTrack); // Positive = Right Turn
                
                // Curvature at the car's current position (Are we currently IN a corner?)
                int carFutureIdx = (closestIdx + 15) % totalWaypoints;
                Vector3 carToFuture = (trackWaypoints[carFutureIdx].position - trackWaypoints[closestIdx].position).normalized;
                Vector3 carRightDir = Vector3.Cross(Vector3.up, trackWaypoints[closestIdx].forward).normalized;
                float carCurve = Vector3.Dot(carToFuture, carRightDir);

                float targetApexBias = 0.5f;

                // Logic: If the curve coming up is sharper than the curve we are currently on -> Corner Entry.
                if (Mathf.Abs(targetCurve) > Mathf.Abs(carCurve) + 0.02f)
                {
                    // Swing OUTSIDE. (If turning right (+), subtract from 0.5 to move left).
                    targetApexBias = 0.5f - (targetCurve * entrySwingAggression);
                }
                else
                {
                    // We are IN the corner (or on a straight). Hug the INSIDE.
                    targetApexBias = 0.5f + (carCurve * apexAggression);
                }

                targetApexBias = Mathf.Clamp01(targetApexBias);
                
                if (Application.isPlaying)
                    smoothedApexBias = Mathf.MoveTowards(smoothedApexBias, targetApexBias, Time.fixedDeltaTime * lineSmoothingSpeed);
                else
                    smoothedApexBias = targetApexBias;

                // --- 4. HARD CEILING WALL MARGINS ---
                float distLeft = Vector3.Distance(center, left);
                float distRight = Vector3.Distance(center, right);

                Vector3 safeLeft = Vector3.MoveTowards(left, center, Mathf.Min(wallSafetyMargin, distLeft));
                Vector3 safeRight = Vector3.MoveTowards(right, center, Mathf.Min(wallSafetyMargin, distRight));

                return Vector3.Lerp(safeLeft, safeRight, smoothedApexBias);
            default:
                return sim.rb.position + (sim.transform.forward * 10f);
        }
    }
    private void LoadTrackCSV()
    {
        string path = Path.Combine(Application.dataPath, trackCsvFileName);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[Director] No track CSV found at {path}. TrackRun phase will not work.");
            return;
        }

        // We only do this string parsing once on init, so GC spikes here are acceptable.
        string[] lines = File.ReadAllLines(path);
        if (lines.Length <= 1) return;

        totalWaypoints = lines.Length - 1;
        trackWaypoints = new TrackWaypoint[totalWaypoints]; // Allocate exactly once

        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length >= 12) // Ensure we are reading all 12 columns now
            {
                trackWaypoints[i - 1] = new TrackWaypoint
                {
                    position = new Vector3(float.Parse(cols[0]), float.Parse(cols[1]), float.Parse(cols[2])),
                    forward = new Vector3(float.Parse(cols[3]), float.Parse(cols[4]), float.Parse(cols[5])),
                    leftEdge = new Vector3(float.Parse(cols[6]), float.Parse(cols[7]), float.Parse(cols[8])),
                    rightEdge = new Vector3(float.Parse(cols[9]), float.Parse(cols[10]), float.Parse(cols[11]))
                };
            }
        }
        Debug.Log($"<color=green>[Director] Loaded {totalWaypoints} waypoints into memory.</color>");
    }
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || trackWaypoints == null || totalWaypoints == 0) return;
        if (currentPhase != TestPhase.TrackRun) return;

        int waypointsToDraw = Mathf.Min(15, totalWaypoints);

        for (int i = 0; i < waypointsToDraw; i++)
        {
            int drawIdx = (currentWaypointIndex + i) % totalWaypoints;
            int nextDrawIdx = (drawIdx + 1) % totalWaypoints;

            Vector3 wpPos = trackWaypoints[drawIdx].position;
            Vector3 nextWpPos = trackWaypoints[nextDrawIdx].position;

            // Draw track width limits in gray
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Gizmos.DrawLine(trackWaypoints[drawIdx].leftEdge, trackWaypoints[drawIdx].rightEdge);

            // Draw center line in cyan
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(wpPos, nextWpPos);
        }

        // Draw the calculated dynamic target point in green!
        Gizmos.color = Color.green;
        Vector3 dynamicTarget = GetDynamicTargetPoint(sim.GetComponent<AIDriverStressTest>().profile.baseLookaheadDistance);
        Gizmos.DrawWireSphere(dynamicTarget, 1.5f);
        Gizmos.DrawLine(sim.transform.position, dynamicTarget);
    }
}