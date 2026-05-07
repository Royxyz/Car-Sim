using UnityEngine;

public enum DifferentialType { Open, Locked, LimitedSlip }

[CreateAssetMenu(fileName = "NewDifferentialData", menuName = "Vehicle Physics/Differential Data")]
public class DifferentialData : ScriptableObject
{
    public DifferentialType diffType = DifferentialType.Open;

    [Header("Slip Tolerance (rad/s)")]
    public float slipTolerance = 2.0f; 

    public float gearRatio = 1.0f;
    public float inertia = 0.05f;
    
    [Header("Torque Split")]
    [Tooltip("0.5 is 50/50. For a center diff, 0.2 means 20% to Front (Left) and 80% to Rear (Right).")]
    [Range(0f, 1f)]
    public float powerBias = 0.5f;
    
    [Range(0f, 1f)] 
    public float preloadLSD = 0.1f;
    
    public float lockingFriction = 50f;
    public float lockingStiffness = 5000f;

    [Header("Coast Characteristics")]
    [Tooltip("Multiplier for the locking force when off-throttle. Lower values allow the outside wheel to spin freely on turn-in.")]
    [Range(0f, 1f)]
    public float coastLockingMultiplier = 0.4f;
}