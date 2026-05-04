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

    [Header("Turbo Dynamics")]
    [Tooltip("The RPM where the turbo starts making positive pressure.")]
    public float boostThresholdRPM = 2500f; 
    [Tooltip("The RPM where the turbo can physically generate its maximum rated boost.")]
    public float optimalBoostRPM = 4500f;   
    [Tooltip("Base speed of the spool. Higher RPM multiplies this.")]
    public float turboSpoolRate = 2.5f;     
    [Tooltip("How fast the wastegate/BOV dumps pressure when lifting off.")]
    public float blowOffRate = 15f;
}