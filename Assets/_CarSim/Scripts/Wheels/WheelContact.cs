using UnityEngine;

[System.Serializable]
public class WheelContact
{
    [Header("Settings")]
    public float rayOriginOffset = 1.0f;
    // Kept here so it doesn't break your existing Inspector serialized data
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

        int hitCount = Physics.RaycastNonAlloc(rayStartPos, -mountUp, hitBuffer, maxSweepLength, trackMask);

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
            // Ray hits the ground. Subtract offset (above mount) and radius (below center) to get pure suspension length.
            hitDistance = validHit.distance - rayOriginOffset - wheelRadius;
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