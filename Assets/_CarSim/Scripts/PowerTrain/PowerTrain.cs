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
    private float _lastReactionTorque; 
    private bool _ignitionCutActive = false; 

    public void Initialize()
    {
        engine.Initialize();
        engineRPM = engine._engineData.idleRPM;
        transmissionInputRPM = 0f;
        _lastReactionTorque = 0f;
    }

    public void UpdatePhysics(float throttle, float actualTransRPM, float wheelLoadTorque, float wheelInertia, float dt)
    {
        transmissionInputRPM = actualTransRPM; 
        
        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);
        
        float idleError = engine._engineData.idleRPM - engineRPM;
        float idleThrottle = idleError > 0f ? Mathf.Clamp01(idleError * 0.005f) : 0f;
        float actualThrottle = Mathf.Max(throttle, idleThrottle);
        

        if (engineRPM >= engine._engineData.redlineRPM)
        {
            _ignitionCutActive = true;
        }
        else if (engineRPM < engine._engineData.redlineRPM - 150f) 
        {
            _ignitionCutActive = false; 
        }

        float engineGeneratedTorque = engine.CalculateDynamicGeneratedTorque(actualThrottle, engineRPM, dt);
        
        if (_ignitionCutActive)
        {
            engineGeneratedTorque = -150f; 
        }

        float engineInternalLoss = engine._engineData.GetLossTorque(engineRPM);
        
        currentNetTorque = engineGeneratedTorque - engineInternalLoss;

        float reflectedLoad = transmission.GetReflectedLoadTorque(wheelLoadTorque);
        float reflectedInertia = transmission.GetReflectedInertia(wheelInertia);

        float slipVelocity = engineRadPerSec - transRadPerSec;
        bool speedsMatch = Mathf.Abs(slipVelocity) < clutch.clutchData.lockThreshold;
        float currentMaxCapacity = clutch.engagement * clutch.clutchData.maxTorqueCapacity;

        float requiredReactionTorque = clutch.CalculateReactionTorque(engine._engineData.engineInertia, currentNetTorque, reflectedInertia, reflectedLoad);
        
        _lastReactionTorque = requiredReactionTorque;

        if (clutch.engagement > 0.01f && speedsMatch && Mathf.Abs(requiredReactionTorque) <= currentMaxCapacity)
        {
            clutch.isLocked = true;
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
        if (clutch.isLocked)
        {
            return transmission.GetOutputTorque(currentNetTorque);
        }

        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);
        return transmission.GetOutputTorque(clutch.CalculateSlippingTorque(engineRadPerSec, transRadPerSec));
    }
}