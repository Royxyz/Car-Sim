using UnityEngine;

[System.Serializable]
public class Engine 
{
    [SerializeField] public EngineData _engineData;

    private float currentRPM;

    public void Initialize()
    {
        _engineData.Initialize();
    }
    
    public float CalculateNetTorque(float throttlePosition, float currentRPM, float loadTorque)
    {
        return _engineData.GetGeneratedTorque(currentRPM, throttlePosition) - 
                _engineData.GetLossTorque(currentRPM) - loadTorque;
        
    }

}
