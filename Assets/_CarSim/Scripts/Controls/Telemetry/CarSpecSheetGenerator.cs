using UnityEngine;
using System.Reflection;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

[RequireComponent(typeof(SimulationController))]
public class CarSpecSheetGenerator : MonoBehaviour
{
    [Header("Settings")]
    public string fileName = "CarSpecSheet.csv";

    [ContextMenu("Generate Spec Sheet CSV")]
    public void GenerateCSV()
    {
        SimulationController car = GetComponent<SimulationController>();
        StringBuilder csv = new StringBuilder();

        // Setup CSV Headers
        csv.AppendLine("Category,Component,Parameter,Value");

        // 1. Chassis & Aero
        ExtractScriptableObject(car.chassisData, "Chassis", "Base Dynamics", csv);
        ExtractScriptableObject(car.steeringData, "Chassis", "Steering", csv);
        ExtractScriptableObject(car.antiRollBar.antiRollBarData, "Chassis", "Anti-Roll Bars", csv);
        ExtractScriptableObject(car.aerodynamics.aeroData, "Aerodynamics", "Aero Settings", csv);

        // 2. Powertrain
        ExtractScriptableObject(car.powerTrain.engine._engineData, "Powertrain", "Engine", csv);
        
        // Custom Induction Extraction: Only write parameters relevant to the selected induction type
        if (car.powerTrain.engine._engineData != null && car.powerTrain.engine._engineData.induction != null)
        {
            ExtractInductionData(car.powerTrain.engine._engineData.induction, "Powertrain", "Forced Induction", csv);
        }

        ExtractScriptableObject(car.powerTrain.clutch.clutchData, "Powertrain", "Clutch", csv);
        ExtractScriptableObject(car.powerTrain.transmission.transmissionData, "Powertrain", "Transmission", csv);
        ExtractScriptableObject(car.autoController.logicData, "Powertrain", "Auto Controller", csv);

        // 3. Drivetrain Layout Validation: Only fetch diffs that actually receive torque
        if (car.drivetrain.drivetrainData != null)
        {
            ExtractScriptableObject(car.drivetrain.drivetrainData, "Drivetrain", "Layout", csv);
            DriveType driveType = car.drivetrain.drivetrainData.driveType;

            if (driveType == DriveType.FWD || driveType == DriveType.AWD)
                ExtractScriptableObject(car.drivetrain.frontDiff.diffData, "Drivetrain", "Front Differential", csv);

            if (driveType == DriveType.AWD)
                ExtractScriptableObject(car.drivetrain.centerDiff.diffData, "Drivetrain", "Center Differential", csv);

            if (driveType == DriveType.RWD || driveType == DriveType.AWD)
                ExtractScriptableObject(car.drivetrain.rearDiff.diffData, "Drivetrain", "Rear Differential", csv);
        }

        // 4. Corners
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

        // Save to Disk
        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(path, csv.ToString());
        Debug.Log($"<color=cyan><b>[Spec Sheet]</b> Generated successfully at: {path}</color>");
    }

    private void ExtractInductionData(InductionData induction, string category, string componentName, StringBuilder csv)
    {
        AddRow(csv, category, componentName, "Induction Type", induction.type.ToString());

        if (induction.type != InductionType.NaturallyAspirated)
        {
            AddRow(csv, category, componentName, "Max Pressure Bar", induction.maxPressureBar.ToString("F2"));
        }

        if (induction.type == InductionType.Supercharged)
        {
            AddRow(csv, category, componentName, "Supercharger Parasitic Drag", induction.superchargerParasiticDrag.ToString("F1"));
        }
        else if (induction.type == InductionType.Turbocharged)
        {
            AddRow(csv, category, componentName, "Boost Threshold RPM", induction.boostThresholdRPM.ToString("F0"));
            AddRow(csv, category, componentName, "Optimal Boost RPM", induction.optimalBoostRPM.ToString("F0"));
            AddRow(csv, category, componentName, "Turbo Spool Rate", induction.turboSpoolRate.ToString("F2"));
            AddRow(csv, category, componentName, "Blow Off Rate", induction.blowOffRate.ToString("F2"));
        }
    }

    private void ExtractScriptableObject(ScriptableObject so, string category, string componentName, StringBuilder csv)
    {
        if (so == null) return;

        FieldInfo[] fields = so.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            object val = field.GetValue(so);
            string valStr = val != null ? val.ToString() : "null";

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

            valStr = valStr.Replace(",", ".");
            
            // Regex to format camelCase into readable Title Case
            string humanizedName = Regex.Replace(field.Name, "(\\B[A-Z])", " $1");
            humanizedName = char.ToUpper(humanizedName[0]) + humanizedName.Substring(1);

            AddRow(csv, category, componentName, humanizedName, valStr);
        }
    }

    // Helper method to keep line building clean
    private void AddRow(StringBuilder csv, string category, string component, string parameter, string value)
    {
        csv.AppendLine($"{category},{component},{parameter},{value}");
    }
}