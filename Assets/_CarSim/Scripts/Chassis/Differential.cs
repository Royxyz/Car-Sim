using UnityEngine;

[System.Serializable]
public class Differential
{
    [SerializeField] public DifferentialData diffData;

    public Vector2 SplitTorque(float inputTorque, float leftSpeed, float rightSpeed)
    {
        float outputTorque = inputTorque * diffData.gearRatio;

        float leftBaseTorque = outputTorque * diffData.powerBias;
        float rightBaseTorque = outputTorque * (1f - diffData.powerBias);

        if (diffData.diffType == DifferentialType.Open)
        {
            return new Vector2(leftBaseTorque, rightBaseTorque);
        }

        bool isOnPower = inputTorque > 10f;

        if (diffData.diffType == DifferentialType.Locked)
        {
            float speedDiff = leftSpeed - rightSpeed;
            float currentStiffness = isOnPower ? diffData.lockingStiffness : (diffData.lockingStiffness * diffData.coastLockingMultiplier);
            
            float lockingTorque = speedDiff * currentStiffness; 
            return new Vector2(leftBaseTorque - lockingTorque, rightBaseTorque + lockingTorque);
        }

        float speedDelta = leftSpeed - rightSpeed;
        float currentFriction = isOnPower ? diffData.lockingFriction : (diffData.lockingFriction * diffData.coastLockingMultiplier);

        float frictionTorque = (diffData.preloadLSD * Mathf.Abs(outputTorque)) + (speedDelta * currentFriction);

        float maxLock = Mathf.Abs(outputTorque);
        frictionTorque = Mathf.Clamp(frictionTorque, -maxLock, maxLock);

        return new Vector2(leftBaseTorque - frictionTorque, rightBaseTorque + frictionTorque);
    }
    public float GetReflectedInertia(float leftInertia, float rightInertia)
    {
        float ratioSq = diffData.gearRatio * diffData.gearRatio;
        return ((leftInertia + rightInertia) / (2f * ratioSq)) + diffData.inertia;
    }

    public float GetReflectedLoad(float leftLoad, float rightLoad)
    {
        return (leftLoad + rightLoad) / diffData.gearRatio;
    }
    
    public float GetInputSpeed(float leftSpeed, float rightSpeed)
    {
        return ((leftSpeed + rightSpeed) * 0.5f) * diffData.gearRatio;
    }
}