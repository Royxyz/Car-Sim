using UnityEngine;

[System.Serializable]
public class ChassisManager
{
    public ChassisData chassisData;
    public SteeringData steeringData;
    public AntiRollBar antiRollBar = new AntiRollBar();
    public WheelAssembly[] corners = new WheelAssembly[4];

    [Range(0f, 1f)] public float brakeBias = 0.65f;
    public LayerMask trackMask = ~0;

    private Vector3[] localMountPositions = new Vector3[4];
    private Vector3[] localMountUps = new Vector3[4];
    private float previousSteerTarget = 0f;

    public void Initialize(Rigidbody rb, Transform root)
    {
        float frontZ = root.InverseTransformPoint(corners[0].suspensionMountPoint.position).z - rb.centerOfMass.z;
        float rearZ = root.InverseTransformPoint(corners[2].suspensionMountPoint.position).z - rb.centerOfMass.z;

        float wheelbase = Mathf.Abs(frontZ) + Mathf.Abs(rearZ);
        float frontWeightRatio = Mathf.Abs(rearZ) / wheelbase;
        float rearWeightRatio = Mathf.Abs(frontZ) / wheelbase;

        for (int i = 0; i < 4; i++)
        {
            if (corners[i] != null)
            {
                float cornerMass = (i < 2) ? (rb.mass * frontWeightRatio) / 2f : (rb.mass * rearWeightRatio) / 2f;
                corners[i].Initialize(cornerMass);

                localMountPositions[i] = root.InverseTransformPoint(corners[i].suspensionMountPoint.position) - rb.centerOfMass;
                localMountUps[i] = root.InverseTransformDirection(corners[i].suspensionMountPoint.up);
            }
        }
    }

    public void UpdateSteeringInterpolation(float newTargetSteer)
    {
        previousSteerTarget = newTargetSteer;
    }

    public void ProcessSubStep(VirtualDynamics vChassis, Transform root, float targetSteer, float stepFraction, float[] driveTorques, float activeBrake, float activeHandbrake, float dt)
    {
        float subStepSteer = Mathf.Lerp(previousSteerTarget, targetSteer, stepFraction);

        for (int i = 0; i < 4; i++)
        {
            WheelAssembly corner = corners[i];

            Vector3 mountWorldPos = vChassis.position + (vChassis.rotation * localMountPositions[i]);
            Vector3 mountUp = vChassis.rotation * localMountUps[i];
            float maxSuspLength = corner.suspension.suspData.targetRideHeight + corner.suspension.suspData.droopTravel;

            corner.contact.EvaluateContact(root, mountWorldPos, mountUp, maxSuspLength, corner.wheel.wheelData.radius, trackMask);

            if (corner.isSteerable)
            {
                float ackermannModifier = (Mathf.Sign(subStepSteer) == (i % 2 == 0 ? -1 : 1))
                    ? steeringData.ackermannInnerMultiplier : steeringData.ackermannOuterMultiplier;
                corner.ackermannSteeringAngle = subStepSteer * ackermannModifier;
            }

            ProcessCornerForces(i, corner, vChassis, mountWorldPos, mountUp, driveTorques[i], activeBrake, activeHandbrake, dt);
        }

        ApplySubStepARB(vChassis, corners[0], corners[1], antiRollBar.antiRollBarData.frontAntiRoll, 0, 1);
        ApplySubStepARB(vChassis, corners[2], corners[3], antiRollBar.antiRollBarData.rearAntiRoll, 2, 3);
    }

