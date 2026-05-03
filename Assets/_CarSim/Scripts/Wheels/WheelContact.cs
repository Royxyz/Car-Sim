using UnityEngine;

[System.Serializable]
public class WheelContact
{
    [Header("Settings")]
    public float rayOriginOffset = 1.0f;
    public float castRadius = 0.12f;

    public bool isGrounded { get; private set; }
    public float hitDistance { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 contactNormal { get; private set; }

    private RaycastHit[] hitBuffer = new RaycastHit[10];

    public void EvaluateContact(Transform vehicleRoot, Vector3 mountPos, Vector3 mountUp, float maxSuspensionLength, float wheelRadius, LayerMask trackMask)
    {
        Vector3 rayStartPos = mountPos + (mountUp * rayOriginOffset);

        float maxSweepLength = maxSuspensionLength + wheelRadius + rayOriginOffset;

        int hitCount = Physics.SphereCastNonAlloc(rayStartPos, castRadius, -mountUp, hitBuffer, maxSweepLength, trackMask);

        bool foundValidHit = false;
        RaycastHit validHit = default;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
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
            hitDistance = validHit.distance + castRadius - wheelRadius - rayOriginOffset;
            contactPoint = validHit.point;
            contactNormal = validHit.normal;
        }
        else
        {
            isGrounded = false;
            hitDistance = maxSuspensionLength;

            contactPoint = mountPos - (mountUp * hitDistance);
            contactNormal = Vector3.up;
        }
    }
}