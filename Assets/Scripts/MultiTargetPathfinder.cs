using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Handles pathfinding between multiple targets, finding the most efficient route using BruteForce algorithm
/// </summary>
public class MultiTargetPathfinder : MonoBehaviour
{
    [Header("References")]
    public RobotController robotController;
    public RobotKinematics robotKinematics;
    public RobotPathfinding pathfinding;
    
    [Header("Target Settings")]
    [Tooltip("List of targets to visit")]
    public List<Transform> targets = new List<Transform>();
    
    [Tooltip("Should the robot return to starting position?")]
    public bool returnToStart = false;
    
    [Header("Debugging")]
    [Tooltip("Show debug lines for optimized path")]
    public bool showOptimizedPath = true;
    
    [Header("Status")]
    [SerializeField] private int currentTargetIndex = -1;
    [SerializeField] private bool routeComplete = false;
    
    // Optimization method (locked to BruteForce)
    [HideInInspector] public RouteOptimizationMethod optimizationMethod = RouteOptimizationMethod.BruteForce;
    
    // Optimized route of target indices
    private List<int> optimizedRoute = new List<int>();
    
    // Starting position
    private Vector3 startPosition;
    
    // Are we currently processing?
    private bool isProcessing = false;
    
    public enum RouteOptimizationMethod
    {
        NearestNeighbor,       // Simple greedy algorithm, fast but suboptimal
        BruteForce,            // Optimal for small numbers of targets (max ~10)
        NearestInsertion       // Better than nearest neighbor, not optimal but reasonable
    }
    
    private void Start()
    {
        if (robotController == null)
            robotController = GetComponent<RobotController>();
            
        if (pathfinding == null && robotController != null)
            pathfinding = robotController.pathfinding;
            
        if (pathfinding == null)
            pathfinding = GetComponent<RobotPathfinding>();
            
        // Store starting position
        startPosition = transform.position;
        
        // Check for errors
        if (pathfinding == null)
        {
            Debug.LogError("No RobotPathfinding component found!");
            enabled = false;
            return;
        }
        
        // Force BruteForce algorithm
        optimizationMethod = RouteOptimizationMethod.BruteForce;
        
        // Register for target reached event
        StartCoroutine(CheckTargetReachedRoutine());
    }
    
