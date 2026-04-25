using UnityEngine;

[System.Serializable]
public class Clutch
{
    [SerializeField] public ClutchData clutchData;

    public float engagement { get; set; }
    public bool isLocked { get; private set; }

    public float CalculateClutchTorque(float engineAngularVelocity, float transmissionAngularVelocity)
    {
        float slipVelocity = engineAngularVelocity - transmissionAngularVelocity;
        
        if (engagement >= 1f && Mathf.Abs(slipVelocity) < clutchData.lockThreshold)
        {
            isLocked = true;
            return 0f; 
        }

        isLocked = false;
        return Mathf.Sign(slipVelocity) * engagement * clutchData.maxTorqueCapacity;
    }

    public float GetLockedTorque(float engineInertia, float engineNetTorque, float transmissionInertia, float transmissionLoadTorque)
    {
        float totalInertia = engineInertia + transmissionInertia;
        float reactionTorque = (engineNetTorque * transmissionInertia + transmissionLoadTorque * engineInertia) / totalInertia;
        
        if (Mathf.Abs(reactionTorque) > clutchData.maxTorqueCapacity)
        {
            isLocked = false;
            return Mathf.Sign(reactionTorque) * clutchData.maxTorqueCapacity;
        }

        return reactionTorque;
    }
}