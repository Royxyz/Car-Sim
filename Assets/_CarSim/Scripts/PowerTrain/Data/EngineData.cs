using UnityEngine;
using System.IO;
using System.Globalization;

[CreateAssetMenu(fileName = "NewEngineData", menuName = "Vehicle Physics/Engine Data")]
public class EngineData : ScriptableObject
{
    [Header("Power Output (CSV 3D LUT)")]
    [Tooltip("Drag your CSV file here. Format: Row 1 = Throttle. Col 1 = RPM.")]
    public TextAsset torqueCsvFile;

    [Header("Physical Characteristics")]
    public float engineInertia = 0.2f;
    
    [Tooltip("Constant parasitic drag (e.g., alternator, water pump, basic rotational resistance)")]
    public float staticFriction = 10f; 
    
    [Tooltip("Mechanical friction that increases with RPM (e.g., piston rings, bearings)")]
    public float dynamicFriction = 0.05f;

    [Header("Intake & Forced Induction")]
    [Tooltip("Time in seconds for the Drive-by-Wire system to go from 0% to 100% throttle.")]
    public float throttleSmoothing = 0.08f; 
    
    public bool isTurbocharged = true;
    
    [Tooltip("Turbo lag in seconds at low RPMs (slow exhaust flow).")]
    public float turboSpoolTimeAtIdle = 1.5f; 
    
    [Tooltip("Turbo lag in seconds at Redline (fast exhaust flow).")]
    public float turboSpoolTimeAtRedline = 0.15f; 
    
    [Tooltip("How fast the blow-off valve dumps pressure when lifting off the gas.")]
    public float blowOffTime = 0.1f;
    

    public float idleRPM = 800f;
    public float redlineRPM = 7500f;

    private float[] throttleAxis;
    private float[] rpmAxis;
    private float[,] torqueTable;
    private bool isInitialized = false;

    public void Initialize()
    {
        if (torqueCsvFile == null)
        {
            Debug.LogError("Torque CSV File is missing in EngineData!");
            return;
        }

        string[] lines = torqueCsvFile.text.Trim().Split('\n');
        
        string[] headerRow = lines[0].Split(',');
        int throttleCols = headerRow.Length - 1;
        throttleAxis = new float[throttleCols];
        for (int i = 0; i < throttleCols; i++)
        {
            throttleAxis[i] = ParseFloat(headerRow[i + 1]);
        }

        int rpmRows = lines.Length - 1;
        rpmAxis = new float[rpmRows];
        torqueTable = new float[rpmRows, throttleCols];

        for (int r = 0; r < rpmRows; r++)
        {
            string[] rowData = lines[r + 1].Split(',');
            rpmAxis[r] = ParseFloat(rowData[0]);

            for (int t = 0; t < throttleCols; t++)
            {
                torqueTable[r, t] = ParseFloat(rowData[t + 1]);
            }
        }

        isInitialized = true;
    }

    public float GetGeneratedTorque(float currentRPM, float throttlePosition)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("EngineData was not initialized! Initializing now, but this causes a spike.");
            Initialize();
        }
        
        currentRPM = Mathf.Clamp(currentRPM, rpmAxis[0], rpmAxis[rpmAxis.Length - 1]);
        throttlePosition = Mathf.Clamp(throttlePosition, throttleAxis[0], throttleAxis[throttleAxis.Length - 1]);
        
        FindIndicesAndLerpFactor(rpmAxis, currentRPM, out int r0, out int r1, out float rLerp);
        FindIndicesAndLerpFactor(throttleAxis, throttlePosition, out int t0, out int t1, out float tLerp);
        
        float q11 = torqueTable[r0, t0];
        float q12 = torqueTable[r1, t0]; 
        float q21 = torqueTable[r0, t1]; 
        float q22 = torqueTable[r1, t1];

        float interpolateR0 = Mathf.Lerp(q11, q21, tLerp);
        float interpolateR1 = Mathf.Lerp(q12, q22, tLerp); 

        return Mathf.Lerp(interpolateR0, interpolateR1, rLerp);
    }

    public float GetLossTorque(float currentRPM)
    {
        return staticFriction + (dynamicFriction * currentRPM * (Mathf.PI / 30f));
    }

    private void FindIndicesAndLerpFactor(float[] axisData, float targetValue, out int index0, out int index1, out float lerpFactor)
    {
        for (int i = 0; i < axisData.Length - 1; i++)
        {
            if (targetValue >= axisData[i] && targetValue <= axisData[i + 1])
            {
                index0 = i;
                index1 = i + 1;
                lerpFactor = (targetValue - axisData[i]) / (axisData[i + 1] - axisData[i]);
                return;
            }
        }
        index0 = axisData.Length - 1;
        index1 = axisData.Length - 1;
        lerpFactor = 0f;
    }

    private float ParseFloat(string value)
    {
        float.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out float result);
        return result;
    }
}