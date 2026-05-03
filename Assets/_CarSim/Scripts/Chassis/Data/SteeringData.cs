using UnityEngine;

[CreateAssetMenu(fileName = "NewSteeringData", menuName = "Vehicle Physics/Steering Data")]
public class SteeringData : ScriptableObject
{
    [Header("Steering Angles")]
    public float maxSteerAngle = 35f;

    [Header("Ackermann Geometry")]
    [Tooltip("Multiplier for the inside wheel (turns sharper).")]
    public float ackermannInnerMultiplier = 1.15f;
    
    [Tooltip("Multiplier for the outside wheel (turns less).")]
    public float ackermannOuterMultiplier = 0.85f;
}