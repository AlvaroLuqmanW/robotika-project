using System.Collections.Generic;
using UnityEngine;

public class GridGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public float mapWidth = 100f;
    public float mapHeight = 100f;
    public float gridSpacing = 5f;

    public List<Vector3> gridPoints = new List<Vector3>();

    void Awake()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        gridPoints.Clear();
        Vector3 startPoint = transform.position;

        int xCount = Mathf.CeilToInt(mapWidth / gridSpacing);
        int zCount = Mathf.CeilToInt(mapHeight / gridSpacing);

        for (int x = 0; x <= xCount; x++)
        {
            for (int z = 0; z <= zCount; z++)
            {
                Vector3 point = startPoint + new Vector3(x * gridSpacing, 0f, z * gridSpacing);
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(point, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    gridPoints.Add(hit.position);
                }
            }
        }

        Debug.Log($"Generated {gridPoints.Count} grid points.");
    }

    void OnDrawGizmos()
    {
        if (gridPoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (var point in gridPoints)
        {
            Gizmos.DrawSphere(point + Vector3.up * 0.5f, 0.3f);
        }
    }
}
