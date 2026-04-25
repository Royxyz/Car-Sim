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
            ? transmissionData.reverseGear 
            : transmissionData.forwardGears[currentGear - 1];

        return ratio * transmissionData.finalDrive;
    }

    public float GetOutputTorque(float inputTorque)
    {
        float ratio = GetTotalRatio();
        return inputTorque * ratio * transmissionData.efficiency;
    }

    public float GetReflectedLoadTorque(float outputLoadTorque)
    {
        float ratio = GetTotalRatio();
        if (Mathf.Abs(ratio) < 0.001f) return 0f;

        return outputLoadTorque / (ratio * transmissionData.efficiency);
    }

    public float GetReflectedInertia(float outputInertia)
    {
        float ratio = GetTotalRatio();
        return (outputInertia * ratio * ratio) + transmissionData.transmissionInertia;
    }
}