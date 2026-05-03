using UnityEngine;

[CreateAssetMenu(fileName = "NewSuspensionData", menuName = "Vehicle Physics/Suspension Data")]
public class SuspensionData : ScriptableObject
{
    public float restLength = 0.5f;
    public float maxTravel = 0.2f;
    public float springStiffness = 35000f;
    public float bumpDamping = 3500f;
    public float reboundDamping = 4000f;

    [Header("Bump Stops (Physical Rubber Limits)")]
    [Tooltip("How far into the travel (meters) before the physical bump stop engages. Usually slightly less than maxTravel.")]
    public float bumpStopEngagement = 0.13f; 
    
    [Tooltip("The stiffness of the rubber bump stop itself. Usually 3x to 5x stiffer than the main spring.")]
    public float bumpStopStiffness = 150000f;

    [Header("Safety Constraints")]
    public float absoluteMaxForce = 150000f;
}