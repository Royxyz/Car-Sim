using UnityEngine;

[System.Serializable]
public class PowertrainManager
{
    public PowerTrain powerTrain;
    public Drivetrain drivetrain;
    public AutoController autoController;

    public void Initialize()
    {
        powerTrain.Initialize();
        autoController.Initialize(powerTrain, autoController.logicData);
    }

    public float[] ProcessTorqueRouting(WheelAssembly[] corners, float activeThrottle, float activeBrake, bool isAutomatic, float dt)
    {
        if (isAutomatic)
        {
            autoController.UpdateController(activeThrottle, activeBrake, dt);
        }

        float[] wheelLoadTorques = new float[4];
        for (int i = 0; i < 4; i++)
        {
            wheelLoadTorques[i] = corners[i].tire.CalculateGripForces(
                corners[i].suspension.currentNormalLoad,
                corners[i].wheel.longitudinalSlip,
                corners[i].wheel.slipAngle,
                corners[i].wheel.forwardSpeed,
                corners[i].wheel.wheelLinearSpeed,
                0f
            ).x * corners[i].wheel.wheelData.radius;
        }

        float reflectedLoad = drivetrain.GetTotalReflectedLoad(
            wheelLoadTorques[0], wheelLoadTorques[1],
            wheelLoadTorques[2], wheelLoadTorques[3]
        );
        
        float reflectedInertia = drivetrain.GetTotalReflectedInertia(
            corners[0].wheel.wheelData.inertia, corners[1].wheel.wheelData.inertia, 
            corners[2].wheel.wheelData.inertia, corners[3].wheel.wheelData.inertia
        );

        float transOutputRadSec = drivetrain.CalculateInputSpeed(
            corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity,
            corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity
        );

        float transOutputRPM = transOutputRadSec * (30f / Mathf.PI);
        float actualTransRPM = transOutputRPM * powerTrain.transmission.GetTotalRatio();

        powerTrain.UpdatePhysics(activeThrottle, actualTransRPM, reflectedLoad, reflectedInertia, dt);

        float transOutputTorque = powerTrain.GetWheelTorque();
        
        // This is the array allocation fix identified in the analysis
        return drivetrain.RouteTorque(transOutputTorque, 
            corners[0].wheel.angularVelocity, corners[1].wheel.angularVelocity, 
            corners[2].wheel.angularVelocity, corners[3].wheel.angularVelocity);
    }
}