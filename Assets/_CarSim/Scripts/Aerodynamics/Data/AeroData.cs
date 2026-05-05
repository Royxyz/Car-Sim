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
    [ContextMenu("Populate Aero Curves")]
    public void GenerateAeroCurves()
    {
        // 1. Downforce / Lift Coefficient (Y) vs Pitch Angle (X)
        // 90s Street car: Mostly neutral. Slight downforce under heavy braking (nose dive), slight lift under hard acceleration.
        downforceVsAoA = new AnimationCurve();
        downforceVsAoA.AddKey(new Keyframe(-15f, 0.25f)); 
        downforceVsAoA.AddKey(new Keyframe(-5f, 0.15f));  
        downforceVsAoA.AddKey(new Keyframe(0f, 0.05f));   
        downforceVsAoA.AddKey(new Keyframe(5f, -0.05f));  
        downforceVsAoA.AddKey(new Keyframe(15f, -0.15f)); 
        downforceVsAoA.AddKey(new Keyframe(30f, -0.20f)); 

        // 2. Drag Coefficient (Y) vs Pitch Angle (X)
        // Base drag coefficient for an early 90s coupe is ~0.33. Pitching exposes more surface area.
        dragVsAoA = new AnimationCurve();
        dragVsAoA.AddKey(new Keyframe(-15f, 0.45f)); 
        dragVsAoA.AddKey(new Keyframe(-5f, 0.36f));
        dragVsAoA.AddKey(new Keyframe(0f, 0.33f));   
        dragVsAoA.AddKey(new Keyframe(5f, 0.37f));
        dragVsAoA.AddKey(new Keyframe(15f, 0.48f));

        // 3. Sideforce Coefficient (Y) vs Yaw Slip Angle (X)
        // Stabilizing force when drifting/sliding.
        sideforceVsSlipAngle = new AnimationCurve();
        sideforceVsSlipAngle.AddKey(new Keyframe(-90f, -0.80f));
        sideforceVsSlipAngle.AddKey(new Keyframe(-45f, -0.60f));
        sideforceVsSlipAngle.AddKey(new Keyframe(-10f, -0.15f));
        sideforceVsSlipAngle.AddKey(new Keyframe(0f, 0f));
        sideforceVsSlipAngle.AddKey(new Keyframe(10f, 0.15f));
        sideforceVsSlipAngle.AddKey(new Keyframe(45f, 0.60f));
        sideforceVsSlipAngle.AddKey(new Keyframe(90f, 0.80f));

        // Smooth out the tangents for realistic fluid dynamics
        for (int i = 0; i < downforceVsAoA.length; i++) UnityEditor.AnimationUtility.SetKeyLeftTangentMode(downforceVsAoA, i, UnityEditor.AnimationUtility.TangentMode.Auto);
        for (int i = 0; i < dragVsAoA.length; i++) UnityEditor.AnimationUtility.SetKeyLeftTangentMode(dragVsAoA, i, UnityEditor.AnimationUtility.TangentMode.Auto);
        for (int i = 0; i < sideforceVsSlipAngle.length; i++) UnityEditor.AnimationUtility.SetKeyLeftTangentMode(sideforceVsSlipAngle, i, UnityEditor.AnimationUtility.TangentMode.Auto);
    }
#endif
}