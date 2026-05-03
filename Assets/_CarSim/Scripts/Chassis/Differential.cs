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

        if (diffData.diffType == DifferentialType.Locked)
        {
            float speedDiff = leftSpeed - rightSpeed;
            float lockingTorque = speedDiff * 1000f; 
            return new Vector2(halfTorque - lockingTorque, halfTorque + lockingTorque);
        }

        float speedDelta = leftSpeed - rightSpeed;
        float frictionTorque = (diffData.preloadLSD * Mathf.Abs(outputTorque)) + (speedDelta * diffData.lockingFriction);
        
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