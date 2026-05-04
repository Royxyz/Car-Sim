using UnityEngine;

[System.Serializable]
public class Differential
{
    [SerializeField] public DifferentialData diffData;

    public Vector2 SplitTorque(float inputTorque, float leftSpeed, float rightSpeed)
    {
        float outputTorque = inputTorque * diffData.gearRatio;
        float halfTorque = outputTorque * 0.5f;

        if (diffData.diffType == DifferentialType.Open)
        {
            return new Vector2(halfTorque, halfTorque);
        }

        // Check if we are on the throttle or coasting/braking
        bool isOnPower = inputTorque > 10f;

        if (diffData.diffType == DifferentialType.Locked)
        {
            float speedDiff = leftSpeed - rightSpeed;
            float currentStiffness = isOnPower ? diffData.lockingStiffness : (diffData.lockingStiffness * diffData.coastLockingMultiplier);
            
            float lockingTorque = speedDiff * currentStiffness; 
            return new Vector2(halfTorque - lockingTorque, halfTorque + lockingTorque);
        }

        // Limited Slip Differential (LSD)
        float speedDelta = leftSpeed - rightSpeed;
        float currentFriction = isOnPower ? diffData.lockingFriction : (diffData.lockingFriction * diffData.coastLockingMultiplier);

        float frictionTorque = (diffData.preloadLSD * Mathf.Abs(outputTorque)) + (speedDelta * currentFriction);
        
        float maxLock = Mathf.Abs(halfTorque);
        frictionTorque = Mathf.Clamp(frictionTorque, -maxLock, maxLock);

        return new Vector2(halfTorque - frictionTorque, halfTorque + frictionTorque);
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