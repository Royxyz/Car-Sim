using UnityEngine;

[System.Serializable]
public class VirtualDynamics
{
    public Vector3 position { get; private set; }
    public Quaternion rotation { get; private set; }
    public Vector3 linearVelocity { get; private set; }
    public Vector3 angularVelocity { get; private set; }

    private Vector3 stepTotalForce;
    private Vector3 stepTotalTorque;

    public void SyncFromRigidbody(Rigidbody rb)
    {
        position = rb.worldCenterOfMass;
        rotation = rb.rotation;
        linearVelocity = rb.linearVelocity;
        angularVelocity = rb.angularVelocity;
    }

    public void SyncToRigidbody(Rigidbody rb)
    {
        rb.linearVelocity = linearVelocity;
        rb.angularVelocity = angularVelocity;
    }

    public void ResetStepAccumulators()
    {
        stepTotalForce = Vector3.zero;
        stepTotalTorque = Vector3.zero;
    }

    public void AddForceAtPosition(Vector3 force, Vector3 worldPos)
    {
        stepTotalForce += force;
        Vector3 radiusFromCoM = worldPos - position;
        stepTotalTorque += Vector3.Cross(radiusFromCoM, force);
    }

    public void IntegrateStep(float dt, Rigidbody rb, float maxSafeTorque)
    {
        stepTotalForce += Physics.gravity * rb.mass;

        linearVelocity += (stepTotalForce / rb.mass) * dt;
        position += linearVelocity * dt;

        if (stepTotalTorque.magnitude > maxSafeTorque)
        {
            stepTotalTorque = stepTotalTorque.normalized * maxSafeTorque;
        }

        Vector3 localTorque = Quaternion.Inverse(rotation) * stepTotalTorque;
        Vector3 localAngAccel = new Vector3(
            localTorque.x / rb.inertiaTensor.x,
            localTorque.y / rb.inertiaTensor.y,
            localTorque.z / rb.inertiaTensor.z
        );

        angularVelocity += (rotation * localAngAccel) * dt;

        Quaternion qVel = new Quaternion(angularVelocity.x, angularVelocity.y, angularVelocity.z, 0f) * rotation;
        Quaternion tempRot = rotation; 

        tempRot.x += 0.5f * qVel.x * dt;
        tempRot.y += 0.5f * qVel.y * dt;
        tempRot.z += 0.5f * qVel.z * dt;
        tempRot.w += 0.5f * qVel.w * dt;

        tempRot.Normalize();
  
        rotation = tempRot;
    }
}