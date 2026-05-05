using UnityEngine;

[System.Serializable]
public class AntiRollBar
{
   [SerializeField] public AntiRollBarData antiRollBarData;
    public void ApplyAntiRollBars(WheelAssembly[] corners, Rigidbody rb)
    {
        // Front Axle (FL = 0, FR = 1)
        ApplyAxleARB(corners[0], corners[1], antiRollBarData.frontAntiRoll, rb);
        // Rear Axle (RL = 2, RR = 3)
        ApplyAxleARB(corners[2], corners[3], antiRollBarData.rearAntiRoll, rb);
    }

    public void ApplyAxleARB(WheelAssembly left, WheelAssembly right, float stiffness, Rigidbody rb)
    {
        float travelL = left.suspension.suspData.targetRideHeight - left.suspension.currentLength;
        float travelR = right.suspension.suspData.targetRideHeight - right.suspension.currentLength;

        float difference = travelL - travelR;
        float antiRollForce = difference * stiffness;

        if (left.suspension.isGrounded)
            rb.AddForceAtPosition(left.suspensionMountPoint.up * antiRollForce, left.suspensionMountPoint.position); // Removed '-'
        if (right.suspension.isGrounded)
            rb.AddForceAtPosition(right.suspensionMountPoint.up * -antiRollForce, right.suspensionMountPoint.position); // Added '-'

        
    }
}
