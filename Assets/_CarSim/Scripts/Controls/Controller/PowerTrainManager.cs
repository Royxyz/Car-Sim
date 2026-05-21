using UnityEngine;

[System.Serializable]
public class PowertrainManager
{
    public ECUData ecuData;
    public PowerTrain powerTrain;
    public Drivetrain drivetrain;
    public AutoController autoController;

    public bool isTcsActive { get; private set;}

    public void Initialize()
    {
        powerTrain.Initialize();
        autoController.Initialize(powerTrain, autoController.logicData);
        isTcsActive = false;
    }

    public float[] ProcessTorqueRouting(WheelAssembly[] corners, float activeThrottle, float activeBrake, bool isAutomatic, float dt)
    {
        if (isAutomatic) autoController.UpdateController(activeThrottle, activeBrake, dt);

        float ecuThrottle = activeThrottle;
        if (ecuData != null && ecuData.enableTractionControl && activeThrottle > 0.01f)
        {
            float maxDrivenSlip = GetMaxDrivenLongitudinalSlip(corners);

            if (maxDrivenSlip > ecuData.tcsSlipThreshold)
            {
                isTcsActive = true;
                float slipExcess = maxDrivenSlip - ecuData.tcsSlipThreshold;
                float throttleCut = slipExcess * ecuData.tcsAggressiveness;

                ecuThrottle = Mathf.Clamp01(activeThrottle - throttleCut);
            }
            else
            {
                isTcsActive = false;
            }
        }
        else
        {
            isTcsActive = false;
        }

        float[] wheelLoadTorques = new float[4];
        for (int i = 0; i < 4; i++)
        {
            wheelLoadTorques[i] = corners[i].tire.CalculateGripForces(
                corners[i].suspension.currentNormalLoad, corners[i].wheel.longitudinalSlip,
                corners[i].wheel.slipAngle, corners[i].wheel.forwardSpeed,
                corners[i].wheel.wheelLinearSpeed, 0f).x * corners[i].wheel.wheelData.radius;
        }

        float reflectedLoad = drivetrain.GetTotalReflectedLoad(wheelLoadTorques[0], wheelLoadTorques[1], wheelLoadTorques[2], wheelLoadTorques[3]);
        float reflectedInertia = drivetrain.GetTotalReflectedInertia(corners[0].wheel.wheelData.inertia, corners[1].wheel.wheelData.inertia, corners[2].wheel.wheelData.inertia, corners[3].wheel.wheelData.inertia);

        float transOutputRadSec = drivetrain.CalculateInputSpeed(corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity, corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity);
        float transOutputRPM = transOutputRadSec * (30f / Mathf.PI);
        float actualTransRPM = transOutputRPM * powerTrain.transmission.GetTotalRatio();

        powerTrain.UpdatePhysics(ecuThrottle, actualTransRPM, reflectedLoad, reflectedInertia, dt);

        float transOutputTorque = powerTrain.GetWheelTorque();

        return drivetrain.RouteTorque(transOutputTorque, corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity, corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity);
    }

    private float GetMaxDrivenLongitudinalSlip(WheelAssembly[] corners)
    {
        float maxSlip = 0f;
        DriveType driveType = drivetrain.drivetrainData.driveType;

        for (int i = 0; i < 4; i++)
        {
            if (driveType == DriveType.FWD && i > 1) continue; 
            if (driveType == DriveType.RWD && i < 2) continue;

            if (corners[i] != null && corners[i].contact.isGrounded)
            {
                float forwardSpeed = Mathf.Max(Mathf.Abs(corners[i].wheel.forwardSpeed), 0.5f); 
                float wheelSpeed = corners[i].wheel.wheelLinearSpeed;
                float instantaneousSlip = (wheelSpeed - corners[i].wheel.forwardSpeed) / forwardSpeed;

                float slip = Mathf.Abs(instantaneousSlip); 
                if (slip > maxSlip) maxSlip = slip;
            }
        }
        return maxSlip;
    }
}