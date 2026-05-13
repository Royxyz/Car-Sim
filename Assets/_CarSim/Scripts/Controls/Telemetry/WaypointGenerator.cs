using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public class TrackWaypointGenerator : MonoBehaviour
{
    [Header("Track Settings")]
    public LayerMask trackLayer;
    public float stepDistance = 5.0f;
    public float maxTrackWidth = 20.0f;
    public float edgeSearchStep = 0.5f;
    public float loopClosureDistance = 10.0f;
    public int maxWaypoints = 3000;
    
    [Header("Export Settings")]
    public string csvFileName = "TrackData.csv";

    // Struct updated to hold track limits
    private struct WaypointData
    {
        public Vector3 pos;
        public Vector3 dir;
        public Vector3 leftEdge;
        public Vector3 rightEdge;
    }

    [ContextMenu("Generate and Export to CSV")]
    public void GenerateAndExport()
    {
        Vector3 currentPos = transform.position;
        Vector3 currentForward = transform.forward;
        Vector3 startPos = currentPos;

        List<WaypointData> waypoints = new List<WaypointData>(maxWaypoints);

        for (int i = 0; i < maxWaypoints; i++)
        {
            Vector3 probeHigh = currentPos + (currentForward * stepDistance) + (Vector3.up * 20f);
            
            if (Physics.Raycast(probeHigh, Vector3.down, out RaycastHit centerHit, 50f, trackLayer))
            {
                Vector3 surfacePoint = centerHit.point;
                Vector3 rightDir = Vector3.Cross(Vector3.up, currentForward).normalized;
                Vector3 leftDir = -rightDir;

                Vector3 rightEdge = FindTrackEdge(surfacePoint, rightDir);
                Vector3 leftEdge = FindTrackEdge(surfacePoint, leftDir);

                Vector3 trackCenter = (rightEdge + leftEdge) / 2f;

                WaypointData wp = new WaypointData { 
                    pos = trackCenter, 
                    dir = Vector3.forward,
                    leftEdge = leftEdge,
                    rightEdge = rightEdge
                };
                
                if (waypoints.Count > 0)
                {
                    Vector3 lookDir = (trackCenter - waypoints[waypoints.Count - 1].pos).normalized;
                    if (lookDir != Vector3.zero)
                    {
                        WaypointData prev = waypoints[waypoints.Count - 1];
                        prev.dir = lookDir;
                        waypoints[waypoints.Count - 1] = prev;
                    }
                }

                waypoints.Add(wp);

                currentForward = (trackCenter - currentPos).normalized;
                currentPos = trackCenter;

                if (i > 10 && (currentPos - startPos).sqrMagnitude < (loopClosureDistance * loopClosureDistance))
                {
                    WaypointData last = waypoints[waypoints.Count - 1];
                    last.dir = (waypoints[0].pos - last.pos).normalized;
                    waypoints[waypoints.Count - 1] = last;
                    Debug.Log($"Track loop closed! Total Waypoints: {waypoints.Count}");
                    break;
                }
            }
            else
            {
                Debug.LogWarning("Lost the track! Raycast missed.");
                break;
            }
        }

        ExportToCSV(waypoints);
    }

    private Vector3 FindTrackEdge(Vector3 centerPoint, Vector3 direction)
    {
        Vector3 lastValidPoint = centerPoint;
        for (float d = edgeSearchStep; d <= maxTrackWidth; d += edgeSearchStep)
        {
            Vector3 testOrigin = centerPoint + (direction * d) + (Vector3.up * 5f);
            if (Physics.Raycast(testOrigin, Vector3.down, out RaycastHit hit, 10f, trackLayer))
                lastValidPoint = hit.point;
            else
                break;
        }
        return lastValidPoint;
    }

    private void ExportToCSV(List<WaypointData> waypoints)
    {
        string path = Path.Combine(Application.dataPath, csvFileName);
        
        // Expanded string builder to handle the new columns
        StringBuilder sb = new StringBuilder(waypoints.Count * 120); 
        sb.AppendLine("PosX,PosY,PosZ,DirX,DirY,DirZ,LeftX,LeftY,LeftZ,RightX,RightY,RightZ");

        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            sb.AppendLine($"{wp.pos.x:F3},{wp.pos.y:F3},{wp.pos.z:F3},{wp.dir.x:F3},{wp.dir.y:F3},{wp.dir.z:F3},{wp.leftEdge.x:F3},{wp.leftEdge.y:F3},{wp.leftEdge.z:F3},{wp.rightEdge.x:F3},{wp.rightEdge.y:F3},{wp.rightEdge.z:F3}");
        }

        File.WriteAllText(path, sb.ToString());
        Debug.Log($"<color=green>Successfully exported track data with limits to: {path}</color>");
    }
}