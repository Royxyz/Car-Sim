using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns red marker cubes at the AI driver’s waypoints and connects them with a LineRenderer.
/// Attach to an empty GameObject and assign the AI driver (or let it auto-find on same object).
/// </summary>
public class WaypointVisualizer : MonoBehaviour
{
    [Header("Reference")]
    public AIDriverStressTest aiDriver;   // Drag here or auto‑detected

    [Header("Visual Settings")]
    public float markerSize = 1.0f;       // Cube edge length
    public float yOffset = 1.0f;          // Height above ground (world Y axis)
    public Color waypointColor = Color.red;
    public float lineWidth = 0.2f;

    private GameObject markersParent;
    private bool visualsCreated = false;

    void Start()
    {
        if (aiDriver == null)
            aiDriver = GetComponent<AIDriverStressTest>();
    }

    void Update()
    {
        // Wait until the AI script has populated its waypoint list
        if (!visualsCreated && aiDriver != null && aiDriver.waypoints != null && aiDriver.waypoints.Count > 0)
        {
            CreateVisuals();
            visualsCreated = true;
        }
    }

    void CreateVisuals()
    {
        List<Vector3> waypoints = aiDriver.waypoints;
        if (waypoints.Count == 0) return;

        // ---- Parent container ----
        markersParent = new GameObject("Waypoint_Markers");
        markersParent.transform.SetParent(transform);

        // ---- Cubes at each waypoint ----
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = waypointColor;

        foreach (Vector3 wp in waypoints)
        {
            Vector3 pos = wp + Vector3.up * yOffset;
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "WaypointMarker";
            cube.transform.position = pos;
            cube.transform.localScale = Vector3.one * markerSize;
            cube.transform.SetParent(markersParent.transform);

            // Remove collider – visuals only
            Destroy(cube.GetComponent<Collider>());

            // Apply red unlit material
            cube.GetComponent<MeshRenderer>().material = mat;
        }

        // ---- Line connecting waypoints ----
        GameObject lineObj = new GameObject("Waypoint_Line");
        lineObj.transform.SetParent(markersParent.transform);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = waypoints.Count;
        lr.useWorldSpace = true;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.material = mat;   // same red material

        for (int i = 0; i < waypoints.Count; i++)
            lr.SetPosition(i, waypoints[i] + Vector3.up * yOffset);
    }

    /// <summary>
    /// Public method to rebuild the visuals (for example if waypoints change at runtime).
    /// </summary>
    public void RefreshVisuals()
    {
        if (markersParent != null)
            Destroy(markersParent);
        visualsCreated = false;
    }
}