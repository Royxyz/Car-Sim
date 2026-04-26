using UnityEngine;

[CreateAssetMenu(fileName = "NewBrakeData", menuName = "Vehicle Physics/Brake Data")]
public class BrakeData : ScriptableObject
{
    public float maxBrakeTorque = 3000f;
    
    public bool hasABS = true;
    public float absSlipThreshold = 0.15f;
    public float absReleaseMultiplier = 0.2f;
}