using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class CoastalAmbienceManager : MonoBehaviour
{
    [Header("FMOD Settings")]
    public EventReference coastalAmbienceEvent;
    private EventInstance ambienceInstance;

    [Header("Track References")]
    [Tooltip("Drag the car's Rigidbody here.")]
    public Rigidbody carRb;
    
    [Header("Seaside Straight Markers")]
    public Transform straightEntry;
    public Transform straightExit;
    
    [Tooltip("How far inland (in meters) you can hear the ocean before it fades out entirely.")]
    public float maxSeaHearingDistance = 400f;

    void Start()
    {
        if (!coastalAmbienceEvent.IsNull)
        {
            ambienceInstance = RuntimeManager.CreateInstance(coastalAmbienceEvent);
            ambienceInstance.start();
        }
    }

    void Update()
    {
        if (carRb == null || straightEntry == null || straightExit == null) return;
        if (!ambienceInstance.isValid()) return;

        UpdateWindAudio();
        UpdateSeaProximity();
    }

    private void UpdateWindAudio()
    {
        float speedKmh = carRb.linearVelocity.magnitude * 3.6f;
        ambienceInstance.setParameterByName("CarSpeed", speedKmh);
    }

    private void UpdateSeaProximity()
    {
        Vector3 entryPos = straightEntry.position;
        Vector3 exitPos = straightExit.position;
        Vector3 carPos = carRb.position;

        Vector3 lineDirection = (exitPos - entryPos).normalized;
        float lineLength = Vector3.Distance(entryPos, exitPos);

        Vector3 carToEntry = carPos - entryPos;
        float projectedDistance = Vector3.Dot(carToEntry, lineDirection);

        projectedDistance = Mathf.Clamp(projectedDistance, 0f, lineLength);
        Vector3 closestPointOnStraight = entryPos + (lineDirection * projectedDistance);
        float distanceToSea = Vector3.Distance(carPos, closestPointOnStraight);

        float seaProximity = 1.0f - Mathf.Clamp01(distanceToSea / maxSeaHearingDistance);

        ambienceInstance.setParameterByName("SeaProximity", seaProximity);
    }

    void OnDestroy()
    {
        if (ambienceInstance.isValid())
        {
            ambienceInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            ambienceInstance.release();
        }
    }

    private void OnDrawGizmos()
    {
        if (straightEntry != null && straightExit != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(straightEntry.position, straightExit.position);
            Gizmos.DrawWireSphere(straightEntry.position, 5f);
            Gizmos.DrawWireSphere(straightExit.position, 5f);
        }
    }
}