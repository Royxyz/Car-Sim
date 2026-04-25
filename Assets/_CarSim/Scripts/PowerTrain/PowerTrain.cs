using UnityEngine;

[System.Serializable]
public class PowerTrain
{
    [SerializeField] public Engine engine;
    [SerializeField] public Clutch clutch;
    [SerializeField] public Transmission transmission;

    public float engineRPM { get; private set; }
    public float transmissionInputRPM { get; private set; }

    public void Initialize()
    {
        engine.Initialize();
        engineRPM = engine._engineData.idleRPM;
        transmissionInputRPM = 0f;
    }

    public void UpdatePhysics(float throttle, float wheelLoadTorque, float wheelInertia, float dt)
    {
        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);
        float idleError = engine._engineData.idleRPM - engineRPM;
        float idleThrottle = 0f;
        if (idleError > 0f)
        {
            idleThrottle = Mathf.Clamp01(idleError * 0.005f); 
        }

        float actualThrottle = Mathf.Max(throttle, idleThrottle);
        
        if (engineRPM >= engine._engineData.redlineRPM)
        {
            actualThrottle = 0f; 
        }

        float engineGeneratedTorque = engine.CalculateDynamicGeneratedTorque(actualThrottle, engineRPM, dt);
        float engineInternalLoss = engine._engineData.GetLossTorque(engineRPM);
        float engineNetTorque = engineGeneratedTorque - engineInternalLoss;

        float reflectedLoad = transmission.GetReflectedLoadTorque(wheelLoadTorque);
        float reflectedInertia = transmission.GetReflectedInertia(wheelInertia);


        float slipVelocity = engineRadPerSec - transRadPerSec;
        bool speedsMatch = Mathf.Abs(slipVelocity) < clutch.clutchData.lockThreshold;
        float currentMaxCapacity = clutch.engagement * clutch.clutchData.maxTorqueCapacity;

        
        float requiredReactionTorque = clutch.CalculateReactionTorque(engine._engineData.engineInertia, engineNetTorque, reflectedInertia, reflectedLoad);

       
        if (clutch.engagement > 0.01f && speedsMatch && Mathf.Abs(requiredReactionTorque) <= currentMaxCapacity)
        {
            clutch.isLocked = true;
            float totalInertia = engine._engineData.engineInertia + reflectedInertia;
            float acceleration = (engineNetTorque - reflectedLoad) / totalInertia;

            engineRadPerSec += acceleration * dt;
            transRadPerSec = engineRadPerSec; 
        }
        else
        {
            clutch.isLocked = false;
            float clutchTorque = clutch.CalculateSlippingTorque(engineRadPerSec, transRadPerSec);

            
            float maxTorqueToZeroSlip = Mathf.Abs(slipVelocity) * (engine._engineData.engineInertia * reflectedInertia) / ((engine._engineData.engineInertia + reflectedInertia) * dt);
            
            if (Mathf.Abs(clutchTorque) > maxTorqueToZeroSlip)
            {
                clutchTorque = Mathf.Sign(slipVelocity) * maxTorqueToZeroSlip;
                engineRadPerSec -= (clutchTorque / engine._engineData.engineInertia) * dt;
                transRadPerSec = engineRadPerSec; 
            }
            else
            {
                float engineAccel = (engineNetTorque - clutchTorque) / engine._engineData.engineInertia;
                float transAccel = (clutchTorque - reflectedLoad) / reflectedInertia;

                engineRadPerSec += engineAccel * dt;
                transRadPerSec += transAccel * dt;
            }
        }

        engineRPM = Mathf.Max(0f, engineRadPerSec * (30f / Mathf.PI));
        transmissionInputRPM = transRadPerSec * (30f / Mathf.PI);
    }

    public float GetWheelTorque()
    {
        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);
        
        float clutchTorque = clutch.CalculateSlippingTorque(engineRadPerSec, transRadPerSec);
        if (clutch.isLocked)
        {
             clutchTorque = engine._engineData.GetGeneratedTorque(engineRPM, 1f); 
        }
        return transmission.GetOutputTorque(clutchTorque);
    }
}