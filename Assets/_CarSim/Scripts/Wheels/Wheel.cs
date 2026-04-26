using UnityEngine;

[System.Serializable]
public class Wheel
{
    [SerializeField] public WheelData wheelData;

    public float angularVelocity { get; private set; }
    public float rotationAngle { get; private set; }

    public void Initialize()
    {
        angularVelocity = 0f;
        rotationAngle = 0f;
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