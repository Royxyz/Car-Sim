using UnityEngine;

[System.Serializable]
public class Wheel
{
    [SerializeField] public WheelData wheelData;

    public float angularVelocity { get; private set; }
    public float rotationAngle { get; private set; }
    
    public float longitudinalSlip { get; private set; }
    public float slipAngle { get; private set; }

    public float forwardSpeed { get; private set; }
    public float wheelLinearSpeed { get; private set; }

    public void Initialize()
    {
        angularVelocity = 0f;
        rotationAngle = 0f;
        longitudinalSlip = 0f;
        slipAngle = 0f;
        forwardSpeed = 0f;
        wheelLinearSpeed = 0f;
    }

    public void CalculateSlips(Vector3 contactPatchLocalVelocity)
    {
        forwardSpeed = contactPatchLocalVelocity.z;
        float lateralSpeed = contactPatchLocalVelocity.x;
        wheelLinearSpeed = angularVelocity * wheelData.radius;

        float speedThreshold = 0.5f; 
        float absForward = Mathf.Max(Mathf.Abs(forwardSpeed), speedThreshold);

        slipAngle = Mathf.Atan2(-lateralSpeed, absForward);
        longitudinalSlip = (wheelLinearSpeed - forwardSpeed) / absForward;
    }

    public void UpdatePhysics(float driveTorque, float brakeTorque, float tireGripTorque, float dt, float rollingResTorque = 0f, float upstreamDrivelineInertia = 0f)
    {
        float directionalBrakeTorque = 0f;
        float directionalRRTorque = 0f;

        if (Mathf.Abs(angularVelocity) > 0.01f)
        {
            directionalBrakeTorque = Mathf.Sign(angularVelocity) * brakeTorque;
            directionalRRTorque = Mathf.Sign(angularVelocity) * rollingResTorque;
        }
        else if (brakeTorque > 0f) 
        {
            directionalBrakeTorque = Mathf.Sign(tireGripTorque) * Mathf.Min(brakeTorque, Mathf.Abs(tireGripTorque));
        }
        
        float netTorque = driveTorque - directionalBrakeTorque - directionalRRTorque - tireGripTorque;

        float totalEffectiveInertia = wheelData.inertia + upstreamDrivelineInertia;
        
        float angularAcceleration = netTorque / totalEffectiveInertia;
        angularVelocity += angularAcceleration * dt;

        float totalResistiveTorque = brakeTorque + rollingResTorque;
        if (totalResistiveTorque > 0f && Mathf.Abs(angularVelocity) < 0.1f && Mathf.Abs(driveTorque - tireGripTorque) < totalResistiveTorque)
        {
            angularVelocity = 0f;
        }

        rotationAngle += angularVelocity * dt;
        rotationAngle %= 2f * Mathf.PI;
    }
}