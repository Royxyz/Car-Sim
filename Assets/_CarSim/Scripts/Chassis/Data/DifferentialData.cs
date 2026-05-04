using UnityEngine;

public enum DifferentialType { Open, Locked, LimitedSlip }

[CreateAssetMenu(fileName = "NewDifferentialData", menuName = "Vehicle Physics/Differential Data")]
public class DifferentialData : ScriptableObject
{
    public DifferentialType diffType = DifferentialType.Open;
    public float gearRatio = 1.0f;
    public float inertia = 0.05f;
    
    [Range(0f, 1f)] 
    public float preloadLSD = 0.1f;
    
    public float lockingFriction = 50f;
    public float lockingStiffness = 5000f;

    [Header("Coast Characteristics")]
    [Tooltip("Multiplier for the locking force when off-throttle. Lower values (e.g., 0.3) allow the outside wheel to spin freely on turn-in, reducing understeer.")]
    [Range(0f, 1f)]
    public float coastLockingMultiplier = 0.4f;
}