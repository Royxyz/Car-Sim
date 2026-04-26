using UnityEngine;

[System.Serializable]
public class Drivetrain
{
    [SerializeField] public DrivetrainData drivetrainData;
    [SerializeField] public Differential frontDiff;
    [SerializeField] public Differential rearDiff;
    [SerializeField] public Differential centerDiff;

    public float[] RouteTorque(float transOutputTorque, float flSpeed, float frSpeed, float rlSpeed, float rrSpeed)
    {
        float[] wheelTorques = new float[4]; 

        switch (drivetrainData.driveType)
        {
            case DriveType.FWD:
                Vector2 frontTorquesFWD = frontDiff.SplitTorque(transOutputTorque, flSpeed, frSpeed);
                wheelTorques[0] = frontTorquesFWD.x;
                wheelTorques[1] = frontTorquesFWD.y;
                wheelTorques[2] = 0f;
                wheelTorques[3] = 0f;
                break;

            case DriveType.RWD:
                Vector2 rearTorquesRWD = rearDiff.SplitTorque(transOutputTorque, rlSpeed, rrSpeed);
                wheelTorques[0] = 0f;
                wheelTorques[1] = 0f;
                wheelTorques[2] = rearTorquesRWD.x;
                wheelTorques[3] = rearTorquesRWD.y;
                break;

            case DriveType.AWD:
                float avgFrontSpeed = (flSpeed + frSpeed) * 0.5f;
                float avgRearSpeed = (rlSpeed + rrSpeed) * 0.5f;
                
                Vector2 centerTorques = centerDiff.SplitTorque(transOutputTorque, avgFrontSpeed, avgRearSpeed);
                
                Vector2 frontTorquesAWD = frontDiff.SplitTorque(centerTorques.x, flSpeed, frSpeed);
                Vector2 rearTorquesAWD = rearDiff.SplitTorque(centerTorques.y, rlSpeed, rrSpeed);

                wheelTorques[0] = frontTorquesAWD.x;
                wheelTorques[1] = frontTorquesAWD.y;
                wheelTorques[2] = rearTorquesAWD.x;
                wheelTorques[3] = rearTorquesAWD.y;
                break;
        }

        return wheelTorques;
    }

    public float GetTotalReflectedInertia(float wInertiaFL, float wInertiaFR, float wInertiaRL, float wInertiaRR)
    {
        if (drivetrainData.driveType == DriveType.FWD)
            return frontDiff.GetReflectedInertia(wInertiaFL, wInertiaFR);
            
        if (drivetrainData.driveType == DriveType.RWD)
            return rearDiff.GetReflectedInertia(wInertiaRL, wInertiaRR);

        float frontReflected = frontDiff.GetReflectedInertia(wInertiaFL, wInertiaFR);
        float rearReflected = rearDiff.GetReflectedInertia(wInertiaRL, wInertiaRR);
        return centerDiff.GetReflectedInertia(frontReflected, rearReflected);
    }

    public float GetTotalReflectedLoad(float loadFL, float loadFR, float loadRL, float loadRR)
    {
        if (drivetrainData.driveType == DriveType.FWD)
            return frontDiff.GetReflectedLoad(loadFL, loadFR);

        if (drivetrainData.driveType == DriveType.RWD)
            return rearDiff.GetReflectedLoad(loadRL, loadRR);

        float frontLoad = frontDiff.GetReflectedLoad(loadFL, loadFR);
        float rearLoad = rearDiff.GetReflectedLoad(loadRL, loadRR);
        return centerDiff.GetReflectedLoad(frontLoad, rearLoad);
    }
}