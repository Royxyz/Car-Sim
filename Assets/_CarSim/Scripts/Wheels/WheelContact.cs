using UnityEngine;

[System.Serializable]
public class WheelContact
{
    [Header("Settings")]
    public float rayOriginOffset = 1.0f;
    [Tooltip("The radius of the collision sphere. Prevents the wheel from falling through small cracks in the road.")]
    public float castRadius = 0.12f; 

    public bool isGrounded { get; private set; }
    public float hitDistance { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 contactNormal { get; private set; }

    private RaycastHit[] hitBuffer = new RaycastHit[10];

    public void EvaluateContact(Transform vehicleRoot, Vector3 mountPos, Vector3 mountUp, float maxSuspensionLength, float wheelRadius, LayerMask trackMask)
    {
        if (hitBuffer == null) hitBuffer = new RaycastHit[10];
        
        Vector3 rayStartPos = mountPos + (mountUp * rayOriginOffset);
        float maxSweepLength = maxSuspensionLength + wheelRadius + rayOriginOffset;

        // FIX: Re-enable the SphereCast, which is much more stable than a pure Raycast for vehicles
        int hitCount = Physics.SphereCastNonAlloc(rayStartPos, castRadius, -mountUp, hitBuffer, maxSweepLength, trackMask);

        bool foundValidHit = false;
        RaycastHit validHit = default;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            // Crucial: Ignore the car's own body colliders
            if (hitBuffer[i].collider.transform.root != vehicleRoot)
            {
                if (hitBuffer[i].distance < closestDistance)
                {
                    closestDistance = hitBuffer[i].distance;
                    validHit = hitBuffer[i];
                    foundValidHit = true;
                }
            }
        }

        if (foundValidHit)
        {
            isGrounded = true;
            
            // FIX: validHit.distance is the sweep distance of the center of the sphere.
            // The physical bottom of the sphere is lower by exactly 'castRadius'.
            // We add castRadius to find the true depth of the ground, then subtract offset and wheelRadius.
            hitDistance = validHit.distance + castRadius - rayOriginOffset - wheelRadius;
            
            contactPoint = validHit.point;
            contactNormal = validHit.normal;
        }
        else
        {
            isGrounded = false;
            hitDistance = maxSuspensionLength;
            contactPoint = mountPos - (mountUp * maxSuspensionLength) - (mountUp * wheelRadius);
            contactNormal = Vector3.up;
        }
    }
}