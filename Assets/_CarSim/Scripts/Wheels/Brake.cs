using UnityEngine;

[System.Serializable]
public class Brake
{
    [SerializeField] public BrakeData brakeData;

    public float currentAppliedTorque { get; private set; }
    public bool isABSDriveActive { get; private set; }

    public void Initialize()
    {
        currentAppliedTorque = 0f;
        isABSDriveActive = false;
    }

    public float CalculateBrakeTorque(float brakePedalInput, float currentLongitudinalSlip)
    {
        float requestedTorque = brakePedalInput * brakeData.maxBrakeTorque;

        if (brakeData.hasABS && brakePedalInput > 0.1f && Mathf.Abs(currentLongitudinalSlip) > brakeData.absSlipThreshold)
        {
            isABSDriveActive = true;
            currentAppliedTorque = requestedTorque * brakeData.absReleaseMultiplier;
        }
        else
        {
            isABSDriveActive = false;
            currentAppliedTorque = requestedTorque;
        }

        return currentAppliedTorque;
    }
}