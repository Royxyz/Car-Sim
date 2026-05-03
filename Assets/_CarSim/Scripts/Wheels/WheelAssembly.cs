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

    public void Initialize()
    {
        suspension.Initialize();
        wheel.Initialize();
        brake.Initialize();
    }

    public void UpdateVisuals()
    {
        if (visualMesh == null || suspensionMountPoint == null) return;

        visualMesh.position = suspensionMountPoint.position - (suspensionMountPoint.up * suspension.currentLength);

        Quaternion steerRotation = Quaternion.AngleAxis(ackermannSteeringAngle, suspensionMountPoint.right);
        Quaternion spinRotation = Quaternion.AngleAxis(wheel.rotationAngle * Mathf.Rad2Deg, Vector3.down); // Assuming X is the axle
        
        visualMesh.rotation = suspensionMountPoint.rotation * steerRotation * spinRotation;
    }
}