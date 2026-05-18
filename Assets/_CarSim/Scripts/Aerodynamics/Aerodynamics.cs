using UnityEngine;

public struct AeroForces
{
    public float frontDownforce;
    public float rearDownforce;
    public Vector3 totalDragWorld;
    public Vector3 totalSideforceWorld;
}

[System.Serializable]
public class Aerodynamics
{
    [SerializeField] public AeroData aeroData;

    public AeroForces CalculateForces(Vector3 carVelocityWorld, Transform carTransform, float frontRideHeight, float rearRideHeight, Vector3 ambientWindWorld = default)
    {
        AeroForces result = new AeroForces();
        
        Vector3 airVelocityWorld = carVelocityWorld - ambientWindWorld;
        Vector3 localAirVelocity = carTransform.InverseTransformDirection(airVelocityWorld);

        float speedSquare = localAirVelocity.sqrMagnitude;
        if (speedSquare < 0.1f) return result;

        float dynamicPressure = 0.5f * aeroData.airDensity * speedSquare;

        float pitchAngle = Mathf.Atan2(-localAirVelocity.y, localAirVelocity.z) * Mathf.Rad2Deg; 
        float slipAngle = Mathf.Atan2(localAirVelocity.x, localAirVelocity.z) * Mathf.Rad2Deg;

        float frontCoef = aeroData.frontBaseDownforceCoef + (-pitchAngle * aeroData.frontPitchSensitivity);
        float rearCoef = aeroData.rearBaseDownforceCoef - (-pitchAngle * aeroData.rearPitchSensitivity);

        float avgRideHeight = Mathf.Max((frontRideHeight + rearRideHeight) * 0.5f, 0.01f);
        float geEfficiency = Mathf.Clamp01(aeroData.optimalRideHeight / avgRideHeight);
        float groundEffectCoef = aeroData.maxGroundEffectCoef * geEfficiency;

        frontCoef += groundEffectCoef * aeroData.groundEffectBias;
        rearCoef += groundEffectCoef * (1- aeroData.groundEffectBias);

        result.frontDownforce = dynamicPressure * aeroData.planformArea * frontCoef;
        result.rearDownforce = dynamicPressure * aeroData.planformArea * rearCoef;

        float dragCoef = aeroData.baseDragCoef + 
                         (Mathf.Abs(pitchAngle) * aeroData.pitchDragSensitivity) + 
                         (Mathf.Abs(slipAngle) * aeroData.yawDragSensitivity);
                         
        float dragMagnitude = dynamicPressure * aeroData.frontalArea * dragCoef;

        result.totalDragWorld = -airVelocityWorld.normalized * dragMagnitude;

        float sideforceCoef = slipAngle * aeroData.yawSideforceSensitivity;
        float sideForceMagnitude = dynamicPressure * aeroData.sideArea * sideforceCoef;

        Vector3 localSideforce = new Vector3(-sideForceMagnitude, 0f, 0f);
        result.totalSideforceWorld = carTransform.TransformDirection(localSideforce);

        return result;
    }
}