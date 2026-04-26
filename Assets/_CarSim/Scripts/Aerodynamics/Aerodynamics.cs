using UnityEngine;

[System.Serializable]
public class Aerodynamics
{
    [SerializeField] public AeroData aeroData;

    public Vector3 CalculateAerodynamicForces(Vector3 localVelocity)
    {
        float speedForward = localVelocity.z;
        float speedLateral = localVelocity.x;

        float forwardDragForce = 0.5f * aeroData.airDensity * aeroData.frontalArea * aeroData.dragCoefficientFront * (speedForward * speedForward);
        forwardDragForce *= -Mathf.Sign(speedForward);

        float lateralDragForce = 0.5f * aeroData.airDensity * aeroData.sideArea * aeroData.dragCoefficientSide * (speedLateral * speedLateral);
        lateralDragForce *= -Mathf.Sign(speedLateral);

        float downforce = 0.5f * aeroData.airDensity * aeroData.topArea * aeroData.downforceCoefficient * (speedForward * speedForward);
        
        if (speedForward < 0f)
        {
            downforce = 0f; 
        }

        return new Vector3(lateralDragForce, -downforce, forwardDragForce);
    }
}