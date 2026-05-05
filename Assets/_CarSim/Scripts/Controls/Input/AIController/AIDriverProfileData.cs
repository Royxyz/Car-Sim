using UnityEngine;

[CreateAssetMenu(fileName = "NewDriverProfile", menuName = "Vehicle Physics/AI/Driver Profile")]
public class AIDriverProfileData : ScriptableObject
{
    [Header("Pace & Grip Utilization")]
    [Tooltip("1.0 = Rides the exact Pacejka peak. <1.0 = Cautious (brakes early). >1.0 = Overdrives the tires (Drifting/Rally).")]
    public float targetGripUtilization = 0.95f;

    [Tooltip("How far ahead the driver looks. Higher = smoother steering but cuts corners. Lower = reactive but jerky.")]
    public float baseLookaheadDistance = 15f;
    public float lookaheadSpeedScaling = 0.5f;

    [Header("Slide & Yaw Management (Oversteer)")]
    [Tooltip("The maximum rear slip angle (degrees) the driver tolerates before abandoning the racing line to counter-steer.")]
    public float yawToleranceAngle = 5.0f;
    
    [Tooltip("Proportional gain for counter-steering. Higher = faster snaps to catch a slide.")]
    public float counterSteerKp = 0.05f;
    
    [Tooltip("Derivative gain for counter-steering. Dampens the counter-steer to prevent tank-slappers (over-correcting).")]
    public float counterSteerKd = 0.01f;

    [Header("Straight-Line Stability (Top Speed Run)")]
    [Tooltip("PID settings for holding a perfectly straight heading at high speeds.")]
    public float headingHoldKp = 0.02f;
    public float headingHoldKd = 0.005f;

    [Header("Pedal Footwork (PID Control)")]
    [Tooltip("How aggressively the driver modulates the throttle to hit target slip. High = stabbing/drifting. Low = smooth/grip.")]
    public float throttleAggressionKp = 2.0f;
    
    [Tooltip("How aggressively the driver brakes. High = slams into ABS. Low = threshold braking.")]
    public float brakeAggressionKp = 5.0f;

    [Header("Advanced Techniques")]
    [Tooltip("If true, driver smoothly blends off the brakes while turning in. If false, brakes only in a straight line.")]
    public bool allowTrailBraking = true;
    
    [Tooltip("If true, driver will pull the handbrake on tight corner entries to deliberately break rear traction.")]
    public bool allowHandbrakeEntry = false;
    
    [Tooltip("If true, driver will kick the clutch to spike engine RPM and break rear traction when dropping below target slip in a drift.")]
    public bool allowClutchKicking = false;
}