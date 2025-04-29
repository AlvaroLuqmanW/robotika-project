using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class AreaGridGenerator : MonoBehaviour
{
    [Header("Grid Parameters")]
    public float gridSpacing = 1.0f; // Distance between grid points
    public float gridHeight = 0.1f; // Height above ground for grid points
    public bool showGrid = true; // Toggle grid visualization
    public Color gridColor = new Color(0, 1, 0, 0.3f); // Color for grid visualization
    public float obstacleCheckRadius = 0.25f; // Radius to check for obstacles

    private List<Vector3> gridPoints = new List<Vector3>();
    private Transform[] cornerPoints;
    private Bounds areaBounds;

    void Start()
    {
        // Get the corner points (children of this waypoint)
        cornerPoints = new Transform[4];
        int cornerIndex = 0;
        foreach (Transform child in transform)
        {
            if (cornerIndex < 4)
            {
                cornerPoints[cornerIndex] = child;
                cornerIndex++;
            }
        }

        if (cornerIndex != 4)
        {
            Debug.LogError("Waypoint must have exactly 4 corner points as children!");
            return;
        }

        // Order the corner points in clockwise order
        OrderCornerPoints();
        
        // Calculate the area bounds
        CalculateAreaBounds();
        
        // Generate the grid
        GenerateGrid();
    }

    private void OrderCornerPoints()
    {
        // Find the center point of all corners
        Vector3 center = Vector3.zero;
        foreach (Transform corner in cornerPoints)
        {
            center += corner.position;
        }
        center /= 4;

        // Sort corners based on their angle relative to the center
        System.Array.Sort(cornerPoints, (a, b) => {
            Vector3 dirA = a.position - center;
            Vector3 dirB = b.position - center;
            float angleA = Mathf.Atan2(dirA.z, dirA.x);
            float angleB = Mathf.Atan2(dirB.z, dirB.x);
            return angleA.CompareTo(angleB);
        });
    }

    private void CalculateAreaBounds()
    {
        // Find the min and max points of the area
        Vector3 min = cornerPoints[0].position;
        Vector3 max = cornerPoints[0].position;

        for (int i = 1; i < 4; i++)
        {
            min = Vector3.Min(min, cornerPoints[i].position);
            max = Vector3.Max(max, cornerPoints[i].position);
        }

        // Create bounds
        areaBounds = new Bounds();
        areaBounds.SetMinMax(min, max);
    }

    private bool IsPointInQuadrilateral(Vector3 point)
    {
        // Convert to 2D for point-in-polygon test
        Vector2 p = new Vector2(point.x, point.z);
        Vector2[] corners = new Vector2[4];
        for (int i = 0; i < 4; i++)
        {
            corners[i] = new Vector2(cornerPoints[i].position.x, cornerPoints[i].position.z);
        }

        // Ray casting algorithm for point-in-polygon test
        bool inside = false;
        for (int i = 0, j = corners.Length - 1; i < corners.Length; j = i++)
        {
            if (((corners[i].y > p.y) != (corners[j].y > p.y)) &&
                (p.x < (corners[j].x - corners[i].x) * (p.y - corners[i].y) / (corners[j].y - corners[i].y) + corners[i].x))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private bool IsPointCollidingWithObstacle(Vector3 point)
    {
        // Check for objects tagged as "Obstacles" using OverlapSphere
        Collider[] colliders = Physics.OverlapSphere(point, obstacleCheckRadius);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Obstacles"))
            {
                return true; // Point collides with an obstacle
            }
        }
        return false; // No collision with obstacles
    }

    private void GenerateGrid()
    {
        gridPoints.Clear();

        // Calculate grid dimensions
        float width = areaBounds.size.x;
        float length = areaBounds.size.z;
        int pointsX = Mathf.CeilToInt(width / gridSpacing);
        int pointsZ = Mathf.CeilToInt(length / gridSpacing);

        // Generate grid points
        for (int x = 0; x <= pointsX; x++)
        {
            for (int z = 0; z <= pointsZ; z++)
            {
                Vector3 point = areaBounds.min + new Vector3(
                    x * gridSpacing,
                    0,
                    z * gridSpacing
                );

                // Check if point is within the quadrilateral
                if (IsPointInQuadrilateral(point))
                {
                    // Check if point is within NavMesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(point, out hit, gridSpacing, NavMesh.AllAreas))
                    {
                        // Adjust point to be on NavMesh
                        point = hit.position;
                        point.y += gridHeight; // Raise point above ground

                        // Check if point collides with obstacle
                        if (!IsPointCollidingWithObstacle(point))
                        {
                            gridPoints.Add(point);
                        }
                    }
                }
            }
        }
    }

    public List<Vector3> GetGridPoints()
    {
        return gridPoints;
    }

    public Vector3 GetNearestGridPoint(Vector3 position)
    {
        Vector3 nearestPoint = Vector3.zero;
        float minDistance = float.MaxValue;

        foreach (Vector3 point in gridPoints)
        {
            float distance = Vector3.Distance(position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = point;
            }
        }

        return nearestPoint;
    }

    private void OnDrawGizmos()
    {
        if (!showGrid || gridPoints.Count == 0)
            return;

        Gizmos.color = gridColor;
        foreach (Vector3 point in gridPoints)
        {
            Gizmos.DrawSphere(point, 0.1f);
        }

        // Draw area boundaries
        Gizmos.color = Color.red;
        for (int i = 0; i < 4; i++)
        {
            int next = (i + 1) % 4;
            if (cornerPoints[i] != null && cornerPoints[next] != null)
            {
                Gizmos.DrawLine(cornerPoints[i].position, cornerPoints[next].position);
            }
        }
    }
} 