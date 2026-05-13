using UnityEngine;

public class AIDriverStressTest : MonoBehaviour, IVehicleInput
{
    [Header("AI Architecture")]
    public DynamicTestDirector director;
    public AIDriverProfileData profile;
    public SimulationController sim;

    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }
    public float Clutch { get; private set; }
    public float Handbrake { get; private set; }
    public bool ShiftUp { get; private set; }
    public bool ShiftDown { get; private set; }

    private float lastHeadingError = 0f;
    private float lastRearSlipAngle = 0f;

    private float throttleIntegral = 0f;
    private float throttleLastError = 0f;
    
    private float brakeIntegral = 0f;
    private float brakeLastError = 0f;

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
        if (phase == TestPhase.Launch || phase == TestPhase.Brake)
        {
            float currentHeading = sim.rb.rotation.eulerAngles.y;
            float targetHeading = director.GetTargetLaunchHeading();
            float headingError = Mathf.DeltaAngle(currentHeading, targetHeading);
            float headingDerivative = (headingError - lastHeadingError) / dt;
            lastHeadingError = headingError;

            Steering = Mathf.Clamp((headingError * profile.headingHoldKp) + (headingDerivative * profile.headingHoldKd), -1f, 1f);
            return;
        }

        float rearLeftSlip = sim.corners[2].wheel.slipAngle * Mathf.Rad2Deg;
        float rearRightSlip = sim.corners[3].wheel.slipAngle * Mathf.Rad2Deg;
        float avgRearSlipAngle = (rearLeftSlip + rearRightSlip) / 2f;
        
        if (Mathf.Abs(avgRearSlipAngle) > profile.yawToleranceAngle)
        {
            float slipDerivative = (avgRearSlipAngle - lastRearSlipAngle) / dt;
            float counterSteerCommand = (avgRearSlipAngle * profile.counterSteerKp) + (slipDerivative * profile.counterSteerKd);
            Steering = Mathf.Clamp(counterSteerCommand, -1f, 1f);
        }
        else
        {
            float lookahead = profile.baseLookaheadDistance + (speed * profile.lookaheadSpeedScaling);
            Vector3 targetPoint = director.GetDynamicTargetPoint(lookahead);
            Vector3 localTarget = sim.transform.InverseTransformPoint(targetPoint);
            float steerError = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            
            float steerKp = 0.05f / Mathf.Max(1f, speed * 0.05f);
            Steering = Mathf.Clamp(steerError * steerKp, -1f, 1f);
        }

        lastRearSlipAngle = avgRearSlipAngle;
    }

    private void HandlePedals(TestPhase phase, float speed, float dt)
    {
        float optimalLongSlip = (1f / sim.corners[0].tire.tireData.longB) * profile.targetGripUtilization;

        float maxAccelSlip = Mathf.Max(Mathf.Abs(sim.corners[2].wheel.longitudinalSlip), Mathf.Abs(sim.corners[3].wheel.longitudinalSlip));
        
        float maxBrakingSlip = 0f;
        for (int i = 0; i < 4; i++)
        {
            float wheelSlip = sim.corners[i].wheel.longitudinalSlip;
            if (wheelSlip < 0f && Mathf.Abs(wheelSlip) > maxBrakingSlip) // Slip is negative when braking
            {
                maxBrakingSlip = Mathf.Abs(wheelSlip);
            }
        }

        if (phase == TestPhase.Launch)
        {
            Brake = 0f;
            Handbrake = 0f;

            float slipError = optimalLongSlip - maxAccelSlip;
            throttleIntegral += slipError * dt;
            throttleIntegral = Mathf.Clamp(throttleIntegral, -1f, 1f); 
            float throttleDerivative = (slipError - throttleLastError) / dt;
            throttleLastError = slipError;

            float baseThrottle = 1.0f; 
            float modulation = (slipError * profile.throttleAggressionKp) + (throttleIntegral * 0.1f) + (throttleDerivative * 0.05f);
            Throttle = Mathf.Clamp01(baseThrottle + modulation);
        }
        else if (phase == TestPhase.Brake)
        {
            Throttle = 0f;

            float slipError = optimalLongSlip - maxBrakingSlip;
            brakeIntegral += slipError * dt;
            brakeIntegral = Mathf.Clamp(brakeIntegral, -1f, 1f);
            
            float brakeDerivative = (slipError - brakeLastError) / dt;
            brakeLastError = slipError;

            float baseBrake = 1.0f; 
            float modulation = (slipError * profile.brakeAggressionKp) + (brakeIntegral * 0.1f) + (brakeDerivative * 0.05f);
            
            Brake = Mathf.Clamp01(baseBrake + modulation);
        }
        else if (phase == TestPhase.Slalom || phase == TestPhase.Skidpad || phase == TestPhase.TrackRun)
        {
            float maxAvailableG = 1.1f * profile.targetGripUtilization;
            float currentLatG = Mathf.Abs(sim.rb.angularVelocity.y * speed) / 9.81f; 
            float remainingLongG = Mathf.Sqrt(Mathf.Max(0f, (maxAvailableG * maxAvailableG) - (currentLatG * currentLatG)));

            Vector3 localTarget = sim.transform.InverseTransformPoint(director.GetDynamicTargetPoint(15f));
            float distanceToCorner = localTarget.magnitude;
            float cornerSharpness = Mathf.Abs(localTarget.x) / Mathf.Max(distanceToCorner, 0.1f);

            if (cornerSharpness > 0.3f && distanceToCorner < (speed * 1.5f))
            {
                Throttle = 0f;
                float requestedBrake = profile.allowTrailBraking ? Mathf.Clamp01(remainingLongG / 1.0f) : 1.0f;

                if (maxBrakingSlip > optimalLongSlip) 
                {
                    requestedBrake *= 0.5f; 
                }
                
                Brake = requestedBrake;
                
                if (profile.allowHandbrakeEntry && cornerSharpness > 0.8f && speed > 10f) Handbrake = 1.0f;
                else Handbrake = 0f;
            }
            else
            {
                Brake = 0f;
                Handbrake = 0f;

                if (maxAccelSlip > (optimalLongSlip * 1.2f))
                {
                    Throttle = Mathf.MoveTowards(Throttle, 0.5f, dt * profile.throttleAggressionKp * 10f);
                }
                else
                {
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
        Brake = 1f; 
        Handbrake = 0f;
        Clutch = 0f;
    }
}