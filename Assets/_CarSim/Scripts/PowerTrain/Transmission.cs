using UnityEngine;

[System.Serializable]
public class Transmission
{
    [SerializeField] public TransmissionData transmissionData;

    public int currentGear { get; private set; } = 0;

    public void ShiftUp()
    {
        if (currentGear < transmissionData.forwardGears.Length)
        {
            currentGear++;
        }
    }

    public void ShiftDown()
    {
        if (currentGear > -1)
        {
            currentGear--;
        }
    }

    public float GetTotalRatio()
    {
        if (currentGear == 0) return 0f;

        float ratio = currentGear == -1 
        ? -Mathf.Abs(transmissionData.reverseGear) 
        : transmissionData.forwardGears[currentGear - 1];

        return ratio * transmissionData.finalDrive;
    }

   public float GetOutputTorque(float inputTorque)
    {
        float ratio = GetTotalRatio();
        float directionalEfficiency = (inputTorque >= 0f) ? transmissionData.efficiency : (1f / Mathf.Max(transmissionData.efficiency, 0.1f));
        
        return inputTorque * ratio * directionalEfficiency;
    }

    public float GetReflectedLoadTorque(float outputLoadTorque)
    {
        float ratio = GetTotalRatio();
        if (Mathf.Abs(ratio) < 0.001f) return 0f;

        float directionalEfficiency = (outputLoadTorque >= 0f) ? (1f / Mathf.Max(transmissionData.efficiency, 0.1f)) : transmissionData.efficiency;
        
        return (outputLoadTorque / ratio) * directionalEfficiency;
    }

    public float GetReflectedInertia(float outputInertia)
    {
        float ratio = GetTotalRatio();
        if (Mathf.Abs(ratio) < 0.001f) return transmissionData.transmissionInertia; 
        
        return (outputInertia / (ratio * ratio)) + transmissionData.transmissionInertia;
    }
}