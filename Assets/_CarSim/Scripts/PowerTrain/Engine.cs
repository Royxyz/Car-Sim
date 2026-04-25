using UnityEngine;

[System.Serializable]
public class Engine 
{
    [SerializeField] public EngineData _engineData;

    public float currentThrottleBlade { get; private set; } 
    public float currentManifoldPressure { get; private set; } 

    public void Initialize()
    {
        _engineData.Initialize();
        currentThrottleBlade = 0f;
        currentManifoldPressure = 0f;
    }

    public float CalculateDynamicGeneratedTorque(float playerThrottle, float currentRPM, float dt)
    {
        
        float throttleSpeed = 1f / _engineData.throttleSmoothing;
        currentThrottleBlade = Mathf.MoveTowards(currentThrottleBlade, playerThrottle, throttleSpeed * dt);

        
        if (!_engineData.isTurbocharged)
        {
            currentManifoldPressure = currentThrottleBlade;
        }
        else
        {
            float naBaseline = Mathf.Min(currentThrottleBlade, 0.5f);

            if (currentThrottleBlade > 0.5f)
            {
                float rpmFactor = Mathf.InverseLerp(_engineData.idleRPM, _engineData.redlineRPM, currentRPM);
                float currentSpoolTime = Mathf.Lerp(_engineData.turboSpoolTimeAtIdle, _engineData.turboSpoolTimeAtRedline, rpmFactor);

                float spoolSpeed = 1f / currentSpoolTime;
                currentManifoldPressure = Mathf.MoveTowards(currentManifoldPressure, currentThrottleBlade, spoolSpeed * dt);
                
                currentManifoldPressure = Mathf.Max(currentManifoldPressure, naBaseline);
            }
            else
            {
                float dumpSpeed = 1f / _engineData.blowOffTime;
                currentManifoldPressure = Mathf.MoveTowards(currentManifoldPressure, currentThrottleBlade, dumpSpeed * dt);
            }
        }

        return _engineData.GetGeneratedTorque(currentRPM, currentManifoldPressure);
    }
}