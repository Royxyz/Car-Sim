using UnityEngine;

[System.Serializable]
public class TireData : ScriptableObject
{
    [Header("Physical Properties")]
    public float maxLoadCapacity = 15000f;
    public float frictionMultiplier = 1.0f;
    public float rollingResistance = 0.015f;

    [Header("Relaxation Lengths (Meters)")]
    [Tooltip("Distance the tire must travel to build 63% of longitudinal slip. (~0.15m)")]
    public float longRelaxationLength = 0.15f; 
    
    [Tooltip("Distance the tire must travel to build 63% of lateral slip angle. (~0.25m)")]
    public float latRelaxationLength = 0.25f;

    [Header("Longitudinal Pacejka Coefficients")]
    public float longB = 10f;
    public float longC = 1.9f;
    public float longD = 1.0f;
    public float longE = 0.97f;

    [Header("Lateral Pacejka Coefficients")]
    public float latB = 12f;
    public float latC = 1.3f;
    public float latD = 1.0f;
    public float latE = -0.5f;
}