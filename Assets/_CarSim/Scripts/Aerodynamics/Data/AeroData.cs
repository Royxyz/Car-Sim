using UnityEngine;

[CreateAssetMenu(fileName = "NewAeroData", menuName = "Vehicle Physics/Aerodynamics Data")]
public class AeroData : ScriptableObject
{
    public float airDensity = 1.225f; 

    public float frontalArea = 2.2f; 
    public float sideArea = 4.5f;    
    public float topArea = 5.0f;     

    public float dragCoefficientFront = 0.34f;
    public float dragCoefficientSide = 0.85f;  
    
    public float downforceCoefficient = 0.15f; 
}