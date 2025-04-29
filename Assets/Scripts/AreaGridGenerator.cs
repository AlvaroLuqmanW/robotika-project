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
    public GameObject gridPointPrefab; // Prefab to instantiate for each grid point

    private List<GameObject> gridObjects = new List<GameObject>();
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
        // Clear any existing grid objects
        ClearGrid();

        // Calculate grid dimensions
        float width = areaBounds.size.x;
        float length = areaBounds.size.z;
        int pointsX = Mathf.CeilToInt(width / gridSpacing);
        int pointsZ = Mathf.CeilToInt(length / gridSpacing);

        // Make sure we have a prefab
        if (gridPointPrefab == null)
        {
            Debug.LogError("Grid Point Prefab is not assigned!");
            return;
        }

        // Create a parent object for grid points
        Transform gridParent = new GameObject("Grid Points").transform;
        gridParent.SetParent(transform);

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
                            // Instantiate grid point GameObject
                            GameObject gridObject = Instantiate(gridPointPrefab, point, Quaternion.identity, gridParent);
                            gridObject.name = $"GridPoint_X{x}_Z{z}";
                            gridObjects.Add(gridObject);
                        }
                    }
                }
            }
        }
    }

    private void ClearGrid()
    {
        // Destroy all existing grid GameObjects
        foreach (GameObject gridObject in gridObjects)
        {
            if (gridObject != null)
            {
                DestroyImmediate(gridObject);
            }
        }
        
        // Clear the list
        gridObjects.Clear();
        
        // Remove any existing grid parent
        Transform gridParent = transform.Find("Grid Points");
        if (gridParent != null)
        {
            DestroyImmediate(gridParent.gameObject);
        }
    }

    public List<GameObject> GetGridObjects()
    {
        return gridObjects;
    }

    public List<Vector3> GetGridPoints()
    {
        List<Vector3> points = new List<Vector3>();
        foreach (GameObject obj in gridObjects)
        {
            if (obj != null)
            {
                points.Add(obj.transform.position);
            }
        }
        return points;
    }

    public GameObject GetNearestGridObject(Vector3 position)
    {
        GameObject nearestObject = null;
        float minDistance = float.MaxValue;

        foreach (GameObject obj in gridObjects)
        {
            if (obj != null)
            {
                float distance = Vector3.Distance(position, obj.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestObject = obj;
                }
            }
        }

        return nearestObject;
    }

    public Vector3 GetNearestGridPoint(Vector3 position)
    {
        GameObject nearestObject = GetNearestGridObject(position);
        return nearestObject != null ? nearestObject.transform.position : Vector3.zero;
    }

    private void OnDrawGizmos()
    {
        if (!showGrid)
            return;
            
        // If in edit mode, we might not have grid objects, so draw based on corner points
        if (gridObjects.Count == 0 && cornerPoints != null && cornerPoints.Length == 4)
        {
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
            return;
        }

        // Draw grid points (if the game is running, we'll have actual grid objects)
        Gizmos.color = gridColor;
        foreach (GameObject obj in gridObjects)
        {
            if (obj != null)
            {
                Gizmos.DrawSphere(obj.transform.position, 0.1f);
            }
        }

        // Draw area boundaries
        if (cornerPoints != null && cornerPoints.Length == 4)
        {
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

    // Call this method to regenerate the grid at runtime
    public void RegenerateGrid()
    {
        GenerateGrid();
    }
} 