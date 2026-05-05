using UnityEngine;

public class AIDriverStressTest : MonoBehaviour, IVehicleInput
{
    [Header("AI Architecture")]
    public DynamicTestDirector director;
    public AIDriverProfileData profile;
    public SimulationController sim;

    // --- IVehicleInput Implementation ---
    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Clutch { get; private set; }
    public float Handbrake { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    // --- PID Memory State ---
    private float lastHeadingError = 0f;
    private float lastRearSlipAngle = 0f;
    private float throttleIntegral = 0f;
    private float throttleLastError = 0f;

    private void FixedUpdate()
    {
        if (director == null || profile == null || sim == null) return;
        
        TestPhase currentPhase = director.GetCurrentPhase();
        if (currentPhase == TestPhase.Idle || currentPhase == TestPhase.Finished)
        {
            ResetInputs();
            return;
        }

        float speed = sim.rb.linearVelocity.magnitude;
        float dt = Time.fixedDeltaTime;

        HandleSteering(currentPhase, speed, dt);
        HandlePedals(currentPhase, speed, dt);
    }

    private void HandleSteering(TestPhase phase, float speed, float dt)
    {
        // 1. STRAIGHT LINE STABILITY (Top Speed & Panic Stop)
        if (phase == TestPhase.Launch || phase == TestPhase.Brake)
        {
            float currentHeading = sim.rb.rotation.eulerAngles.y;
            float targetHeading = director.GetTargetLaunchHeading();
            
            // Normalize error to -180 to 180 degrees
            float headingError = Mathf.DeltaAngle(currentHeading, targetHeading);
            
            float headingDerivative = (headingError - lastHeadingError) / dt;
            lastHeadingError = headingError;

            // PID calculation for steering
            Steering = Mathf.Clamp((headingError * profile.headingHoldKp) + (headingDerivative * profile.headingHoldKd), -1f, 1f);
            return;
        }

        // 2. CORNERING & OVERSTEER MANAGEMENT (Slalom & Skidpad)
        
        // Calculate the rear slip angle (Average of RL and RR wheels)
        float rearLeftSlip = sim.corners[2].wheel.slipAngle * Mathf.Rad2Deg;
        float rearRightSlip = sim.corners[3].wheel.slipAngle * Mathf.Rad2Deg;
        float avgRearSlipAngle = (rearLeftSlip + rearRightSlip) / 2f;
        
        // A. Is the car sliding past the driver's comfort zone?
        if (Mathf.Abs(avgRearSlipAngle) > profile.yawToleranceAngle)
        {
            // YAW CONTROLLER: OVERRIDE PATH AND CATCH THE SLIDE
            float slipDerivative = (avgRearSlipAngle - lastRearSlipAngle) / dt;
            
            // If rear slips right (positive angle), steer right (positive steering) to catch it.
            float counterSteerCommand = (avgRearSlipAngle * profile.counterSteerKp) + (slipDerivative * profile.counterSteerKd);
            Steering = Mathf.Clamp(counterSteerCommand, -1f, 1f);
        }
        else
        {
            // B. Normal Path Following (Pure Pursuit)
            float lookahead = profile.baseLookaheadDistance + (speed * profile.lookaheadSpeedScaling);
            Vector3 targetPoint = director.GetDynamicTargetPoint(lookahead);
            
            Vector3 localTarget = sim.transform.InverseTransformPoint(targetPoint);
            float steerError = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            
            // Standard Kp steering (scaled slightly by speed to prevent wobbling)
            float steerKp = 0.05f / Mathf.Max(1f, speed * 0.05f);
            Steering = Mathf.Clamp(steerError * steerKp, -1f, 1f);
        }

        lastRearSlipAngle = avgRearSlipAngle;
    }

    private void HandlePedals(TestPhase phase, float speed, float dt)
    {
        // Calculate peak grip targets from tire data
        float optimalLongSlip = (1f / sim.corners[2].tire.tireData.longB) * profile.targetGripUtilization;
        float currentMaxRearSlip = Mathf.Max(Mathf.Abs(sim.corners[2].wheel.longitudinalSlip), Mathf.Abs(sim.corners[3].wheel.longitudinalSlip));

        if (phase == TestPhase.Launch)
        {
            Brake = 0f;
            Handbrake = 0f;

            // THROTTLE PID: Keep rear tires exactly at the Pacejka Peak
            float slipError = optimalLongSlip - currentMaxRearSlip;
            
            throttleIntegral += slipError * dt;
            throttleIntegral = Mathf.Clamp(throttleIntegral, -1f, 1f); // Anti-windup
            
            float throttleDerivative = (slipError - throttleLastError) / dt;
            throttleLastError = slipError;

            // If we have grip to spare, pin it. If slipping, modulate aggressively.
            float baseThrottle = 1.0f; 
            float modulation = (slipError * profile.throttleAggressionKp) + (throttleIntegral * 0.1f) + (throttleDerivative * 0.05f);
            
            Throttle = Mathf.Clamp01(baseThrottle + modulation);
        }
        else if (phase == TestPhase.Brake)
        {
            Throttle = 0f;
            // Simplified ABS/Threshold braking targeting profile aggression
            Brake = Mathf.Clamp01(profile.brakeAggressionKp);
        }
        else if (phase == TestPhase.Slalom || phase == TestPhase.Skidpad)
        {
            // FRICTION CIRCLE MATH
            float maxAvailableG = 1.1f * profile.targetGripUtilization;
            float currentLatG = Mathf.Abs(sim.rb.angularVelocity.y * speed) / 9.81f; 
            float remainingLongG = Mathf.Sqrt(Mathf.Max(0f, (maxAvailableG * maxAvailableG) - (currentLatG * currentLatG)));

            Vector3 localTarget = sim.transform.InverseTransformPoint(director.GetDynamicTargetPoint(15f));
            float distanceToCorner = localTarget.magnitude;
            float cornerSharpness = Mathf.Abs(localTarget.x) / Mathf.Max(distanceToCorner, 0.1f);

            // Trail Braking Logic
            if (cornerSharpness > 0.3f && distanceToCorner < (speed * 1.5f))
            {
                Throttle = 0f;
                Brake = profile.allowTrailBraking ? Mathf.Clamp01(remainingLongG / 1.0f) : 1.0f;
                
                // Advanced: Handbrake Entry
                if (profile.allowHandbrakeEntry && cornerSharpness > 0.8f && speed > 10f)
                {
                    Handbrake = 1.0f;
                }
                else
                {
                    Handbrake = 0f;
                }
            }
            else
            {
                // Corner Exit / Steady State Traction
                Brake = 0f;
                Handbrake = 0f;

                if (currentMaxRearSlip > (optimalLongSlip * 1.2f))
                {
                    // Rear is stepping out under power. Use PID modulation to hold the slide.
                    Throttle = Mathf.MoveTowards(Throttle, 0.5f, dt * profile.throttleAggressionKp * 10f);
                }
                else
                {
                    // Power out based on remaining grip in the friction circle
                    float requestedThrottle = (remainingLongG / 1.0f);
                    Throttle = Mathf.Clamp01(requestedThrottle * profile.throttleAggressionKp);
                }
            }
        }
    }

    private void ResetInputs()
    {
        Steering = 0f;
        Throttle = 0f;
        Brake = 1f; // Hold brakes when finished
        Handbrake = 0f;
        Clutch = 0f;
    }
}