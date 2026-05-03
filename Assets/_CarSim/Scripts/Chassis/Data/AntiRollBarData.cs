using UnityEngine;

[CreateAssetMenu(fileName = "NewARBData", menuName = "Vehicle Physics/AntiRollBar Data")]
public class AntiRollBarData : ScriptableObject
{
    [Header("Front Anti Roll Bar")]
    public float frontAntiRoll = 5000f;

    [Header("Rear Anti Roll Bar")]
    public float rearAntiRoll = 3000f;
}