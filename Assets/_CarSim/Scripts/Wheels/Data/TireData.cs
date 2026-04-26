using UnityEngine;

[CreateAssetMenu(fileName = "NewTireData", menuName = "Vehicle Physics/Tire Data")]
public class TireData : ScriptableObject
{
    public float frictionMultiplier = 1.0f;
    public float rollingResistance = 0.015f;

    public float longB = 10f;
    public float longC = 1.9f;
    public float longD = 1f;
    public float longE = 0.97f;

    public float latB = 10f;
    public float latC = 1.9f;
    public float latD = 1f;
    public float latE = 0.97f;
}