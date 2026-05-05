using UnityEngine;
using System.Collections.Generic;

public class AIDriverStressTest : MonoBehaviour, IVehicleInput
{
    [Header("Dependencies")]
    public AdvancedTelemetryLogger telemetryLogger;
    public SimulationController sim;

    [Header("AI Tuning")]
    public float lookaheadDistance = 15f;
    public float lookaheadSpeedScaling = 0.5f;
    public float steeringKp = 0.05f;
    public float targetGripUtilization = 0.95f; // Pushes to 95% of the tire's physical limit

    // --- IVehicleInput Implementation ---
    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Clutch { get; private set; }
    public float Handbrake { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    public List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isTesting = false;

    private void Start()
    {
        GenerateBenchmarkCircuit();
        Invoke("BeginTest", 2.0f); // Give the car 2 seconds to settle on the suspension
    }

    private void GenerateBenchmarkCircuit()
    {
        Vector3 start = sim.rb.position;
        // 1. The Launch Straight (400m - Tests 0-100, Top Speed, Aero Drag)
        waypoints.Add(start + new Vector3(0, 0, 400));
        // 2. Heavy Trail Braking into Hairpin (Tests ABS, Damper Dive, Bump Steer)
        waypoints.Add(start + new Vector3(-20, 0, 420));
        waypoints.Add(start + new Vector3(-40, 0, 400));
        waypoints.Add(start + new Vector3(-40, 0, 380));
        // 3. Transient Slalom Section (Tests ARBs, Roll inertia, Yaw response)
        waypoints.Add(start + new Vector3(-20, 0, 340));
        waypoints.Add(start + new Vector3(-60, 0, 300));
        waypoints.Add(start + new Vector3(-20, 0, 260));
        waypoints.Add(start + new Vector3(-60, 0, 220));
        // 4. Steady-State Sweeper (Tests Max Lateral G, Tire Load Dropoff, Camber Gain)
        waypoints.Add(start + new Vector3(-100, 0, 150));
        waypoints.Add(start + new Vector3(-150, 0, 100));
        waypoints.Add(start + new Vector3(-150, 0, 50));
        waypoints.Add(start + new Vector3(-100, 0, 0));
        // 5. Return to start
        waypoints.Add(start);
    }

    private void BeginTest()
    {
        Debug.Log("<color=green><b>[AI]</b> Test Initiated: Commencing Pure Pursuit Circuit.</color>");
        isTesting = true;
        if (telemetryLogger != null) telemetryLogger.StartLogging();
    }

    private void FixedUpdate()
    {
        if (!isTesting) return;
        
        float speed = sim.rb.linearVelocity.magnitude;
        Vector3 currentPos = sim.rb.position;

        // 1. Waypoint Progression
        if (Vector3.Distance(currentPos, waypoints[currentWaypointIndex]) < 10f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Count)
            {
                FinishTest();
                return;
            }
        }

        // 2. Pure Pursuit Steering
        Vector3 targetPoint = waypoints[currentWaypointIndex];
        float dynamicLookahead = lookaheadDistance + (speed * lookaheadSpeedScaling);
        Vector3 localTarget = sim.transform.InverseTransformPoint(targetPoint);
        
        // Calculate the arc required to hit the lookahead point
        float steerError = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        Steering = Mathf.Clamp(steerError * steeringKp, -1f, 1f);

        // 3. Friction Circle & Slip Management (The AI Brain)
        ManageTractionAndBraking(localTarget, speed);
    }

    private void ManageTractionAndBraking(Vector3 localTarget, float speed)
    {
        // How sharp is the corner approaching?
        float distanceToCorner = localTarget.magnitude;
        float cornerSharpness = Mathf.Abs(localTarget.x) / Mathf.Max(distanceToCorner, 0.1f); 
        
        // Assume maximum physical tire grip is approx 1.1G (from Pacejka)
        float maxAvailableG = 1.1f * targetGripUtilization;
        
        // Calculate current lateral G used by cornering
        Vector3 localAccel = sim.transform.InverseTransformDirection((sim.rb.linearVelocity - sim.rb.position) / Time.fixedDeltaTime); // Crude estimate
        float currentLatG = Mathf.Abs(sim.rb.angularVelocity.y * speed) / 9.81f; 

        // Friction Circle Equation: remaining grip for acceleration/braking
        float remainingLongG = Mathf.Sqrt(Mathf.Max(0f, (maxAvailableG * maxAvailableG) - (currentLatG * currentLatG)));

        if (cornerSharpness > 0.3f && distanceToCorner < (speed * 2f)) 
        {
            // Trail Braking logic: Smoothly trade brake for steering
            Throttle = 0f;
            Brake = Mathf.Clamp01(remainingLongG / 1.0f); // Blend brake off as lateral G builds
        }
        else
        {
            // Optimal Launch & Touge Acceleration logic
            Brake = 0f;
            
            // 1. Calculate how much the rear is slipping
            float maxSlip = Mathf.Max(Mathf.Abs(sim.corners[2].wheel.longitudinalSlip), Mathf.Abs(sim.corners[3].wheel.longitudinalSlip));
            
            // 2. The mathematical peak of grip. (For longB = 10, this is 0.10 slip)
            float mathematicalPeakSlip = 1f / sim.corners[2].tire.tireData.longB; 
            
            // 3. TOUGE TUNE: Allow the AI to push 80% past the peak grip before panicking!
            float tougeSlipLimit = mathematicalPeakSlip * 1.8f; 

            if (maxSlip > tougeSlipLimit)
            {
                // Soft rev-limiter style throttle cut instead of slamming it shut
                // Drops throttle to 60% to maintain the slide, rather than 20% to kill it.
                Throttle = Mathf.MoveTowards(Throttle, 0.6f, Time.fixedDeltaTime * 10f); 
            }
            else
            {
                // Overdrive the friction circle slightly to initiate power-on oversteer exiting corners
                float aggressiveThrottleRequest = (remainingLongG / 1.0f) * 1.2f; 
                Throttle = Mathf.Clamp01(aggressiveThrottleRequest); 
            }
        }
    }

    private void FinishTest()
    {
        isTesting = false;
        Throttle = 0f;
        Brake = 1f;
        Steering = 0f;
        Debug.Log("<color=red><b>[AI]</b> Circuit Complete. Stopping.</color>");
        if (telemetryLogger != null) telemetryLogger.StopLoggingAndSave();
    }
}