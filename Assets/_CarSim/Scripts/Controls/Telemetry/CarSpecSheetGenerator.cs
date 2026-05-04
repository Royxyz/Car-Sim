using UnityEngine;
using System.Reflection;
using System.Text;
using System.IO;

[RequireComponent(typeof(SimulationController))]
public class CarSpecSheetGenerator : MonoBehaviour
{
    [Header("Settings")]
    public string fileName = "CarSpecSheet.csv";

    // Right-click the component in the Inspector to run this
    [ContextMenu("Generate Spec Sheet CSV")]
    public void GenerateCSV()
    {
        SimulationController car = GetComponent<SimulationController>();
        StringBuilder csv = new StringBuilder();

        // 1. Setup CSV Headers
        csv.AppendLine("Category,Component,Parameter,Value");

        // 2. Extract Data using Reflection
        ExtractScriptableObject(car.chassisData, "Chassis", "Base Dynamics", csv);
        ExtractScriptableObject(car.steeringData, "Chassis", "Steering", csv);
        ExtractScriptableObject(car.antiRollBar.antiRollBarData, "Chassis", "Anti-Roll Bars", csv);

        ExtractScriptableObject(car.aerodynamics.aeroData, "Aerodynamics", "Aero Settings", csv);

        ExtractScriptableObject(car.powerTrain.engine._engineData, "Powertrain", "Engine", csv);
        if (car.powerTrain.engine._engineData != null)
        {
            ExtractScriptableObject(car.powerTrain.engine._engineData.induction, "Powertrain", "Forced Induction", csv);
        }

        ExtractScriptableObject(car.powerTrain.clutch.clutchData, "Powertrain", "Clutch", csv);
        ExtractScriptableObject(car.powerTrain.transmission.transmissionData, "Powertrain", "Transmission", csv);
        ExtractScriptableObject(car.autoController.logicData, "Powertrain", "Auto Controller", csv);

        ExtractScriptableObject(car.drivetrain.drivetrainData, "Drivetrain", "Layout", csv);
        ExtractScriptableObject(car.drivetrain.frontDiff.diffData, "Drivetrain", "Front Differential", csv);
        ExtractScriptableObject(car.drivetrain.centerDiff.diffData, "Drivetrain", "Center Differential", csv);
        ExtractScriptableObject(car.drivetrain.rearDiff.diffData, "Drivetrain", "Rear Differential", csv);

        if (car.corners != null && car.corners.Length >= 4)
        {
            if (car.corners[0] != null)
            {
                ExtractScriptableObject(car.corners[0].wheel.wheelData, "Front Axle", "Wheel Config", csv);
                ExtractScriptableObject(car.corners[0].tire.tireData, "Front Axle", "Tire Compound", csv);
                ExtractScriptableObject(car.corners[0].brake.brakeData, "Front Axle", "Brakes", csv);
                ExtractScriptableObject(car.corners[0].suspension.suspData, "Front Axle", "Suspension", csv);
            }

            if (car.corners[2] != null)
            {
                ExtractScriptableObject(car.corners[2].wheel.wheelData, "Rear Axle", "Wheel Config", csv);
                ExtractScriptableObject(car.corners[2].tire.tireData, "Rear Axle", "Tire Compound", csv);
                ExtractScriptableObject(car.corners[2].brake.brakeData, "Rear Axle", "Brakes", csv);
                ExtractScriptableObject(car.corners[2].suspension.suspData, "Rear Axle", "Suspension", csv);
            }
        }

        // 3. Save to Disk
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(path, csv.ToString());
        Debug.Log($"<color=cyan><b>[Spec Sheet]</b> Generated successfully at: {path}</color>");
    }

    private void ExtractScriptableObject(ScriptableObject so, string category, string componentName, StringBuilder csv)
    {
        if (so == null) return;

        // Grab all public fields from the ScriptableObject using Reflection
        FieldInfo[] fields = so.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            object val = field.GetValue(so);
            string valStr = val != null ? val.ToString() : "null";

            // Special formatting to prevent Arrays (like your gear ratios) from breaking the CSV
            if (val is float[] floatArray)
            {
                valStr = string.Join(" : ", floatArray);
            }
            else if (val is Vector3 v3)
            {
                valStr = $"{v3.x:F2} | {v3.y:F2} | {v3.z:F2}";
            }
            else if (val is Vector2 v2)
            {
                valStr = $"{v2.x:F2} | {v2.y:F2}";
            }
            else if (val is AnimationCurve curve)
            {
                valStr = $"Curve Keys: {curve.length}";
            }
            else if (val is TextAsset textAsset)
            {
                valStr = $"CSV File: {textAsset.name}";
            }

            // Strip out any commas that might exist in the string values to prevent column shifting
            valStr = valStr.Replace(",", ".");

            csv.AppendLine($"{category},{componentName},{field.Name},{valStr}");
        }
    }
}