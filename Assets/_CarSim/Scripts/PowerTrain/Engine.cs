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

        if (_engineData.induction == null) 
        {
            Debug.LogWarning("InductionData missing on EngineData!");
            return _engineData.GetGeneratedTorque(currentRPM, currentThrottleBlade);
        }

        float naBaselinePressure = 1.0f; 
        float extraParasiticDrag = 0f;

        switch (_engineData.induction.type)
        {
            case InductionType.NaturallyAspirated:
                currentManifoldPressure = currentThrottleBlade * naBaselinePressure;
                break;

            case InductionType.Supercharged:
                float targetSCPressure = Mathf.Lerp(0f, _engineData.induction.maxPressureBar, currentThrottleBlade);
                currentManifoldPressure = targetSCPressure;

                float rpmFactorSC = Mathf.InverseLerp(_engineData.idleRPM, _engineData.redlineRPM, currentRPM);
                extraParasiticDrag = _engineData.induction.superchargerParasiticDrag * rpmFactorSC;
                break;

            case InductionType.Turbocharged:
                float baseVacuum = Mathf.Min(currentThrottleBlade, 0.4f) * (1f / 0.4f); 
                
                if (currentThrottleBlade > 0.4f)
                {
                    float boostRequest = Mathf.InverseLerp(0.4f, 1.0f, currentThrottleBlade);
                    float targetTurboPressure = Mathf.Lerp(naBaselinePressure, _engineData.induction.maxPressureBar, boostRequest);

                    float rpmFactorTurbo = Mathf.InverseLerp(_engineData.idleRPM, _engineData.redlineRPM, currentRPM);
                    float currentSpoolTime = Mathf.Lerp(_engineData.induction.turboSpoolTimeAtIdle, _engineData.induction.turboSpoolTimeAtRedline, rpmFactorTurbo);

                    float spoolSpeed = _engineData.induction.maxPressureBar / currentSpoolTime;
                    currentManifoldPressure = Mathf.MoveTowards(currentManifoldPressure, targetTurboPressure, spoolSpeed * dt);

                    currentManifoldPressure = Mathf.Max(currentManifoldPressure, baseVacuum);
                }
                else
                {
                    float dumpSpeed = _engineData.induction.maxPressureBar / _engineData.induction.blowOffTime;
                    currentManifoldPressure = Mathf.MoveTowards(currentManifoldPressure, baseVacuum, dumpSpeed * dt);
                }
                break;
        }


        float mappedThrottle = Mathf.Clamp(currentManifoldPressure, 0f, 1f); 
        float baseNATorque = _engineData.GetGeneratedTorque(currentRPM, mappedThrottle);

        float boostMultiplier = Mathf.Max(1.0f, currentManifoldPressure); 

        return (baseNATorque * boostMultiplier) - extraParasiticDrag;
    }
}