    private void ProcessCornerForces(int index, WheelAssembly corner, VirtualDynamics vChassis, Vector3 mountPos, Vector3 mountUp, float driveTorque, float activeBrake, float activeHandbrake, float dt)
    {
        float maxSuspLength = corner.suspension.suspData.targetRideHeight + corner.suspension.suspData.droopTravel;
        float expectedLength = corner.contact.isGrounded ? corner.contact.hitDistance : maxSuspLength;
        float compressionVel = (corner.suspension.currentLength - expectedLength) / dt;

        float suspForceMag = corner.suspension.CalculateForce(corner.contact.isGrounded, corner.contact.hitDistance, compressionVel);
        Vector3 suspensionForceWorld = mountUp * suspForceMag;
        Vector3 gripForceWorld = Vector3.zero;

        float biasMultiplier = (index < 2) ? (brakeBias * 2f) : ((1f - brakeBias) * 2f);
        float cornerHandbrake = (index > 1) ? activeHandbrake : 0f;

        if (corner.contact.isGrounded)
        {
            Vector3 contactRadius = corner.contact.contactPoint - vChassis.position;
            Vector3 contactVelWorld = vChassis.linearVelocity + Vector3.Cross(vChassis.angularVelocity, contactRadius);

            float compDist = corner.suspension.suspData.targetRideHeight - corner.suspension.currentLength;
            float dynamicToe = compDist * corner.suspension.suspData.bumpSteerPerMeter;
            if (index == 1 || index == 3) dynamicToe = -dynamicToe;

            float totalSteer = corner.ackermannSteeringAngle + dynamicToe;
            Quaternion wheelRot = vChassis.rotation * Quaternion.Euler(0, totalSteer, 0);

            Vector3 contactVelPlanar = Vector3.ProjectOnPlane(contactVelWorld, corner.contact.contactNormal);
            Vector3 contactVelLocal = Quaternion.Inverse(wheelRot) * contactVelPlanar;

            corner.wheel.CalculateSlips(contactVelLocal);

            float brakeTorque = corner.brake.CalculateBrakeTorque(activeBrake, cornerHandbrake, biasMultiplier, corner.wheel.longitudinalSlip);
            float effectiveFrictionLoad = Mathf.Max(0f, suspForceMag);

            Vector2 gripForceLocal = corner.tire.CalculateGripForces(
                effectiveFrictionLoad, corner.wheel.longitudinalSlip, corner.wheel.slipAngle,
                corner.wheel.forwardSpeed, corner.wheel.wheelLinearSpeed, dt
            );

            Vector3 gripDirLong = Vector3.ProjectOnPlane(wheelRot * Vector3.forward, corner.contact.contactNormal).normalized;
            Vector3 gripDirLat = Vector3.ProjectOnPlane(wheelRot * Vector3.right, corner.contact.contactNormal).normalized;

            gripForceWorld = (gripDirLong * gripForceLocal.x) + (gripDirLat * gripForceLocal.y);
            float rollingResForce = corner.tire.GetRollingResistanceForce(effectiveFrictionLoad);

            if (Mathf.Abs(corner.wheel.forwardSpeed) > 0.1f)
            {
                gripForceWorld += gripDirLong * (-Mathf.Sign(corner.wheel.forwardSpeed) * rollingResForce);
            }

            corner.wheel.UpdatePhysics(driveTorque, brakeTorque, gripForceLocal.x * corner.wheel.wheelData.radius, dt, rollingResForce * corner.wheel.wheelData.radius);

            vChassis.AddForceAtPosition(gripForceWorld, corner.contact.contactPoint);
        }
        else
        {
            corner.wheel.UpdatePhysics(driveTorque, corner.brake.CalculateBrakeTorque(activeBrake, cornerHandbrake, biasMultiplier, 0f), 0f, dt, 0f);
        }

        vChassis.AddForceAtPosition(suspensionForceWorld, mountPos);
    }

    private void ApplySubStepARB(VirtualDynamics vChassis, WheelAssembly left, WheelAssembly right, float stiffness, int idxL, int idxR)
    {
        float travelL = left.suspension.suspData.targetRideHeight - left.suspension.currentLength;
        float travelR = right.suspension.suspData.targetRideHeight - right.suspension.currentLength;

        float difference = travelL - travelR;
        float antiRollForce = difference * stiffness;

        if (left.suspension.isGrounded)
        {
            Vector3 mountWorldPos = vChassis.position + (vChassis.rotation * localMountPositions[idxL]);
            Vector3 mountUp = vChassis.rotation * localMountUps[idxL];
            vChassis.AddForceAtPosition(mountUp * antiRollForce, mountWorldPos);
        }

        if (right.suspension.isGrounded)
        {
            Vector3 mountWorldPos = vChassis.position + (vChassis.rotation * localMountPositions[idxR]);
            Vector3 mountUp = vChassis.rotation * localMountUps[idxR];
            vChassis.AddForceAtPosition(mountUp * -antiRollForce, mountWorldPos);
        }
    }
}