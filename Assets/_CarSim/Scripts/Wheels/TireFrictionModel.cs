using UnityEngine;

[System.Serializable]
public class TireFrictionModel
{
    [SerializeField] public TireData tireData;

    public float dynamicLongSlip { get; private set; }
    public float dynamicSlipAngle { get; private set; }

    public void Initialize()
    {
        dynamicLongSlip = 0f;
        dynamicSlipAngle = 0f;
    }

    public Vector2 CalculateGripForces(float normalLoad, float rawLongSlip, float rawSlipAngle, float forwardSpeed, float wheelLinearSpeed, float dt)
    {
        if (normalLoad <= 0f)
        {
            dynamicLongSlip = 0f;
            dynamicSlipAngle = 0f;
            return Vector2.zero;
        }

        float transportVelocity = Mathf.Max(Mathf.Abs(forwardSpeed), Mathf.Abs(wheelLinearSpeed), 1.0f);
        float distanceTraveled = transportVelocity * dt;

        float longBlend = 1f - Mathf.Exp(-distanceTraveled / tireData.longRelaxationLength);
        float latBlend = 1f - Mathf.Exp(-distanceTraveled / tireData.latRelaxationLength);

        dynamicLongSlip = Mathf.Lerp(dynamicLongSlip, rawLongSlip, longBlend);
        dynamicSlipAngle = Mathf.Lerp(dynamicSlipAngle, rawSlipAngle, latBlend);

        float effectiveLoad = Mathf.Min(normalLoad, tireData.maxLoadCapacity);

        float normalizedLoad = effectiveLoad / tireData.maxLoadCapacity;
        float loadFalloffMultiplier = 1.0f - (normalizedLoad * tireData.loadSensitivity);
        float finalFrictionMult = tireData.frictionMultiplier * Mathf.Max(0.3f, loadFalloffMultiplier);

        float slipMagnitude = Mathf.Sqrt((dynamicLongSlip * dynamicLongSlip) + (dynamicSlipAngle * dynamicSlipAngle));
        slipMagnitude = Mathf.Max(slipMagnitude, 0.0001f);

        float rawFxMag = CalculatePacejka(slipMagnitude, tireData.longB, tireData.longC, tireData.longD, tireData.longE) * effectiveLoad * finalFrictionMult;
        float rawFyMag = CalculatePacejka(slipMagnitude, tireData.latB, tireData.latC, tireData.latD, tireData.latE) * effectiveLoad * finalFrictionMult;

        float Fx = rawFxMag * (dynamicLongSlip / slipMagnitude);
        float Fy = rawFyMag * (dynamicSlipAngle / slipMagnitude);

        

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