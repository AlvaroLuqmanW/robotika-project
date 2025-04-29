using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controller for automatically handling multi-target navigation with BruteForce optimization
/// and area-based bomb search
/// </summary>
public class MultiTargetController : MonoBehaviour
{
    [Header("References")]
    public MultiTargetPathfinder pathfinder;
    public NavMeshAgent agent;
    
    [Header("Target Settings")]
    [Tooltip("Target GameObjects to navigate to")]
    public GameObject[] targetObjects = new GameObject[3];
    
    [Tooltip("Should the robot return to starting position after visiting all targets?")]
    public bool returnToStart = true;
    
    [Header("Navigation Settings")]
    [Tooltip("Delay before starting navigation (seconds)")]
    public float startDelay = 1.0f;
    
    [Header("Bomb Search Settings")]
    [Tooltip("Lidar detection radius")]
    public float lidarRadius = 5f;
    [Tooltip("Lidar scan interval in seconds")]
    public float scanInterval = 0.2f;
    [Tooltip("How close the robot needs to be to a bomb to consider it 'visited'")]
    public float bombVisitDistance = 1.0f;
    [Tooltip("Layer mask for bomb detection")]
    public LayerMask bombLayerMask = -1;

    // Search state variables
    private enum RobotState { NavigatingToArea, SearchingArea, MovingToBomb, BombVisited }
    private RobotState currentState = RobotState.NavigatingToArea;
    private GameObject currentAreaTarget;
    private GameObject detectedBomb;
    private AreaGridGenerator currentAreaGrid;
    private List<Vector3> searchGridPoints = new List<Vector3>();
    private int currentGridPointIndex = 0;
    
    private void Start()
    {
        // Get pathfinder if not set
        if (pathfinder == null)
            pathfinder = GetComponent<MultiTargetPathfinder>();
            
        if (pathfinder == null)
        {
            Debug.LogError("No MultiTargetPathfinder component found!");
            enabled = false;
            return;
        }
        
        // Configure pathfinder
        pathfinder.returnToStart = returnToStart;
        
        // Add targets from inspector
        AddTargetsToPathfinder();
        
        // Override the pathfinder's target reached event to handle our custom behavior
        // Register with both event mechanisms to ensure compatibility
        pathfinder.OnTargetReached = HandleTargetReached;
        pathfinder.onTargetReached += HandleTargetReached;
        
        Debug.Log("MultiTargetController registered with pathfinder events");
        
        // Start navigation after delay
        StartCoroutine(StartNavigationAfterDelay());
    }
    
