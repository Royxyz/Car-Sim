using UnityEngine;

[CreateAssetMenu(fileName = "NewSuspensionData", menuName = "Vehicle Physics/Suspension Data")]
public class SuspensionData : ScriptableObject
{
    public float restLength = 0.5f;
    public float maxTravel = 0.2f;
    public float springStiffness = 35000f;
    public float bumpDamping = 3500f;
    public float reboundDamping = 4000f;

    [Header("Safety Constraints")]
    [Tooltip("Maximum allowed vertical force to prevent physics explosions (e.g., 150000 for cars, much higher for trucks).")]
    public float absoluteMaxForce = 150000f;
}