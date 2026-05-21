using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SimulationController))]
public class SkidmarkManager : MonoBehaviour
{
    [Header("Dependencies")]
    public TrailRenderer skidTrailPrefab;
    private SimulationController sim;

    [Header("Thresholds")]
    [Tooltip("Multiplier of the tire's optimal slip before it starts drawing a line.")]
    public float slipThresholdMultiplier = 1.2f;
    [Tooltip("Offset above the ground to prevent Z-fighting (meters).")]
    public float groundOffset = 0.02f;

    // We track the active trail for each of the 4 wheels
    private TrailRenderer[] activeTrails = new TrailRenderer[4];
    private Transform[] emitPoints = new Transform[4];

    // Object Pool to prevent Instantiating/Destroying during gameplay
    private Queue<TrailRenderer> trailPool = new Queue<TrailRenderer>();

    private void Awake()
    {
        sim = GetComponent<SimulationController>();

        // Create invisible emit points for each wheel
        for (int i = 0; i < 4; i++)
        {
            emitPoints[i] = new GameObject($"SkidEmitPoint_{i}").transform;
            emitPoints[i].SetParent(transform);
        }

        // Pre-warm the pool with 20 trails
        for (int i = 0; i < 20; i++)
        {
            trailPool.Enqueue(CreateNewTrail());
        }
    }

    private void LateUpdate()
    {
        if (sim.corners == null || sim.corners.Length != 4) return;

        for (int i = 0; i < 4; i++)
        {
            WheelAssembly corner = sim.corners[i];
            
            // 1. Calculate if we are exceeding the optimal grip limits
            float optimalLongSlip = 1f / corner.tire.tireData.longB;
            float optimalLatSlip = (1f / corner.tire.tireData.latB) * Mathf.Rad2Deg;

            float currentLongSlip = Mathf.Abs(corner.wheel.longitudinalSlip);
            float currentLatSlip = Mathf.Abs(corner.wheel.slipAngle * Mathf.Rad2Deg);

            bool isSkidding = (currentLongSlip > optimalLongSlip * slipThresholdMultiplier) || 
                              (currentLatSlip > optimalLatSlip * slipThresholdMultiplier);
                              
            // Brake lockup override (ABS off)
            if (Mathf.Abs(corner.wheel.angularVelocity) < 1f && Mathf.Abs(corner.wheel.forwardSpeed) > 2f) 
                isSkidding = true;

            // 2. Manage the Trail State
            if (isSkidding && corner.contact.isGrounded)
            {
                if (activeTrails[i] == null)
                {
                    StartSkid(i);
                }
                UpdateSkidPosition(i, corner);
            }
            else
            {
                if (activeTrails[i] != null)
                {
                    EndSkid(i);
                }
            }
        }
    }

    private void StartSkid(int wheelIndex)
    {
        if (trailPool.Count == 0) trailPool.Enqueue(CreateNewTrail());
        
        TrailRenderer trail = trailPool.Dequeue();
        trail.gameObject.SetActive(true);
        trail.Clear(); // Nuke any old vertices
        trail.emitting = true;
        
        activeTrails[wheelIndex] = trail;
    }

    private void UpdateSkidPosition(int wheelIndex, WheelAssembly corner)
    {
        Transform emit = emitPoints[wheelIndex];
        
        // Push slightly above ground normal to stop Z-fighting
        emit.position = corner.contact.contactPoint + (corner.contact.contactNormal * groundOffset);

        // CRITICAL FOR "TRANSFORM Z" ALIGNMENT:
        // Z-Axis must point UP away from the road. 
        // Y-Axis must point FORWARD along the wheel's travel.
        // This makes the Trail width stretch flat across the X-Axis on the asphalt.
        Vector3 wheelForward = sim.transform.forward; // You can refine this to the visualMesh.forward if wheels steer
        if (corner.visualMesh != null) wheelForward = corner.visualMesh.forward;
        
        Vector3 projectedForward = Vector3.ProjectOnPlane(wheelForward, corner.contact.contactNormal).normalized;
        
        emit.rotation = Quaternion.LookRotation(corner.contact.contactNormal, projectedForward);

        // Snap the active trail to this dummy transform
        activeTrails[wheelIndex].transform.position = emit.position;
        activeTrails[wheelIndex].transform.rotation = emit.rotation;
    }

    private void EndSkid(int wheelIndex)
    {
        TrailRenderer trail = activeTrails[wheelIndex];
        trail.emitting = false;
        
        // We do NOT destroy it, and we do NOT immediately pool it, otherwise it vanishes instantly.
        // We let it sit in the world. A coroutine or invoke will reclaim it after its lifetime expires.
        StartCoroutine(ReclaimTrail(trail, trail.time));
        
        activeTrails[wheelIndex] = null;
    }

    private System.Collections.IEnumerator ReclaimTrail(TrailRenderer trail, float delay)
    {
        yield return new WaitForSeconds(delay);
        trail.gameObject.SetActive(false);
        trail.transform.SetParent(transform); // Re-parent for tidiness
        trailPool.Enqueue(trail);
    }

    private TrailRenderer CreateNewTrail()
    {
        TrailRenderer tr = Instantiate(skidTrailPrefab, transform);
        tr.gameObject.SetActive(false);
        return tr;
    }
}