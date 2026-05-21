using UnityEngine;
using System.Reflection;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(SimulationController))]
public class CarSpecSheetGenerator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Exporting as Markdown (.md) makes it highly legible in code editors, GitHub, and Discord.")]
    public string fileName = "CarSpecSheet.md";

    // Data Structure: Category -> Component -> List of (Parameter, Value)
    private Dictionary<string, Dictionary<string, List<KeyValuePair<string, string>>>> specDatabase;

    [ContextMenu("Generate Spec Sheet")]
    public void GenerateDocument()
    {
        SimulationController car = GetComponent<SimulationController>();
        specDatabase = new Dictionary<string, Dictionary<string, List<KeyValuePair<string, string>>>>();

        // 1. Chassis & Aero (Added null checks using '?.' to prevent errors on incomplete cars)
        ExtractScriptableObject(car.chassisData, "Chassis & Dynamics", "Base Dynamics");
        ExtractScriptableObject(car.steeringData, "Chassis & Dynamics", "Steering");
        ExtractScriptableObject(car.antiRollBarData, "Chassis & Dynamics", "Anti-Roll Bars");
        ExtractScriptableObject(car.aerodynamics?.aeroData, "Aerodynamics", "Aero Settings");

        // 2. Powertrain
        ExtractScriptableObject(car.powerTrain?.engine?._engineData, "Powertrain", "Engine");
        
        if (car.powerTrain?.engine?._engineData?.induction != null)
        {
            ExtractInductionData(car.powerTrain.engine._engineData.induction, "Powertrain", "Forced Induction");
        }

        ExtractScriptableObject(car.powerTrain?.clutch?.clutchData, "Powertrain", "Clutch");
        ExtractScriptableObject(car.powerTrain?.transmission?.transmissionData, "Powertrain", "Transmission");
        ExtractScriptableObject(car.autoController?.logicData, "Powertrain", "Auto Controller");

        // 3. Drivetrain Layout Validation
        if (car.drivetrain?.drivetrainData != null)
        {
            ExtractScriptableObject(car.drivetrain.drivetrainData, "Drivetrain", "Layout");
            DriveType driveType = car.drivetrain.drivetrainData.driveType;

            if (driveType == DriveType.FWD || driveType == DriveType.AWD)
                ExtractScriptableObject(car.drivetrain.frontDiff?.diffData, "Drivetrain", "Front Differential");

            if (driveType == DriveType.AWD)
                ExtractScriptableObject(car.drivetrain.centerDiff?.diffData, "Drivetrain", "Center Differential");

            if (driveType == DriveType.RWD || driveType == DriveType.AWD)
                ExtractScriptableObject(car.drivetrain.rearDiff?.diffData, "Drivetrain", "Rear Differential");
        }

        // 4. Corners
        if (car.corners != null && car.corners.Length >= 4)
        {
            if (car.corners[0] != null)
            {
                ExtractScriptableObject(car.corners[0].wheel?.wheelData, "Front Axle", "Wheel Config");
                ExtractScriptableObject(car.corners[0].tire?.tireData, "Front Axle", "Tire Compound");
                ExtractScriptableObject(car.corners[0].brake?.brakeData, "Front Axle", "Brakes");
                ExtractScriptableObject(car.corners[0].suspension?.suspData, "Front Axle", "Suspension");
            }

            if (car.corners[2] != null)
            {
                ExtractScriptableObject(car.corners[2].wheel?.wheelData, "Rear Axle", "Wheel Config");
                ExtractScriptableObject(car.corners[2].tire?.tireData, "Rear Axle", "Tire Compound");
                ExtractScriptableObject(car.corners[2].brake?.brakeData, "Rear Axle", "Brakes");
                ExtractScriptableObject(car.corners[2].suspension?.suspData, "Rear Axle", "Suspension");
            }
        }

        SaveAsMarkdown();
    }

    private void ExtractInductionData(InductionData induction, string category, string componentName)
    {
        AddRecord(category, componentName, "Induction Type", induction.type.ToString());

        if (induction.type != InductionType.NaturallyAspirated)
            AddRecord(category, componentName, "Max Pressure", induction.maxPressureBar.ToString("F2") + " Bar");

        if (induction.type == InductionType.Supercharged)
        {
            AddRecord(category, componentName, "Supercharger Parasitic Drag", induction.superchargerParasiticDrag.ToString("F1") + " Nm");
        }
        else if (induction.type == InductionType.Turbocharged)
        {
            AddRecord(category, componentName, "Boost Threshold RPM", induction.boostThresholdRPM.ToString("F0"));
            AddRecord(category, componentName, "Optimal Boost RPM", induction.optimalBoostRPM.ToString("F0"));
            AddRecord(category, componentName, "Turbo Spool Rate", induction.turboSpoolRate.ToString("F2"));
            AddRecord(category, componentName, "Blow Off Rate", induction.blowOffRate.ToString("F2"));
        }
    }

    private void ExtractScriptableObject(ScriptableObject so, string category, string componentName)
    {
        if (so == null) return;

        FieldInfo[] fields = so.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            object val = field.GetValue(so);
            string valStr = FormatValue(val);

            // Regex to format camelCase into readable Title Case
            string humanizedName = Regex.Replace(field.Name, "(\\B[A-Z])", " $1");
            humanizedName = char.ToUpper(humanizedName[0]) + humanizedName.Substring(1);

            AddRecord(category, componentName, humanizedName, valStr);
        }
    }

    // Improved data formatting for human legibility
    private string FormatValue(object val)
    {
        if (val == null) return "N/A";
        
        if (val is float f) return f.ToString("0.###");
        if (val is bool b) return b ? "Yes" : "No";
        if (val is float[] floatArray) return "[ " + string.Join(", ", floatArray.Select(x => x.ToString("0.###"))) + " ]";
        if (val is Vector3 v3) return $"X: {v3.x:F2} | Y: {v3.y:F2} | Z: {v3.z:F2}";
        if (val is Vector2 v2) return $"X: {v2.x:F2} | Y: {v2.y:F2}";
        if (val is AnimationCurve curve) return $"Curve ({curve.length} keys)";
        if (val is TextAsset textAsset) return $"File: {textAsset.name}";

        return val.ToString();
    }

    // Buffers data into the nested dictionary
    private void AddRecord(string category, string component, string parameter, string value)
    {
        if (!specDatabase.ContainsKey(category))
            specDatabase[category] = new Dictionary<string, List<KeyValuePair<string, string>>>();

        if (!specDatabase[category].ContainsKey(component))
            specDatabase[category][component] = new List<KeyValuePair<string, string>>();

        specDatabase[category][component].Add(new KeyValuePair<string, string>(parameter, value));
    }

    // Compiles the dictionary into a formatted Markdown document
    private void SaveAsMarkdown()
    {
        StringBuilder md = new StringBuilder();
        
        md.AppendLine("# Vehicle Specification Sheet");
        md.AppendLine($"*Generated on: {System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}*\n");

        foreach (var category in specDatabase)
        {
            md.AppendLine($"## {category.Key}");
            md.AppendLine("---");

            foreach (var component in category.Value)
            {
                md.AppendLine($"### {component.Key}");
                md.AppendLine("| Parameter | Value |");
                md.AppendLine("|---|---|");

                foreach (var param in component.Value)
                {
                    md.AppendLine($"| **{param.Key}** | {param.Value} |");
                }
                md.AppendLine(); // Spacer
            }
        }

        string path = Path.Combine(Application.dataPath, fileName);
        File.WriteAllText(path, md.ToString());
        Debug.Log($"<color=cyan><b>[Spec Sheet]</b> Markdown document generated successfully at: {path}</color>");
    }
}