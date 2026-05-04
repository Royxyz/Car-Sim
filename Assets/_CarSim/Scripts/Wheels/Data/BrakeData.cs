using UnityEngine;

[CreateAssetMenu(fileName = "NewBrakeData", menuName = "Vehicle Physics/Brake Data")]
public class BrakeData : ScriptableObject
{
    public float maxBrakeTorque = 3000f;
    
    [Tooltip("Torque applied when the handbrake is pulled. High enough to lock the rear wheels instantly.")]
    public float maxHandbrakeTorque = 4000f; 
    
    public bool hasABS = true;
    public float absSlipThreshold = 0.15f;
    public float absReleaseMultiplier = 0.2f;
}