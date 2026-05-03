using UnityEngine;

[System.Serializable]
public class WheelContact
{
    [Header("Settings")]
    [Tooltip("How far above the mount point to start the raycast (prevents the ray from starting underground on hard bottom-outs).")]
    public float rayOriginOffset = 1.0f;

    public bool isGrounded { get; private set; }
    public float hitDistance { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 contactNormal { get; private set; }

    private RaycastHit[] hitBuffer = new RaycastHit[10];

    public void EvaluateContact(Transform vehicleRoot, Vector3 mountPos, Vector3 mountUp, float suspensionRestLength, float suspensionMaxTravel, float wheelRadius, LayerMask trackMask)
    {
        Vector3 rayStartPos = mountPos + (mountUp * rayOriginOffset);
        float maxRayLength = suspensionRestLength + suspensionMaxTravel + wheelRadius + rayOriginOffset;

        int hitCount = Physics.RaycastNonAlloc(rayStartPos, -mountUp, hitBuffer, maxRayLength, trackMask);
        
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
            hitDistance = validHit.distance - wheelRadius - rayOriginOffset;
            contactPoint = validHit.point;
            contactNormal = validHit.normal;
        }
        else
        {
            isGrounded = false;
            hitDistance = suspensionRestLength + suspensionMaxTravel;

            contactPoint = mountPos - (mountUp * hitDistance);
            contactNormal = Vector3.up;
        }
    }
}