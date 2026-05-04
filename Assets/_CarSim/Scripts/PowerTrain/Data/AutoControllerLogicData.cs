using UnityEngine;

[CreateAssetMenu(fileName = "NewAutoControllerLogic", menuName = "Vehicle Physics/Auto Controller Logic")]
public class AutoControllerLogicData : ScriptableObject
{
    [Header("Launch & Creep Dynamics")]
    [Tooltip("If true, simulates a fluid torque converter that creeps forward at idle. If false, acts like a DCT/Sequential and requires throttle to move.")]
    public bool hasTorqueConverterCreep = false;
    
    [Tooltip("How hard the clutch drags at idle if creep is enabled (0.0 to 1.0).")]
    public float idleCreepEngagement = 0.05f;

    [Tooltip("The RPM where the robotic clutch begins to grab.")]
    public float biteRPM = 1200f; 
    
    [Tooltip("The RPM where the robotic clutch is fully mechanically locked.")]
    public float lockRPM = 2000f;

    [Header("Auto-Shift Variables")]
    [Tooltip("The RPM threshold that triggers an upshift.")]
    public float upshiftRPM = 6500f;
    
    [Tooltip("The RPM threshold that triggers a downshift (to prevent bogging).")]
    public float downshiftRPM = 2500f;
    
    [Tooltip("Time in seconds it takes the logic to complete a gear swap.")]
    public float shiftDuration = 0.2f;
}