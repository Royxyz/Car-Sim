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

        float engineGeneratedTorque = engine._engineData.GetGeneratedTorque(engineRPM, throttle);
        float engineInternalLoss = engine._engineData.GetLossTorque(engineRPM);
        float engineNetTorque = engineGeneratedTorque - engineInternalLoss;

        float reflectedLoad = transmission.GetReflectedLoadTorque(wheelLoadTorque);
        float reflectedInertia = transmission.GetReflectedInertia(wheelInertia);

        if (clutch.engagement >= 1f && Mathf.Abs(engineRadPerSec - transRadPerSec) < clutch.clutchData.lockThreshold)
        {
            float totalInertia = engine._engineData.engineInertia + reflectedInertia;
            float netSystemTorque = engineNetTorque - reflectedLoad;
            float acceleration = netSystemTorque / totalInertia;

            engineRadPerSec += acceleration * dt;
            transRadPerSec = engineRadPerSec;
        }
        else
        {
            float clutchTorque = clutch.CalculateClutchTorque(engineRadPerSec, transRadPerSec);

            float engineAccel = (engineNetTorque - clutchTorque) / engine._engineData.engineInertia;
            float transAccel = (clutchTorque - reflectedLoad) / reflectedInertia;

            engineRadPerSec += engineAccel * dt;
            transRadPerSec += transAccel * dt;
        }

        engineRPM = Mathf.Max(0f, engineRadPerSec * (30f / Mathf.PI));
        transmissionInputRPM = transRadPerSec * (30f / Mathf.PI);
    }

    public float GetWheelTorque()
    {
        float engineRadPerSec = engineRPM * (Mathf.PI / 30f);
        float transRadPerSec = transmissionInputRPM * (Mathf.PI / 30f);
        
        float clutchTorque = clutch.CalculateClutchTorque(engineRadPerSec, transRadPerSec);
        return transmission.GetOutputTorque(clutchTorque);
    }
}