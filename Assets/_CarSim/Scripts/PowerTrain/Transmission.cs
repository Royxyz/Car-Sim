using UnityEngine;

[CreateAssetMenu(fileName = "NewTransmissionData", menuName = "Vehicle Physics/Transmission Data")]
public class TransmissionData : ScriptableObject
{
    [Header("Gearing")]
    [Tooltip("Array representing the gear ratio (N) for each forward gear.")]
    public float[] forwardGears = { 3.1f, 1.8f, 1.3f, 1.0f, 0.8f };
    
    [Tooltip("The gear ratio for reverse. Usually around the same absolute ratio as 1st gear.")]
    public float reverseGear = 3.2f;
    
    [Tooltip("The differential ratio. Total multiplier is currentGear * finalDrive.")]
    public float finalDrive = 3.73f;

    [Header("Physical Characteristics")]
    [Tooltip("A multiplier representing drivetrain power loss, usually between 0.85 and 0.95.")]
    [Range(0f, 1f)]
    public float efficiency = 0.90f;

    [Tooltip("The rotational mass of the gearbox internals, used when calculating lumped mass.")]
    public float transmissionInertia = 0.15f;
}