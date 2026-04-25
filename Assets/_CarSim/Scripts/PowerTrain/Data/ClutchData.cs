using UnityEngine;

[CreateAssetMenu(fileName = "NewClutchData", menuName = "Vehicle Physics/Clutch Data")]
public class ClutchData : ScriptableObject
{
    [Tooltip("The maximum torque the clutch can transfer when fully locked in Nm. Should be slightly higher than peak engine torque to prevent slipping at full throttle.")]
    public float maxTorqueCapacity = 450f;

    [Tooltip("The tiny slip difference (epsilon) in rad/s. Below this speed difference, the math switches from kinetic friction (slipping) to static friction (locked).")]
    public float lockThreshold = 2.0f;
}