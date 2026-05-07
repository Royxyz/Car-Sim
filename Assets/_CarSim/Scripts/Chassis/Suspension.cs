using UnityEngine;

[System.Serializable]
public class Suspension
{
    [SerializeField] public SuspensionData suspData;

    public float currentLength { get; private set; }
    public float currentNormalLoad { get; private set; }
    public bool isGrounded { get; private set; } 
    
    private float staticPreloadForce = 0f;

  
    public void Initialize(float restingMass) 
    {
        currentLength = suspData.targetRideHeight;
        currentNormalLoad = 0f;
        isGrounded = false;
        staticPreloadForce = restingMass * 9.81f; 
    }
    public float CalculateForce(bool isGrounded, float hitDistance, float compressionVelocity)
    {
        this.isGrounded = isGrounded;
        
        float maxDroopLength = suspData.targetRideHeight + suspData.droopTravel;
        float maxBumpLength = suspData.targetRideHeight - suspData.bumpTravel;

        this.currentLength = isGrounded ? hitDistance : maxDroopLength;

        if (!isGrounded) 
        {
            currentNormalLoad = 0f;
            return 0f;
        }

        float deviation = suspData.targetRideHeight - currentLength;
        float springForce = staticPreloadForce + (deviation * suspData.springStiffness);
        springForce = Mathf.Max(0f, springForce); 

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
        this.currentLength = Mathf.Clamp(currentLength, maxBumpLength, maxDroopLength);
        
        return currentNormalLoad;
    }
}