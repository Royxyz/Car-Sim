using UnityEngine;

[System.Serializable]
public class Suspension
{
    [SerializeField] public SuspensionData suspData;

    public float currentLength { get; private set; }
    public float currentNormalLoad { get; private set; }
    public bool isGrounded { get; private set; } 
    
    public void Initialize(float restingMass) 
    {
        float freeLength = suspData.targetRideHeight + suspData.droopTravel;
        float expectedSag = (restingMass * 9.81f) / suspData.springStiffness;

        currentLength = Mathf.Clamp(freeLength - expectedSag, suspData.targetRideHeight - suspData.bumpTravel, freeLength);
        
        currentNormalLoad = 0f;
        isGrounded = false;
    }

    public float CalculateForce(bool isGrounded, float hitDistance, float compressionVelocity)
    {
        this.isGrounded = isGrounded;

        float freeLength = suspData.targetRideHeight + suspData.droopTravel;
        float maxBumpLength = suspData.targetRideHeight - suspData.bumpTravel;

        this.currentLength = isGrounded ? hitDistance : freeLength;

        if (!isGrounded) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float springCompression = freeLength - currentLength;
        float springForce = Mathf.Max(0f, springCompression * suspData.springStiffness);

        float currentBumpDistance = (currentLength - maxBumpLength); 
        if (currentBumpDistance < suspData.bumpStopGap)
        {
            float bumpStopCompression = suspData.bumpStopGap - currentBumpDistance;
            springForce += bumpStopCompression * suspData.bumpStopStiffness; 
        }

        float blendFactor = Mathf.InverseLerp(-suspData.blendWindow, suspData.blendWindow, compressionVelocity);
        float activeDamping = Mathf.Lerp(suspData.reboundDamping, suspData.bumpDamping, blendFactor);
        float dampingForce = compressionVelocity * activeDamping;

        if (compressionVelocity <= 0f) dampingForce = Mathf.Max(dampingForce, -springForce * 0.8f);

        float totalForce = springForce + dampingForce;
    
        currentNormalLoad = Mathf.Clamp(totalForce, 0f, suspData.absoluteMaxForce); 
        this.currentLength = Mathf.Clamp(currentLength, maxBumpLength, freeLength);
        
        return currentNormalLoad;
    }
}