using UnityEngine;

[System.Serializable]
public class AntiRollBar
{
    public float frontAntiRoll = 5000f;
    public float rearAntiRoll = 3000f;

    public void ApplyAntiRollBars(WheelAssembly[] corners, Rigidbody rb)
    {
        // Front Axle (FL = 0, FR = 1)
        ApplyAxleARB(corners[0], corners[1], frontAntiRoll, rb);
        // Rear Axle (RL = 2, RR = 3)
        ApplyAxleARB(corners[2], corners[3], rearAntiRoll, rb);
    }

    public void ApplyAxleARB(WheelAssembly left, WheelAssembly right, float stiffness, Rigidbody rb)
    {
        float travelL = left.suspension.suspData.restLength - left.suspension.currentLength;
        float travelR = right.suspension.suspData.restLength - right.suspension.currentLength;

        // Positive means left is more compressed than right
        float difference = travelL - travelR;
        float antiRollForce = difference * stiffness;

        if (left.suspension.isGrounded)
            rb.AddForceAtPosition(left.suspensionMountPoint.up * -antiRollForce, left.suspensionMountPoint.position);
        if (right.suspension.isGrounded)
            rb.AddForceAtPosition(right.suspensionMountPoint.up * antiRollForce, right.suspensionMountPoint.position);
    }
}
