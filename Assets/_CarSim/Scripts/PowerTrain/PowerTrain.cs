using UnityEngine;

[System.Serializable]
public class PowerTrain
{
    [SerializeField] public Engine engine;
    [SerializeField] public Clutch clutch;
    [SerializeField] public Transmission transmission;

    public float engineRPM { get; private set; }
    public float transmissionInputRPM { get; private set; }
    public float currentNetTorque { get; private set; }

    public void Initialize()
    {
        engine.Initialize();
        engineRPM = engine._engineData.idleRPM;
        transmissionInputRPM = 0f;
    }


    public void UpdatePhysics(float throttle, float actualTransRPM, float wheelLoadTorque, float wheelInertia, float dt)
    {
        // 1. Hard-sync the transmission to the actual wheel speed
        transmissionInputRPM = actualTransRPM; 
        
        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);
        
        float idleError = engine._engineData.idleRPM - engineRPM;
        float idleThrottle = idleError > 0f ? Mathf.Clamp01(idleError * 0.005f) : 0f;
        float actualThrottle = Mathf.Max(throttle, idleThrottle);
        
        if (engineRPM >= engine._engineData.redlineRPM) actualThrottle = 0f;

        float engineGeneratedTorque = engine.CalculateDynamicGeneratedTorque(actualThrottle, engineRPM, dt);
        float engineInternalLoss = engine._engineData.GetLossTorque(engineRPM);
        
        // 2. Store the real torque for the wheels to use later
        currentNetTorque = engineGeneratedTorque - engineInternalLoss;

        float reflectedLoad = transmission.GetReflectedLoadTorque(wheelLoadTorque);
        float reflectedInertia = transmission.GetReflectedInertia(wheelInertia);

        float slipVelocity = engineRadPerSec - transRadPerSec;
        bool speedsMatch = Mathf.Abs(slipVelocity) < clutch.clutchData.lockThreshold;
        float currentMaxCapacity = clutch.engagement * clutch.clutchData.maxTorqueCapacity;

        float requiredReactionTorque = clutch.CalculateReactionTorque(engine._engineData.engineInertia, currentNetTorque, reflectedInertia, reflectedLoad);

        if (clutch.engagement > 0.01f && speedsMatch && Mathf.Abs(requiredReactionTorque) <= currentMaxCapacity)
        {
            clutch.isLocked = true;
            // 3. Forward Kinematics: If locked, the heavy wheels dictate the engine speed
            engineRadPerSec = transRadPerSec;
        }
        else
        {
            clutch.isLocked = false;
            float clutchTorque = clutch.CalculateSlippingTorque(engineRadPerSec, transRadPerSec);
            float maxTorqueToZeroSlip = Mathf.Abs(slipVelocity) * (engine._engineData.engineInertia * reflectedInertia) / ((engine._engineData.engineInertia + reflectedInertia) * dt);

            if (Mathf.Abs(clutchTorque) > maxTorqueToZeroSlip)
            {
                clutchTorque = Mathf.Sign(slipVelocity) * maxTorqueToZeroSlip;
            }

            float engineAccel = (currentNetTorque - clutchTorque) / engine._engineData.engineInertia;
            engineRadPerSec += engineAccel * dt;
        }

        engineRPM = Mathf.Max(0f, engineRadPerSec * (30f / Mathf.PI));
    }

    public float GetWheelTorque()
    {
        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);

        float clutchTorque = clutch.CalculateSlippingTorque(engineRadPerSec, transRadPerSec);
        if (clutch.isLocked)
        {
             // 4. FIX: Use the actual torque the engine is producing, NOT 100% throttle!
             clutchTorque = currentNetTorque;
        }
        return transmission.GetOutputTorque(clutchTorque);
    }

}