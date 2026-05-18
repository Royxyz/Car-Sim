using UnityEngine;

[CreateAssetMenu(fileName = "NewAeroData", menuName = "Vehicle Physics/Advanced Aerodynamics Data")]
public class AeroData : ScriptableObject
{
    [Header("Environment & Geometry")]
    public float airDensity = 1.225f; 
    public float frontalArea = 2.0f;     
    public float planformArea = 8.0f;   
    public float sideArea = 4.5f;        

    [Header("Drag (Opposes Velocity Vector)")]
    public float baseDragCoef = 0.35f;   
    public float pitchDragSensitivity = 0.004f; 
    public float yawDragSensitivity = 0.015f;   

    [Header("Front Axle Downforce")]
    [Tooltip("Positive value = Downforce. Most street cars actually have slight lift (-0.05) here at factory ride height.")]
    public float frontBaseDownforceCoef = 0.05f; 
    [Tooltip("Added front downforce per degree of NEGATIVE pitch (nose dive).")]
    public float frontPitchSensitivity = 0.015f; 

    [Header("Rear Axle Downforce")]
    public float rearBaseDownforceCoef = 0.08f;
    [Tooltip("Lost rear downforce per degree of NEGATIVE pitch (tail lifts, spoiler loses bite).")]
    public float rearPitchSensitivity = 0.012f;

    [Header("Sideforce (Yaw)")]
    [Tooltip("Lateral force coefficient per degree of slip angle. Pushes against the slide.")]
    public float yawSideforceSensitivity = 0.04f;

    [Header("Underbody Ground Effect")]
    [Tooltip("Physical height from ground where underbody suction is 100% efficient.")]
    public float optimalRideHeight = 0.12f;
    [Tooltip("Maximum additive downforce coefficient from purely ground effect (Venturi suction).")]
    public float maxGroundEffectCoef = 0.15f;
    [Tooltip("0.65 bias means 65% of GE applied at front, 35% at rear")]
    public float groundEffectBias = 0.5f;
    
    [Header("Application Center")]
    [Tooltip("Offset for Drag and Sideforce. Usually near the physical center of the car volume, slightly above the floor.")]
    public Vector3 aeroCenterOffset = new Vector3(0f, 0.4f, 0f);
}