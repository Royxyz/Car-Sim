using UnityEngine;

[CreateAssetMenu(fileName = "NewSuspensionData", menuName = "Vehicle Physics/Suspension Data")]
public class SuspensionData : ScriptableObject
{
    [Header("Geometry (Ride Height Centric)")]
    [Tooltip("The exact distance from the mount to the wheel center when sitting still.")]
    public float targetRideHeight = 0.46f;

    [Tooltip("How far the wheel can compress UP from the ride height.")]
    public float bumpTravel = 0.15f;

    [Tooltip("How far the wheel can hang DOWN from the ride height when airborne.")]
    public float droopTravel = 0.10f;       

    [Header("Spring & Dampers")]
    public float springStiffness = 35000f;
    public float bumpDamping = 3500f;
    public float reboundDamping = 4000f;

    [Header("Bump Stops")]
    public float bumpStopGap = 0.02f; 
    public float bumpStopStiffness = 150000f;
    public float absoluteMaxForce = 150000f;

    [Header("Kinematics")]
    [Tooltip("Degrees of negative camber gained per meter of suspension compression. Mimics Double Wishbone arcs.")]
    public float camberGainPerMeter = 12f; 
    
    [Tooltip("Degrees of toe change per meter of compression (Bump Steer). Mimics tie-rod arc discrepancies. Positive = Toe Out under compression.")]
    public float bumpSteerPerMeter = 3f;
}