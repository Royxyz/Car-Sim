using UnityEngine;

[System.Serializable]
public class Wheel
{
    [SerializeField] public WheelData wheelData;

    public float angularVelocity { get; private set; }
    public float rotationAngle { get; private set; }
    
    // New Data Properties
    public float longitudinalSlip { get; private set; }
    public float slipAngle { get; private set; }

    public void Initialize()
    {
        angularVelocity = 0f;
        rotationAngle = 0f;
        longitudinalSlip = 0f;
        slipAngle = 0f;
    }

    public void CalculateSlips(Vector3 contactPatchLocalVelocity)
        {
            float forwardSpeed = contactPatchLocalVelocity.z;
            float lateralSpeed = contactPatchLocalVelocity.x;
            float wheelLinearSpeed = angularVelocity * wheelData.radius;

            float speedThreshold = 0.1f;
            float absForward = Mathf.Max(Mathf.Abs(forwardSpeed), speedThreshold);

            // Fades in grip smoothly at low speeds
            float slipDampener = Mathf.Clamp01(absForward / 2.0f); 
            
            slipAngle = Mathf.Atan2(lateralSpeed, absForward) * slipDampener;
            longitudinalSlip = ((wheelLinearSpeed - forwardSpeed) / absForward) * slipDampener;
            
            // Do NOT add the if/else block here. Let the dampener do its job.
        }

    public void UpdatePhysics(float driveTorque, float brakeTorque, float tireGripTorque, float dt)
    {
        float directionalBrakeTorque = 0f;

        if (Mathf.Abs(angularVelocity) > 0.01f)
        {
            directionalBrakeTorque = Mathf.Sign(angularVelocity) * brakeTorque;
        }

        float netTorque = driveTorque - directionalBrakeTorque - tireGripTorque;

        float angularAcceleration = netTorque / wheelData.inertia;
        angularVelocity += angularAcceleration * dt;

        if (brakeTorque > 0f && Mathf.Abs(angularVelocity) < 0.1f && Mathf.Abs(netTorque) < brakeTorque)
        {
            angularVelocity = 0f;
        }

        rotationAngle += angularVelocity * dt;
        rotationAngle %= 2f * Mathf.PI;
    }
}