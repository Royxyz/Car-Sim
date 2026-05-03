using UnityEngine;

[CreateAssetMenu(fileName = "TransmissionData", menuName = "Vehicle Physics/Transmission Data")]
public class TransmissionData : ScriptableObject
{
    [Header("Gearing")]
    [Tooltip("Array representing the gear ratio (N) for each forward gear.")]
    public float[] forwardGears = { 3.636f, 2.235f, 1.590f, 1.137f, 0.971f, 0.756f };
    
    [Tooltip("The gear ratio for reverse.")]
    public float reverseGear = 3.545f;
    
    [Tooltip("The differential ratio.")]
    public float finalDrive = 3.900f;

    [Header("Physical Characteristics")]
    [Tooltip("A multiplier representing drivetrain power loss. Symmetrical AWD has more parasitic loss than 2WD, typically around 15-20%.")]
    [Range(0f, 1f)]
    public float efficiency = 0.80f; 

    [Tooltip("The rotational mass of the gearbox internals.")]
    public float transmissionInertia = 0.20f;
}