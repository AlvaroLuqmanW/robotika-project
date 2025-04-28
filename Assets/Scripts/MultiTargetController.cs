using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controller for automatically handling multi-target navigation with BruteForce optimization
/// </summary>
public class MultiTargetController : MonoBehaviour
{
    [Header("References")]
    public MultiTargetPathfinder pathfinder;
    
    [Header("Target Settings")]
    [Tooltip("Target positions to navigate to")]
    public Vector3[] targetPositions = new Vector3[3];
    
    [Tooltip("Should the robot return to starting position after visiting all targets?")]
    public bool returnToStart = true;
    
    [Header("Navigation Settings")]
    [Tooltip("Delay before starting navigation (seconds)")]
    public float startDelay = 1.0f;
    
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
        
        // Start navigation after delay
        StartCoroutine(StartNavigationAfterDelay());
    }
    
    /// <summary>
    /// Starts navigation after a short delay
    /// </summary>
    private IEnumerator StartNavigationAfterDelay()
    {
        yield return new WaitForSeconds(startDelay);
        
        Debug.Log("Starting multi-target navigation with BruteForce optimization");
        pathfinder.StartMultiTargetNavigation();
    }
    
    /// <summary>
    /// Add target positions from the inspector array to the pathfinder
    /// </summary>
    private void AddTargetsToPathfinder()
    {
        pathfinder.targets.Clear();
        
        foreach (Vector3 position in targetPositions)
        {
            // Create a temporary transform to represent the target position
            GameObject tempTarget = new GameObject("TempTarget");
            tempTarget.transform.position = position;
            pathfinder.targets.Add(tempTarget.transform);
        }
        
        if (pathfinder.targets.Count == 0)
        {
            Debug.LogWarning("No target positions set for multi-target navigation!");
        }
        else
        {
            Debug.Log("Added " + pathfinder.targets.Count + " target positions to pathfinder");
        }
    }
    
    /// <summary>
    /// Clean up temporary target objects when the script is disabled
    /// </summary>
    private void OnDisable()
    {
        if (pathfinder != null)
        {
            foreach (Transform target in pathfinder.targets)
            {
                if (target != null && target.name == "TempTarget")
                {
                    Destroy(target.gameObject);
                }
            }
        }
    }
} 