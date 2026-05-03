using UnityEngine;

[System.Serializable]
public class WheelAssembly
{
    [Header("Hierarchy & Spatial")]
    [Tooltip("The empty GameObject representing where the suspension attaches to the chassis.")]
    public Transform suspensionMountPoint; 
    [Tooltip("The graphical mesh of the wheel/tire.")]
    public Transform visualMesh;

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
    
    // FIX 5: State variables for visual interpolation
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

        visualMesh.position = suspensionMountPoint.position - (suspensionMountPoint.up * smoothedSuspensionLength);

        Quaternion steerRotation = Quaternion.AngleAxis(ackermannSteeringAngle, suspensionMountPoint.up);
        Quaternion spinRotation = Quaternion.AngleAxis(wheel.rotationAngle * Mathf.Rad2Deg, Vector3.right); 
        Quaternion targetRotation = suspensionMountPoint.rotation * steerRotation * spinRotation;

        smoothedRotation = Quaternion.Slerp(smoothedRotation, targetRotation, Time.deltaTime * lerpSpeed);
        visualMesh.rotation = smoothedRotation;
    }
}