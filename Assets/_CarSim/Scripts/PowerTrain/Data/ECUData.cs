using UnityEngine;

[CreateAssetMenu(fileName = "NewECUData", menuName = "Vehicle Physics/Powertrain/ECU Data")]
public class ECUData : ScriptableObject
{
    [Header("Traction Control System (TCS)")]
    public bool enableTractionControl = true;
    
    [Tooltip("The longitudinal slip threshold before the ECU cuts power.")]
    public float tcsSlipThreshold = 0.12f;
    
    [Tooltip("How aggressively the throttle is cut when slip is detected.")]
    public float tcsAggressiveness = 10.0f;
}