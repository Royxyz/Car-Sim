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
            this.currentLength = Mathf.Clamp(currentLength, suspData.restLength - suspData.maxTravel, suspData.restLength + suspData.maxTravel);

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
                
                // THE FIX: Never let rebound damping pull the tire completely off the ground
                // We cap the negative damping force to 80% of the positive spring force
                dampingForce = Mathf.Max(dampingForce, -springForce * 0.8f); 
            }

            float totalForce = springForce + dampingForce;
            currentNormalLoad = Mathf.Max(0f, totalForce); 
            return currentNormalLoad;
        }

    public Vector3 GetWheelVisualPosition(Vector3 suspensionMountPoint, Vector3 downVector)
    {
        return suspensionMountPoint + (downVector * currentLength);
    }
}