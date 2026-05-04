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

        // --- FIX 1: Tire Load Sensitivity ---
        float normalizedLoad = effectiveLoad / tireData.maxLoadCapacity;
        float loadFalloffMultiplier = 1.0f - (normalizedLoad * tireData.loadSensitivity);
        // Ensure friction multiplier never drops below a 30% hard floor to prevent total loss of control
        float finalFrictionMult = tireData.frictionMultiplier * Mathf.Max(0.3f, loadFalloffMultiplier);

        float slipMagnitude = Mathf.Sqrt((dynamicLongSlip * dynamicLongSlip) + (dynamicSlipAngle * dynamicSlipAngle));
        slipMagnitude = Mathf.Max(slipMagnitude, 0.0001f);

        // Apply the load-sensitive multiplier to the Pacejka formulas
        float rawFxMag = CalculatePacejka(slipMagnitude, tireData.longB, tireData.longC, tireData.longD, tireData.longE) * effectiveLoad * finalFrictionMult;
        float rawFyMag = CalculatePacejka(slipMagnitude, tireData.latB, tireData.latC, tireData.latD, tireData.latE) * effectiveLoad * finalFrictionMult;

        float Fx = rawFxMag * (dynamicLongSlip / slipMagnitude);
        float Fy = rawFyMag * (dynamicSlipAngle / slipMagnitude);

        // --- FIX 2: Friction Ellipse Constraint ---
        // Prevents generating more total lateral/longitudinal force than physically possible at the current slip
        float maxAllowedForce = CalculatePacejka(slipMagnitude, tireData.longB, tireData.longC, tireData.longD, tireData.longE) * effectiveLoad * finalFrictionMult;
        Vector2 combinedForce = new Vector2(Fx, Fy);

        if (combinedForce.magnitude > maxAllowedForce)
        {
            combinedForce = combinedForce.normalized * maxAllowedForce;
        }

        return combinedForce;
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