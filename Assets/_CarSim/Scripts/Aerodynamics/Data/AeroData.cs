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

    [Header("Ground Effect")]
    [Tooltip("The physical distance (in meters) from the floor where the underbody aero is 100% efficient.")]
    public float optimalRideHeight = 0.12f;
    
    [Tooltip("How much extra downforce is generated when the car hits the optimal ride height.")]
    public float groundEffectMultiplier = 1.25f;

    [Header("Dynamic Curves")]
    [Tooltip("X: Pitch Angle of Attack (deg). Y: Lift Coefficient. Negative pitch (nose down) should increase downforce.")]
    public AnimationCurve downforceVsAoA = AnimationCurve.Linear(-5f, 1.8f, 5f, 1.0f);

    [Tooltip("X: Pitch Angle of Attack (deg). Y: Drag Coefficient. Pitching up or down exposes more surface area, increasing drag.")]
    public AnimationCurve dragVsAoA = AnimationCurve.Linear(-5f, 0.6f, 5f, 0.6f);

    [Tooltip("X: Yaw Slip Angle (deg). Y: Sideforce Coefficient. How hard the air pushes back when the car goes sideways.")]
    public AnimationCurve sideforceVsSlipAngle = AnimationCurve.Linear(-90f, -1.2f, 90f, 1.2f);

    #if UNITY_EDITOR
    [ContextMenu("Populate  Aero Curves")]
    public void GenerateAeroCurves()
    {
        // 1. Downforce / Lift Coefficient (Y) vs Pitch Angle (X)
        downforceVsAoA = new AnimationCurve();
        downforceVsAoA.AddKey(new Keyframe(-15f, 0.70f)); 
        downforceVsAoA.AddKey(new Keyframe(-5f, 0.45f));  
        downforceVsAoA.AddKey(new Keyframe(0f, 0.30f));   
        downforceVsAoA.AddKey(new Keyframe(5f, 0.15f));   
        downforceVsAoA.AddKey(new Keyframe(15f, -0.05f)); 
        downforceVsAoA.AddKey(new Keyframe(30f, -0.10f)); 

        // 2. Drag Coefficient (Y) vs Pitch Angle (X)
        dragVsAoA = new AnimationCurve();
        dragVsAoA.AddKey(new Keyframe(-15f, 0.55f)); 
        dragVsAoA.AddKey(new Keyframe(-5f, 0.41f));
        dragVsAoA.AddKey(new Keyframe(0f, 0.38f));   
        dragVsAoA.AddKey(new Keyframe(5f, 0.41f));
        dragVsAoA.AddKey(new Keyframe(15f, 0.55f));

        // 3. Sideforce Coefficient (Y) vs Yaw Slip Angle (X)
        sideforceVsSlipAngle = new AnimationCurve();
        sideforceVsSlipAngle.AddKey(new Keyframe(-90f, -1.10f));
        sideforceVsSlipAngle.AddKey(new Keyframe(-45f, -0.85f));
        sideforceVsSlipAngle.AddKey(new Keyframe(-10f, -0.30f));
        sideforceVsSlipAngle.AddKey(new Keyframe(0f, 0f));
        sideforceVsSlipAngle.AddKey(new Keyframe(10f, 0.30f));
        sideforceVsSlipAngle.AddKey(new Keyframe(45f, 0.85f));
        sideforceVsSlipAngle.AddKey(new Keyframe(90f, 1.10f));

        for (int i = 0; i < downforceVsAoA.length; i++) UnityEditor.AnimationUtility.SetKeyLeftTangentMode(downforceVsAoA, i, UnityEditor.AnimationUtility.TangentMode.Auto);
        for (int i = 0; i < dragVsAoA.length; i++) UnityEditor.AnimationUtility.SetKeyLeftTangentMode(dragVsAoA, i, UnityEditor.AnimationUtility.TangentMode.Auto);
        for (int i = 0; i < sideforceVsSlipAngle.length; i++) UnityEditor.AnimationUtility.SetKeyLeftTangentMode(sideforceVsSlipAngle, i, UnityEditor.AnimationUtility.TangentMode.Auto);

    }
#endif
}