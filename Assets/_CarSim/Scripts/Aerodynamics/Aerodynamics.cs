using UnityEngine;

[System.Serializable]
public class Aerodynamics
{
    [SerializeField] public AeroData aeroData;

    public Vector3 CalculateAerodynamicForces(Vector3 carVelocityWorld, Transform carTransform, Vector3 ambientWindWorld = default)
    {
        Vector3 airVelocityWorld = carVelocityWorld - ambientWindWorld;
        Vector3 localAirVelocity = carTransform.InverseTransformDirection(airVelocityWorld);

        float speedSquare = localAirVelocity.sqrMagnitude;
        if (speedSquare < 0.1f) return Vector3.zero;

        float dynamicPressure = 0.5f * aeroData.airDensity * speedSquare;
  
        float aoa = Mathf.Atan2(-localAirVelocity.y, Mathf.Abs(localAirVelocity.z)) * Mathf.Rad2Deg; 
        float slipAngle = Mathf.Atan2(localAirVelocity.x, localAirVelocity.z) * Mathf.Rad2Deg;

        float cL = aeroData.downforceVsAoA.Evaluate(aoa);
        float cD = aeroData.dragVsAoA.Evaluate(aoa);
        float cS = aeroData.sideforceVsSlipAngle.Evaluate(slipAngle);

        float downforce = dynamicPressure * aeroData.topArea * cL;

        Vector3 centerOfPressureWorld = carTransform.TransformPoint(aeroData.centerOfPressureOffset);
        float actualRideHeight = aeroData.optimalRideHeight;

        int trackLayerMask = ~LayerMask.GetMask("Vehicle"); 
        if (Physics.Raycast(centerOfPressureWorld, -Vector3.up, out RaycastHit hit, 2.0f, trackLayerMask))
        {
            actualRideHeight = hit.distance - aeroData.centerOfPressureOffset.y;
            actualRideHeight = Mathf.Max(actualRideHeight, 0.01f); 
        }

        float rideHeightFactor = Mathf.Clamp01(aeroData.optimalRideHeight / actualRideHeight);
        downforce += (downforce * rideHeightFactor * aeroData.groundEffectMultiplier);

        float drag = dynamicPressure * aeroData.frontalArea * cD;
        drag *= Mathf.Sign(localAirVelocity.z);

        float sideForce = dynamicPressure * aeroData.sideArea * cS;

        return new Vector3(-sideForce, -downforce, -drag);
    }
}