using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MultiTargetPathfinder : MonoBehaviour
{
    [Header("References")]
    public RobotController robotController;
    public RobotPathfinding pathfinding;

    [Header("Target Settings")]
    public List<Transform> targets = new List<Transform>();
    public bool returnToStart = false;

    [Header("Debugging")]
    public bool showOptimizedPath = true;

    [SerializeField] private int currentTargetIndex = -1;
    [SerializeField] private bool routeComplete = false;

    private List<int> optimizedRoute = new List<int>();
    private Vector3 startPosition;
    private bool isProcessing = false;

    public enum RouteOptimizationMethod
    {
        NearestNeighbor,
        BruteForce,
        NearestInsertion
    }

    private void Start()
    {
        if (robotController == null)
            robotController = GetComponent<RobotController>();

        if (pathfinding == null && robotController != null)
            pathfinding = robotController.pathfinding;

        if (pathfinding == null)
            pathfinding = GetComponent<RobotPathfinding>();

        startPosition = transform.position;

        if (pathfinding == null)
        {
            Debug.LogError("No RobotPathfinding component found!");
            enabled = false;
            return;
        }

        StartCoroutine(CheckTargetReachedRoutine());
    }

    public void StartMultiTargetNavigation()
    {
        if (targets.Count == 0)
        {
            Debug.LogWarning("No targets set for multi-target navigation!");
            return;
        }

        if (isProcessing)
        {
            Debug.LogWarning("Already processing a route!");
            return;
        }

        if (targets.Count > 10)
        {
            Debug.LogWarning("BruteForce algorithm best for 10 or fewer targets.");
        }

        isProcessing = true;
        routeComplete = false;
        currentTargetIndex = -1;

        OptimizeRoute();
        MoveToNextTarget();
    }

    public void StopNavigation()
    {
        isProcessing = false;
        pathfinding.target = null;
    }

    public void ForceMoveToNextTarget()
    {
        if (optimizedRoute == null || optimizedRoute.Count == 0) return;
        MoveToNextTarget();
    }

    private void OptimizeRoute()
    {
        if (targets.Count == 0) return;

        optimizedRoute.Clear();
        OptimizeBruteForce();

        Debug.Log("Calculated optimal route using BruteForce algorithm: " + string.Join(" → ", optimizedRoute));
    }

    private void MoveToNextTarget()
    {
        if (!isProcessing || optimizedRoute.Count == 0)
            return;

        currentTargetIndex++;

        if (currentTargetIndex >= optimizedRoute.Count)
        {
            if (returnToStart)
            {
                Debug.Log("All targets visited, returning to start position");

                GameObject tempTarget = new GameObject("TempStartTarget");
                tempTarget.transform.position = startPosition;
                pathfinding.target = tempTarget.transform;
                Destroy(tempTarget, 0.1f);

                StartCoroutine(CheckReturnToStartRoutine());
            }
            else
            {
                routeComplete = true;
                isProcessing = false;
                Debug.Log("Multi-target route complete!");
            }
            return;
        }

        int targetIndex = optimizedRoute[currentTargetIndex];
        pathfinding.target = targets[targetIndex];

        Debug.Log("Moving to target " + (currentTargetIndex + 1) + "/" + optimizedRoute.Count);
    }

    private IEnumerator CheckTargetReachedRoutine()
    {
        float checkInterval = 0.2f;

        while (true)
        {
            if (!isProcessing || pathfinding.target == null)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }

            float distance = Vector3.Distance(transform.position, pathfinding.target.position);

            if (distance <= pathfinding.arrivalDistance)
            {
                yield return new WaitForSeconds(0.5f);
                MoveToNextTarget();
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private IEnumerator CheckReturnToStartRoutine()
    {
        float checkInterval = 0.2f;

        while (true)
        {
            if (!isProcessing)
                yield break;

            float distance = Vector3.Distance(transform.position, startPosition);

            if (distance <= pathfinding.arrivalDistance)
            {
                routeComplete = true;
                isProcessing = false;
                Debug.Log("Multi-target route complete! Returned to start position.");
                yield break;
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void OptimizeBruteForce()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < targets.Count; i++)
            indices.Add(i);

        List<int> bestRoute = new List<int>();
        float shortestDistance = float.MaxValue;

        BruteForcePermutations(indices, 0, ref bestRoute, ref shortestDistance);

        optimizedRoute = new List<int>(bestRoute);

        Debug.Log("Optimal path found with total distance: " + shortestDistance.ToString("F2") + " units");
    }

    private void BruteForcePermutations(List<int> indices, int k, ref List<int> bestRoute, ref float shortestDistance)
    {
        if (k == indices.Count)
        {
            float totalDistance = CalculateRouteLength(indices);

            if (totalDistance < shortestDistance)
            {
                shortestDistance = totalDistance;
                bestRoute = new List<int>(indices);
            }
        }
        else
        {
            for (int i = k; i < indices.Count; i++)
            {
                int temp = indices[k];
                indices[k] = indices[i];
                indices[i] = temp;

                BruteForcePermutations(indices, k + 1, ref bestRoute, ref shortestDistance);

                temp = indices[k];
                indices[k] = indices[i];
                indices[i] = temp;
            }
        }
    }

    private float CalculateRouteLength(List<int> route)
    {
        if (route.Count < 2)
            return 0;

        float totalDistance = 0;
        Vector3 startPoint = transform.position;
        Vector3 nextPoint = targets[route[0]].position;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(startPoint, nextPoint, NavMesh.AllAreas, path))
            totalDistance += CalculatePathLength(path);
        else
            totalDistance += Vector3.Distance(startPoint, nextPoint);

        for (int i = 0; i < route.Count - 1; i++)
        {
            Vector3 fromPoint = targets[route[i]].position;
            Vector3 toPoint = targets[route[i + 1]].position;

            if (NavMesh.CalculatePath(fromPoint, toPoint, NavMesh.AllAreas, path))
                totalDistance += CalculatePathLength(path);
            else
                totalDistance += Vector3.Distance(fromPoint, toPoint);
        }

        if (returnToStart)
        {
            Vector3 lastPoint = targets[route[route.Count - 1]].position;

            if (NavMesh.CalculatePath(lastPoint, startPoint, NavMesh.AllAreas, path))
                totalDistance += CalculatePathLength(path);
            else
                totalDistance += Vector3.Distance(lastPoint, startPoint);
        }

        return totalDistance;
    }

    private float CalculatePathLength(NavMeshPath path)
    {
        float length = 0;

        if (path.corners.Length < 2)
            return 0;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            length += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return length;
    }
}
