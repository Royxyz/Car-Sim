using UnityEngine;

[CreateAssetMenu(fileName = "NewAutoControllerLogic", menuName = "Vehicle Physics/Auto Controller Logic")]
public class AutoControllerLogicData : ScriptableObject
{
    [Header("Auto-Clutch Variables")]
    [Tooltip("The RPM where the robotic clutch begins to grab.")]
    public float biteRPM = 1000f;
    
    [Tooltip("The RPM where the robotic clutch is fully engaged.")]
    public float lockRPM = 1500f;

    [Header("Auto-Shift Variables")]
    [Tooltip("The RPM threshold that triggers an upshift.")]
    public float upshiftRPM = 6500f;
    
    [Tooltip("The RPM threshold that triggers a downshift (to prevent bogging).")]
    public float downshiftRPM = 2500f;
    
    [Tooltip("Time in seconds it takes the logic to complete a gear swap (simulating shifter movement).")]
    public float shiftDuration = 0.3f;
}