using UnityEngine;

[CreateAssetMenu(fileName = "NewChassisData", menuName = "Vehicle Physics/Chassis Data")]
public class ChassisData : ScriptableObject
{
    [Header("Mass & Inertia")]
    public float totalMass = 1500f;
    [Tooltip("Offset from the Rigidbody center. Lower values prevent flipping.")]

    public Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f);
    
    [Tooltip("The virtual bounding box used to calculate rotational inertia if the RB is missing one.")]
    public Vector3 inertiaTensorBoxSize = new Vector3(2.8f, 1.0f, 4.5f);

    [Header("Safety Clamps")]
    [Tooltip("Multiplier for vehicle mass to determine max safe torque before cartwheeling.")]
    public float maxSafeTorqueMultiplier = 100f;
}