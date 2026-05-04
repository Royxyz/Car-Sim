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
        float throttleSpeed = 1f / Mathf.Max(_engineData.throttleSmoothing, 0.001f);
        currentThrottleBlade = Mathf.MoveTowards(currentThrottleBlade, playerThrottle, throttleSpeed * dt);

        if (_engineData.induction == null) 
        {
            return _engineData.GetGeneratedTorque(currentRPM, currentThrottleBlade);
        }

        float naBaselinePressure = 1.0f; 
        float engineVacuum = 0.2f;      
        float extraParasiticDrag = 0f;

        switch (_engineData.induction.type)
        {
            case InductionType.NaturallyAspirated:
                currentManifoldPressure = Mathf.Lerp(engineVacuum, naBaselinePressure, currentThrottleBlade);
                break;

            case InductionType.Supercharged:

                float rpmRatio = Mathf.InverseLerp(_engineData.idleRPM, _engineData.redlineRPM, currentRPM);
                float maxAvailableSCBoost = naBaselinePressure + (_engineData.induction.maxPressureBar * rpmRatio);
                currentManifoldPressure = Mathf.Lerp(engineVacuum, maxAvailableSCBoost, currentThrottleBlade);
                extraParasiticDrag = _engineData.induction.superchargerParasiticDrag * rpmRatio;
                break;

            case InductionType.Turbocharged:
                float exhaustEnergyFactor = Mathf.InverseLerp(
                    _engineData.induction.boostThresholdRPM, 
                    _engineData.induction.optimalBoostRPM, 
                    currentRPM
                );

                float maxAvailableTurboPressure = naBaselinePressure + (_engineData.induction.maxPressureBar * exhaustEnergyFactor);

                float targetPressure = Mathf.Lerp(engineVacuum, maxAvailableTurboPressure, currentThrottleBlade);

                if (targetPressure > currentManifoldPressure)
                {
                    float dynamicSpoolRate = Mathf.Lerp(_engineData.induction.turboSpoolRate * 0.5f, _engineData.induction.turboSpoolRate * 3f, exhaustEnergyFactor);
                    
                    currentManifoldPressure = Mathf.Lerp(currentManifoldPressure, targetPressure, 1f - Mathf.Exp(-dynamicSpoolRate * dt));
                }
                else
                {
                    currentManifoldPressure = Mathf.Lerp(currentManifoldPressure, targetPressure, 1f - Mathf.Exp(-_engineData.induction.blowOffRate * dt));
                }
                break;
        }

        float baseNATorque = _engineData.GetGeneratedTorque(currentRPM, currentThrottleBlade);
        float boostMultiplier = Mathf.Max(1.0f, currentManifoldPressure); 

        return (baseNATorque * boostMultiplier) - extraParasiticDrag;
    }
}