    /// <summary>
    /// Starts navigation to all targets in the optimized order
    /// </summary>
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
            Debug.LogWarning("BruteForce algorithm works best with 10 or fewer targets. Performance may be affected.");
        }
        
        isProcessing = true;
        routeComplete = false;
        currentTargetIndex = -1;
        
        // Optimize the route
        OptimizeRoute();
        
        // Start navigation to first target
        MoveToNextTarget();
    }
    
    /// <summary>
    /// Stops the current multi-target navigation
    /// </summary>
    public void StopNavigation()
    {
        isProcessing = false;
        pathfinding.target = null;
    }
    
    /// <summary>
    /// Optimize the route using the brute force method
    /// </summary>
    public void OptimizeRoute()
    {
        if (targets.Count == 0)
            return;
            
        optimizedRoute.Clear();
        OptimizeBruteForce();
        
        // Log the optimized route
        Debug.Log("Calculated optimal route using BruteForce algorithm: " + string.Join(" → ", optimizedRoute));
    }
    
    /// <summary>
    /// Move to the next target in the optimized route
    /// </summary>
    private void MoveToNextTarget()
    {
        if (!isProcessing || optimizedRoute.Count == 0)
            return;
            
        currentTargetIndex++;
        
        // Check if we've completed all targets
        if (currentTargetIndex >= optimizedRoute.Count)
        {
            // Check if we should return to start
            if (returnToStart)
            {
                Debug.Log("All targets visited, returning to start position");
                
                // Create a temporary target at the start position
                GameObject tempTarget = new GameObject("TempStartTarget");
                tempTarget.transform.position = startPosition;
                
                // Set as target
                pathfinding.target = tempTarget.transform;
                
                // Destroy after 0.1 seconds (after pathfinding has started)
                Destroy(tempTarget, 0.1f);
                
                // Mark as complete once we reach the start
                CheckReturnToStartRoutine();
            }
            else
            {
                // Route is complete
                routeComplete = true;
                isProcessing = false;
                Debug.Log("Multi-target route complete!");
            }
            
            return;
        }
        
        // Move to the next target
        int targetIndex = optimizedRoute[currentTargetIndex];
        pathfinding.target = targets[targetIndex];
        
        Debug.Log("Moving to target " + (currentTargetIndex + 1) + "/" + optimizedRoute.Count + " (index: " + targetIndex + ")");
    }
    
    /// <summary>
    /// Check if we've reached the current target
    /// </summary>
    private IEnumerator CheckTargetReachedRoutine()
    {
        float checkInterval = 0.2f;
        
        while (true)
        {
            // Skip if we're not processing or no target
            if (!isProcessing || pathfinding.target == null)
            {
                yield return new WaitForSeconds(checkInterval);
                continue;
            }
            
            // Check if target reached by looking at pathfinding
            float distance = Vector3.Distance(transform.position, pathfinding.target.position);
            
            if (distance <= pathfinding.arrivalDistance)
            {
                
                // Target reached
                Debug.Log("Target reached!");

                // Scan for bomb at this location
                SearchArea();
                
                // Wait until the area search is complete before proceeding
                // We'll use a short interval to check if isProcessing was set back to true by the SearchAreaCoroutine
                while (!isProcessing)
                {
                    yield return new WaitForSeconds(0.2f);
                }
                
                // Move to next target
                MoveToNextTarget();
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    /// <summary>
    /// Check if we've returned to the start position
    /// </summary>
    private IEnumerator CheckReturnToStartRoutine()
    {
        float checkInterval = 0.2f;
        
        while (true)
        {
            // Skip if we're not processing
            if (!isProcessing)
            {
                yield break;
            }
            
            // Check if we're back at start
            float distance = Vector3.Distance(transform.position, startPosition);
            
            if (distance <= pathfinding.arrivalDistance)
            {
                // Route is complete
                routeComplete = true;
                isProcessing = false;
                Debug.Log("Multi-target route complete! Returned to start position.");
                yield break;
            }
            
            yield return new WaitForSeconds(checkInterval);
        }
    }
    
    /// <summary>
    /// Optimize using brute force (exact optimal solution)
    /// </summary>
    private void OptimizeBruteForce()
    {
        List<int> indices = new List<int>();
        for (int i = 0; i < targets.Count; i++)
            indices.Add(i);
            
        List<int> bestRoute = new List<int>();
        float shortestDistance = float.MaxValue;
        
        // Generate all permutations and find the shortest one
        BruteForcePermutations(indices, 0, ref bestRoute, ref shortestDistance);
        
        optimizedRoute = new List<int>(bestRoute);
        
        Debug.Log("Optimal path found with total distance: " + shortestDistance.ToString("F2") + " units");
    }
    
    /// <summary>
    /// Recursive helper for brute force method to generate permutations
    /// </summary>
    private void BruteForcePermutations(List<int> indices, int k, ref List<int> bestRoute, ref float shortestDistance)
    {
        if (k == indices.Count)
        {
            // Calculate the total distance of this route
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
                // Swap
                int temp = indices[k];
                indices[k] = indices[i];
                indices[i] = temp;
                
                // Recurse
                BruteForcePermutations(indices, k + 1, ref bestRoute, ref shortestDistance);
                
                // Swap back
                temp = indices[k];
                indices[k] = indices[i];
                indices[i] = temp;
            }
        }
    }
    
    /// <summary>
    /// Calculate the total length of a route
    /// </summary>
    private float CalculateRouteLength(List<int> route)
    {
        if (route.Count < 2)
            return 0;
            
        float totalDistance = 0;
        
        // Add distance from start to first target
        Vector3 startPoint = transform.position;
        Vector3 nextPoint = targets[route[0]].position;
        
        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(startPoint, nextPoint, NavMesh.AllAreas, path))
        {
            totalDistance += CalculatePathLength(path);
        }
        else
        {
            totalDistance += Vector3.Distance(startPoint, nextPoint);
        }
        
        // Add distances between consecutive targets
        for (int i = 0; i < route.Count - 1; i++)
        {
            Vector3 fromPoint = targets[route[i]].position;
            Vector3 toPoint = targets[route[i + 1]].position;
            
            if (NavMesh.CalculatePath(fromPoint, toPoint, NavMesh.AllAreas, path))
            {
                totalDistance += CalculatePathLength(path);
            }
            else
            {
                totalDistance += Vector3.Distance(fromPoint, toPoint);
            }
        }
        
        // Add distance from last target to start if returning to start
        if (returnToStart)
        {
            Vector3 lastPoint = targets[route[route.Count - 1]].position;
            
            if (NavMesh.CalculatePath(lastPoint, startPoint, NavMesh.AllAreas, path))
            {
                totalDistance += CalculatePathLength(path);
            }
            else
            {
                totalDistance += Vector3.Distance(lastPoint, startPoint);
            }
        }
        
        return totalDistance;
    }
    
    /// <summary>
    /// Calculate the length of a NavMesh path
    /// </summary>
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
    
    /// <summary>
    /// Searches the gridded area for bombs
    /// </summary>
    private void SearchArea()
    {
        // Get the neartest GameObject with the AreaGridGenerator component
        AreaGridGenerator areaGridGenerator = FindObjectOfType<AreaGridGenerator>();
        if (areaGridGenerator == null)
        {
            Debug.LogError("No AreaGridGenerator found in scene!");
        }else{
            Debug.Log("AreaGridGenerator : " + areaGridGenerator.name);
            
            // Start the search coroutine and wait for it to complete before continuing
            StartCoroutine(SearchAreaCoroutine(areaGridGenerator));
        }
    }
    
    /// <summary>
    /// Coroutine to perform the area search and force waiting
    /// </summary>
    private IEnumerator SearchAreaCoroutine(AreaGridGenerator areaGridGenerator)
    {
        // Pause the multi-target navigation process
        bool wasProcessing = isProcessing;
        isProcessing = false;
        
        Debug.Log("Beginning area search...");
        
        // Wait for 3 seconds (or adjust time as needed)
        yield return new WaitForSeconds(5f);
        
        // Add additional search logic here
        robotKinematics.StopRobot();

        // 

        
        Debug.Log("Area search complete!");

        robotKinematics.isStopping = false;
        
        // Resume multi-target navigation
        isProcessing = wasProcessing;
    }
    
    private void OnDrawGizmos()
    {
        if (!showOptimizedPath || optimizedRoute.Count == 0 || targets.Count == 0)
            return;
        
        // Draw optimized route
        Gizmos.color = Color.yellow;
        
        // Draw line from start to first target
        if (optimizedRoute.Count > 0)
        {
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawLine(start, targets[optimizedRoute[0]].position);
        }
        
        // Draw lines between targets
        for (int i = 0; i < optimizedRoute.Count - 1; i++)
        {
            Gizmos.DrawLine(targets[optimizedRoute[i]].position, targets[optimizedRoute[i + 1]].position);
        }
        
        // Draw line from last target back to start if returning
        if (returnToStart && optimizedRoute.Count > 0)
        {
            Vector3 start = Application.isPlaying ? startPosition : transform.position;
            Gizmos.DrawLine(targets[optimizedRoute[optimizedRoute.Count - 1]].position, start);
        }
        
        // Draw spheres at each target position
        for (int i = 0; i < targets.Count; i++)
        {
            // Current target is red, others are green
            int routeIndex = optimizedRoute.IndexOf(i);
            if (routeIndex == currentTargetIndex && Application.isPlaying)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(targets[i].position, 0.3f);
            }
            else
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(targets[i].position, 0.2f);
                
                // Editor-only code for drawing labels
                #if UNITY_EDITOR
                Handles.Label(targets[i].position + Vector3.up * 0.3f, 
                    (routeIndex != -1) ? (routeIndex + 1).ToString() : "?");
                #endif
            }
        }
    }
} 