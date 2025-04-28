using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LocalAStarSearch : MonoBehaviour
{
    [Header("References")]
    public RobotPathfinding pathfinding;
    public Transform robotTransform;

    [Header("Search Settings")]
    public float nodeSpacing = 2f;
    public float searchRadius = 15f;
    public float bombDetectRange = 4f;

    [Header("Visual Settings")]
    public bool drawGridGizmos = true;
    public Color gridColor = Color.cyan;
    public Color visitedColor = Color.red;

    [HideInInspector] public bool isSearching = false;  // <- PUBLIC SEKARANG

    private Queue<Vector3> searchQueue = new Queue<Vector3>();
    private List<Vector3> visitedNodes = new List<Vector3>();

    void Start()
    {
        if (robotTransform == null) robotTransform = transform;
        if (pathfinding == null) pathfinding = GetComponent<RobotPathfinding>();
    }

    public void BeginSearch()
    {
        if (isSearching) return;

        Debug.Log("<color=cyan>[Mission]</color> Started Local A* Search!");

        GenerateSearchGrid();
        isSearching = true;
    }

    void Update()
    {
        if (!isSearching || searchQueue.Count == 0) return;

        if (!pathfinding.target || pathfinding.distanceToTarget <= pathfinding.arrivalDistance)
        {
            Vector3 nextTarget = searchQueue.Dequeue();

            GameObject temp = new GameObject("SearchNode");
            temp.transform.position = nextTarget;
            Destroy(temp, 2f);

            pathfinding.target = temp.transform;
            visitedNodes.Add(nextTarget);

            Debug.Log("Searching next point...");
        }

        Collider[] hits = Physics.OverlapSphere(robotTransform.position, bombDetectRange);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bombs"))
            {
                Debug.Log("<color=lime>[Mission]</color> Bomb Found! Local search complete!");
                isSearching = false;
                return;
            }
        }

        if (searchQueue.Count == 0 && isSearching)
        {
            Debug.Log("<color=yellow>[Mission]</color> Finished sweeping this area, no bomb found.");
            isSearching = false;
        }
    }

    void GenerateSearchGrid()
    {
        searchQueue.Clear();
        visitedNodes.Clear();

        Vector3 center = robotTransform.position;
        int steps = Mathf.CeilToInt(searchRadius / nodeSpacing);

        for (int x = -steps; x <= steps; x++)
        {
            for (int z = -steps; z <= steps; z++)
            {
                Vector3 point = center + new Vector3(x * nodeSpacing, 0, z * nodeSpacing);

                if (Vector3.Distance(center, point) <= searchRadius && IsWalkable(point))
                {
                    searchQueue.Enqueue(point);
                }
            }
        }

        Debug.Log("Generated " + searchQueue.Count + " search points.");
    }

    bool IsWalkable(Vector3 position)
    {
        NavMeshHit hit;
        return NavMesh.SamplePosition(position, out hit, 1.0f, NavMesh.AllAreas);
    }

    void OnDrawGizmos()
    {
        if (!drawGridGizmos) return;

        Gizmos.color = gridColor;
        foreach (var p in searchQueue)
        {
            Gizmos.DrawWireSphere(p, 0.3f);
        }

        Gizmos.color = visitedColor;
        foreach (var p in visitedNodes)
        {
            Gizmos.DrawSphere(p, 0.3f);
        }
    }
}
