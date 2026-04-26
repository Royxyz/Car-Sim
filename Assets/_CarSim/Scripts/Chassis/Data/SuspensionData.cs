using UnityEngine;

[CreateAssetMenu(fileName = "NewSuspensionData", menuName = "Vehicle Physics/Suspension Data")]
public class SuspensionData : ScriptableObject
{
    public float restLength = 0.5f;
    public float maxTravel = 0.2f;
    public float springStiffness = 35000f;
    public float bumpDamping = 3500f;
    public float reboundDamping = 4000f;
}