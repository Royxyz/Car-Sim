using UnityEngine;

[System.Serializable]
public class WheelAssembly
{
    [Header("Hierarchy & Spatial")]
    [Tooltip("The empty GameObject representing where the suspension attaches to the chassis.")]
    public Transform suspensionMountPoint; 
    
    [Tooltip("The main graphical mesh. If 'Wheel Spin Mesh' is assigned, this acts as the non-spinning hub/enclosure (steer & camber only).")]
    public Transform visualMesh;

    [Tooltip("(Optional) The specific mesh that rotates. If left empty, 'Visual Mesh' will handle the spinning.")]
    public Transform wheelSpinMesh;

    [Tooltip("Correction for mesh export orientations (e.g., set X to -90 for Blender meshes). Leave at 0,0,0 for your standard cars.")]
    public Vector3 meshRotationOffset = Vector3.zero;

    [Header("Corner Configurations")]
    public bool isSteerable = false;
    public float ackermannSteeringAngle = 0f; 

    [Header("Physics Modules")]
    public Suspension suspension;
    public Wheel wheel;
    public Brake brake;
    public TireFrictionModel tire;

    [Tooltip("Handles track detection and raycasting.")]
    public WheelContact contact = new WheelContact();

    public Vector3 lastCalculatedForce { get; private set; }
    
    private float smoothedSuspensionLength;
    private Quaternion smoothedRotation;

    public void Initialize(float vehicleMass)
    {
        suspension.Initialize(vehicleMass);
        wheel.Initialize();
        brake.Initialize();
        
        if (suspension.suspData != null)
        {
            smoothedSuspensionLength = suspension.suspData.targetRideHeight;
        }
        
        if (visualMesh != null)
        {
            smoothedRotation = visualMesh.rotation;
        }
    }

    public void UpdateVisuals()
    {
        if (visualMesh == null || suspensionMountPoint == null) return;

        float lerpSpeed = 25f;
        smoothedSuspensionLength = Mathf.Lerp(smoothedSuspensionLength, suspension.currentLength, Time.deltaTime * lerpSpeed);

        Vector3 targetPosition = suspensionMountPoint.position - (suspensionMountPoint.up * smoothedSuspensionLength);
        visualMesh.position = targetPosition;

        float compressionDistance = suspension.suspData.targetRideHeight - smoothedSuspensionLength;
        float camberAngle = -compressionDistance * suspension.suspData.camberGainPerMeter;

        Quaternion steerRotation = Quaternion.AngleAxis(ackermannSteeringAngle, Vector3.up);
        Quaternion camberRotation = Quaternion.AngleAxis(camberAngle, Vector3.forward);
        Quaternion spinRotation = Quaternion.AngleAxis(wheel.rotationAngle * Mathf.Rad2Deg, Vector3.right); 

        Quaternion meshCorrection = Quaternion.Euler(meshRotationOffset); 

        if (wheelSpinMesh != null)
        {
            Quaternion hubTargetRotation = suspensionMountPoint.rotation * steerRotation * camberRotation;
            smoothedRotation = Quaternion.Slerp(smoothedRotation, hubTargetRotation, Time.deltaTime * lerpSpeed);

            visualMesh.rotation = smoothedRotation * meshCorrection; 

            wheelSpinMesh.position = targetPosition;
            wheelSpinMesh.rotation = smoothedRotation * spinRotation * meshCorrection; 
        }
        else
        {
            Quaternion fullTargetRotation = suspensionMountPoint.rotation * steerRotation * camberRotation * spinRotation;
            smoothedRotation = Quaternion.Slerp(smoothedRotation, fullTargetRotation, Time.deltaTime * lerpSpeed);

            visualMesh.rotation = smoothedRotation * meshCorrection; 
        }
    }
}