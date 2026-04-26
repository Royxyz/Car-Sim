using UnityEngine;

[CreateAssetMenu(fileName = "NewWheelData", menuName = "Vehicle Physics/Wheel Data")]
public class WheelData : ScriptableObject
{
    public float mass = 20f;
    public float radius = 0.33f;
    public float inertia = 1.2f;
}