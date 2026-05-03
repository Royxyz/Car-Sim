using UnityEngine;

public enum InductionType { NaturallyAspirated, Supercharged, Turbocharged }

[CreateAssetMenu(fileName = "NewInductionData", menuName = "Vehicle Physics/Induction Data")]
public class InductionData : ScriptableObject
{
    public InductionType type = InductionType.NaturallyAspirated;

    [Header("Boost Characteristics")]
    [Tooltip("Max absolute manifold pressure in Bar. (1.0 = NA, 1.5 = ~7 psi boost, 2.0 = ~14 psi boost)")]
    public float maxPressureBar = 1.0f;

    [Header("Supercharger Specific")]
    [Tooltip("Parasitic drag torque at redline (Nm) required to spin the belt. (Drains power)")]
    public float superchargerParasiticDrag = 30f;

    [Header("Turbocharger Specific")]
    public float turboSpoolTimeAtIdle = 1.5f;
    public float turboSpoolTimeAtRedline = 0.15f;
    public float blowOffTime = 0.1f;
}