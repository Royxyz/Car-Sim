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

    public float CalculateBrakeTorque(float brakePedalInput, float handbrakeInput, float biasMultiplier, float currentLongitudinalSlip)
    {
        float requestedFootBrake = brakePedalInput * biasMultiplier * brakeData.maxBrakeTorque;
        float requestedHandBrake = handbrakeInput * brakeData.maxHandbrakeTorque;

        float totalRequestedTorque = Mathf.Max(requestedFootBrake, requestedHandBrake);

        if (handbrakeInput > 0.1f)
        {
            isABSDriveActive = false;
            currentAppliedTorque = totalRequestedTorque;
            return currentAppliedTorque;
        }

        if (brakeData.hasABS && brakePedalInput > 0.1f && Mathf.Abs(currentLongitudinalSlip) > brakeData.absSlipThreshold)
        {
            isABSDriveActive = true;
            currentAppliedTorque = requestedFootBrake * brakeData.absReleaseMultiplier;
        }
        else
        {
            isABSDriveActive = false;
            currentAppliedTorque = requestedFootBrake;
        }

        return currentAppliedTorque;
    }
}