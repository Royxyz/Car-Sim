using UnityEngine;

[System.Serializable]
public class Clutch
{
    [SerializeField] public ClutchData clutchData;

    public float engagement { get; set; }
    public bool isLocked { get; set; } // Now controlled by PowerTrain

    public float CalculateSlippingTorque(float engineAngularVelocity, float transmissionAngularVelocity)
    {
        float slipVelocity = engineAngularVelocity - transmissionAngularVelocity;
        return Mathf.Sign(slipVelocity) * engagement * clutchData.maxTorqueCapacity;
    }

    public float CalculateReactionTorque(float engineInertia, float engineNetTorque, float transmissionInertia, float transmissionLoadTorque)
    {
        float totalInertia = engineInertia + transmissionInertia;
        // Calculates the physical torque required to hold the engine and transmission together as a single mass
        return (engineNetTorque * transmissionInertia + transmissionLoadTorque * engineInertia) / totalInertia;
    }
}