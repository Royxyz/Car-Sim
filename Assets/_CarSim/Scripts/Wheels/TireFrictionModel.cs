using UnityEngine;

[System.Serializable]
public class TireFrictionModel
{
    [SerializeField] public TireData tireData;

    public Vector2 CalculateGripForces(float normalLoad, float longitudinalSlip, float slipAngle)
    {
        if (normalLoad <= 0f) return Vector2.zero;

        float Fx = CalculatePacejka(longitudinalSlip, tireData.longB, tireData.longC, tireData.longD, tireData.longE) * normalLoad * tireData.frictionMultiplier;
        float Fy = CalculatePacejka(slipAngle, tireData.latB, tireData.latC, tireData.latD, tireData.latE) * normalLoad * tireData.frictionMultiplier;

        return new Vector2(Fx, Fy);
    }

    private float CalculatePacejka(float slip, float B, float C, float D, float E)
    {
        return D * Mathf.Sin(C * Mathf.Atan(B * slip - E * (B * slip - Mathf.Atan(B * slip))));
    }

    public float GetRollingResistanceForce(float normalLoad)
    {
        return normalLoad * tireData.rollingResistance;
    }
}