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

    public float CalculateForce(bool isGrounded, float hitDistance, float suspensionCompressionVelocity)
    {
        this.isGrounded = isGrounded;
        
        // Target is hit distance if grounded, otherwise max droop
        this.currentLength = isGrounded ? hitDistance : suspData.restLength + suspData.maxTravel;
        this.currentLength = Mathf.Clamp(currentLength, suspData.restLength - suspData.maxTravel, suspData.restLength + suspData.maxTravel);

        if (!isGrounded) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float compression = suspData.restLength - currentLength;

        // If the spring is completely stretched out, it applies no force
        if (compression < -suspData.maxTravel) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float springForce = compression * suspData.springStiffness;
        
        // Smooth, physics-based damping
        float dampingForce = 0f;
        if (suspensionCompressionVelocity > 0f) 
        {
            dampingForce = suspensionCompressionVelocity * suspData.bumpDamping;
        }
        else 
        {
            dampingForce = suspensionCompressionVelocity * suspData.reboundDamping;
        }

        float totalForce = springForce + dampingForce;
        
        // Tires can only push up on the chassis, not pull it down
        currentNormalLoad = Mathf.Max(0f, totalForce); 
        return currentNormalLoad;
    }

    public Vector3 GetWheelVisualPosition(Vector3 suspensionMountPoint, Vector3 downVector)
    {
        return suspensionMountPoint + (downVector * currentLength);
    }
}