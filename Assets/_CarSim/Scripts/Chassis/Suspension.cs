using UnityEngine;

[System.Serializable]
public class Suspension
{
    [SerializeField] public SuspensionData suspData;

    public float currentLength { get; private set; }
    public float currentVelocity { get; private set; }
    public float currentNormalLoad { get; private set; }
    public bool isGrounded { get; private set; } 

    public void Initialize()
    {
        currentLength = suspData.restLength;
        currentVelocity = 0f;
        currentNormalLoad = 0f;
        isGrounded = false;
    }

    public float CalculateForceFromRaycast(bool didHit, float hitDistance, float dt)
    {
        isGrounded = didHit;
        float targetLength = isGrounded ? hitDistance : suspData.restLength + suspData.maxTravel;

        targetLength = Mathf.Clamp(targetLength, suspData.restLength - suspData.maxTravel, suspData.restLength + suspData.maxTravel);
        
        currentVelocity = (targetLength - currentLength) / dt;
        currentLength = targetLength;

        if (!isGrounded) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float compression = suspData.restLength - currentLength;

        if (compression < -suspData.maxTravel) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float springForce = compression * suspData.springStiffness;
        
        float dampingForce = 0f;
        if (currentVelocity < 0f) 
        {
            dampingForce = -currentVelocity * suspData.bumpDamping;
        }
        else 
        {
            dampingForce = -currentVelocity * suspData.reboundDamping;
        }

        float totalForce = springForce + dampingForce;
        
        currentNormalLoad = Mathf.Max(0f, totalForce); 
        return totalForce;
    }

    public Vector3 GetWheelVisualPosition(Vector3 suspensionMountPoint, Vector3 downVector)
    {
        return suspensionMountPoint + (downVector * currentLength);
    }
}