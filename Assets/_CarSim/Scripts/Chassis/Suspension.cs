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
        this.currentLength = isGrounded ? hitDistance : suspData.restLength + suspData.maxTravel;

        if (!isGrounded) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float compression = suspData.restLength - currentLength;
        if (compression < -suspData.maxTravel) return 0f;

        float springForce = compression * suspData.springStiffness;
        float dampingForce = 0f;

        if (suspensionCompressionVelocity > 0f) 
        {
            dampingForce = suspensionCompressionVelocity * suspData.bumpDamping;
        }
        else 
        {
            dampingForce = suspensionCompressionVelocity * suspData.reboundDamping;
            dampingForce = Mathf.Max(dampingForce, -springForce * 0.8f); 
        }

        if (compression > suspData.maxTravel)
        {
            float excess = compression - suspData.maxTravel;
            excess = Mathf.Min(excess, 0.15f); 
            
            springForce += excess * suspData.springStiffness * 3f; 
            if (suspensionCompressionVelocity > 0f) 
            {
                dampingForce += suspensionCompressionVelocity * suspData.bumpDamping * 1.5f; 
            }
        }

        float totalForce = springForce + dampingForce;
     
        float absoluteMaxForce = suspData.absoluteMaxForce; 
        currentNormalLoad = Mathf.Clamp(totalForce, 0f, absoluteMaxForce); 
        this.currentLength = Mathf.Clamp(currentLength, suspData.restLength - suspData.maxTravel, suspData.restLength + suspData.maxTravel);
        
        return currentNormalLoad;
        
    }

    public Vector3 GetWheelVisualPosition(Vector3 suspensionMountPoint, Vector3 downVector)
    {
        return suspensionMountPoint + (downVector * currentLength);
    }
}