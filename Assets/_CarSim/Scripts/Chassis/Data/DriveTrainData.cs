using UnityEngine;

public enum DriveType { FWD, RWD, AWD }

[CreateAssetMenu(fileName = "NewDrivetrainData", menuName = "Vehicle Physics/Drivetrain Data")]
public class DrivetrainData : ScriptableObject
{
    public DriveType driveType = DriveType.AWD;
}