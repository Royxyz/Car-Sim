using UnityEngine;

[CreateAssetMenu(fileName = "NewAeroData", menuName = "Vehicle Physics/Advanced Aerodynamics Data")]
public class AeroData : ScriptableObject
{
    [Header("Environment")]
    public float airDensity = 1.225f; 

    [Header("Geometry")]
    public float frontalArea = 2.5f; 
    public float sideArea = 5.0f;    
    public float topArea = 6.0f;     

    [Header("Center of Pressure")]
    [Tooltip("Offset relative to the Transform Origin. Usually placed slightly high, and behind the Center of Mass for high-speed stability.")]
    public Vector3 centerOfPressureOffset = new Vector3(0f, 0.4f, -0.8f);

    [Header("Dynamic Curves")]
    [Tooltip("X: Pitch Angle of Attack (deg). Y: Lift Coefficient. Negative pitch (nose down) should increase downforce.")]
    public AnimationCurve downforceVsAoA = AnimationCurve.Linear(-5f, 1.8f, 5f, 1.0f);

    [Tooltip("X: Pitch Angle of Attack (deg). Y: Drag Coefficient. Pitching up or down exposes more surface area, increasing drag.")]
    public AnimationCurve dragVsAoA = AnimationCurve.Linear(-5f, 0.6f, 5f, 0.6f);

    [Tooltip("X: Yaw Slip Angle (deg). Y: Sideforce Coefficient. How hard the air pushes back when the car goes sideways.")]
    public AnimationCurve sideforceVsSlipAngle = AnimationCurve.Linear(-90f, -1.2f, 90f, 1.2f);
}