    /// <summary>
    /// Starts navigation after a short delay
    /// </summary>
    private IEnumerator StartNavigationAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        
        Debug.Log("Starting multi-target navigation with area-based bomb search");
        pathfinder.StartMultiTargetNavigation();
    }
    
    /// <summary>
    /// Add target GameObjects from the inspector array to the pathfinder
    /// </summary>
    private void AddTargetsToPathfinder()
    {
        pathfinder.targets.Clear();
        
        foreach (GameObject targetObject in targetObjects)
        {
            if (targetObject != null)
            {
                print("Adding target: " + targetObject.transform.position);
                pathfinder.targets.Add(targetObject.transform);
            }
        }
        
        if (pathfinder.targets.Count == 0)
        {
            Debug.LogWarning("No target objects set for multi-target navigation!");
        }
        else
        {
            Debug.Log("Added " + pathfinder.targets.Count + " target objects to pathfinder");
        }
    }
    
    /// <summary>
    /// Called every frame to update the robot's behavior based on its current state
    /// </summary>
    private void Update()
    {
        switch (currentState)
        {
            case RobotState.NavigatingToArea:
                // Handled by the pathfinder
                break;
                
            case RobotState.SearchingArea:
                SearchForBomb();
                break;
                
            case RobotState.MovingToBomb:
                if (detectedBomb != null && Vector3.Distance(transform.position, detectedBomb.transform.position) <= bombVisitDistance)
                {
                    Debug.Log("Bomb visited!");
                    currentState = RobotState.BombVisited;
                    ContinueToNextTarget();
                }
                break;
                
            case RobotState.BombVisited:
                // Waiting for ContinueToNextTarget to reset the state
                break;
        }
    }
    
    /// <summary>
    /// Handler for when the robot reaches a target area
    /// </summary>
    /// <param name="targetTransform">The transform of the reached target</param>
    private void HandleTargetReached(Transform targetTransform)
    {
        Debug.Log("Reached target area: " + targetTransform.name);
        
        // Save the current area target
        currentAreaTarget = targetTransform.gameObject;
        
        // Get the area grid generator
        currentAreaGrid = currentAreaTarget.GetComponent<AreaGridGenerator>();
        if (currentAreaGrid == null)
        {
            Debug.LogWarning("No AreaGridGenerator found on target. Looking for it in children...");
            currentAreaGrid = currentAreaTarget.GetComponentInChildren<AreaGridGenerator>();
        }
        
        if (currentAreaGrid != null)
        {
            Debug.Log("Found AreaGridGenerator on target: " + currentAreaGrid.name);
            
            // Get grid points for searching
            searchGridPoints = currentAreaGrid.GetGridPoints();
            Debug.Log("Retrieved " + searchGridPoints.Count + " grid points from AreaGridGenerator");
            
            currentGridPointIndex = 0;
            
            // Add a small delay before starting search to ensure we're fully stopped
            StartCoroutine(DelayedSearchStart());
        }
        else
        {
            Debug.LogError("No AreaGridGenerator component found on or in target!");
            ContinueToNextTarget();
        }
    }
    
    /// <summary>
    /// Adds a delayed search start coroutine
    /// </summary>
    private IEnumerator DelayedSearchStart()
    {
        Debug.Log("Waiting briefly before starting area search...");
        yield return new WaitForSeconds(0.5f);
        
        // Switch to searching state
        currentState = RobotState.SearchingArea;
        Debug.Log("Starting area search with " + searchGridPoints.Count + " grid points");
        
        // Start search with first grid point using pathfinder
        if (searchGridPoints.Count > 0)
        {
            Debug.Log("Setting first search point: " + searchGridPoints[currentGridPointIndex]);
            pathfinder.pathfinding.target = CreateTempTarget(searchGridPoints[currentGridPointIndex]);
            
            // Start regular Lidar scanning for bombs
            StartCoroutine(ScanForBombsRoutine());
        }
        else
        {
            Debug.LogWarning("No grid points available for search!");
            ContinueToNextTarget();
        }
    }
    
    /// <summary>
    /// Searches for bombs using SphereCast at regular intervals
    /// </summary>
    private IEnumerator ScanForBombsRoutine()
    {
        while (currentState == RobotState.SearchingArea || currentState == RobotState.MovingToBomb)
        {
            // Perform the bomb scan
            RaycastHit[] hits = Physics.SphereCastAll(transform.position, lidarRadius, transform.forward, 0.1f, bombLayerMask);
            
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Bombs"))
                {
                    Debug.Log("Bomb detected: " + hit.collider.gameObject.name);
                    detectedBomb = hit.collider.gameObject;
                    
                    // Switch to moving to bomb state
                    currentState = RobotState.MovingToBomb;
                    
                    // Navigate to the bomb using pathfinder
                    pathfinder.pathfinding.target = detectedBomb.transform;
                    
                    // Break out of the loop once we find a bomb
                    break;
                }
            }
            
            // Wait for the next scan interval
            yield return new WaitForSeconds(scanInterval);
        }
    }
    
    /// <summary>
    /// Handles the grid-based area search behavior
    /// </summary>
    private void SearchForBomb()
    {
        // If we're already moving to a bomb, don't continue searching
        if (currentState != RobotState.SearchingArea)
            return;
            
        // Check if we've reached the current grid point (using pathfinder distance check)
        float distanceToPoint = Vector3.Distance(transform.position, searchGridPoints[currentGridPointIndex]);
        
        if (distanceToPoint <= pathfinder.pathfinding.arrivalDistance)
        {
            // Move to the next grid point
            currentGridPointIndex++;
            
            // If we've checked all grid points and found nothing, move to next target
            if (currentGridPointIndex >= searchGridPoints.Count)
            {
                Debug.Log("Searched entire area, no bomb found. Moving to next target.");
                ContinueToNextTarget();
                return;
            }
            
            // Set the destination to the next grid point using pathfinder
            pathfinder.pathfinding.target = CreateTempTarget(searchGridPoints[currentGridPointIndex]);
        }
    }
    
    /// <summary>
    /// Continues to the next target in the pathfinder's list
    /// </summary>
    private void ContinueToNextTarget()
    {
        // Reset state
        currentState = RobotState.NavigatingToArea;
        detectedBomb = null;
        
        // Tell the pathfinder to continue to the next target
        pathfinder.MoveToNextTarget();
    }
    
    /// <summary>
    /// Draw gizmos for visualization
    /// </summary>
    private void OnDrawGizmos()
    {
        // Visualize the Lidar radius
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, lidarRadius);
            
            // Visualize current target and bomb if detected
            if (currentAreaTarget != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentAreaTarget.transform.position, 0.5f);
            }
            
            if (detectedBomb != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, detectedBomb.transform.position);
                Gizmos.DrawSphere(detectedBomb.transform.position, 0.5f);
            }
        }
    }

    // Add helper method to create temporary targets
    private Transform CreateTempTarget(Vector3 position)
    {
        GameObject tempTarget = new GameObject("TempSearchTarget");
        tempTarget.transform.position = position;
        Destroy(tempTarget, 5f); // Clean up after a delay
        return tempTarget.transform;
    }
} 