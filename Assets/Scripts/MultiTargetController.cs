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
    [Tooltip("Target GameObjects to navigate to")]
    public GameObject[] targets = new GameObject[3];
    
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
    /// Add targets from the inspector array to the pathfinder
    /// </summary>
    private void AddTargetsToPathfinder()
    {
        pathfinder.targets.Clear();
        
        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                pathfinder.targets.Add(target.transform);
            }
        }
        
        if (pathfinder.targets.Count == 0)
        {
            Debug.LogWarning("No targets set for multi-target navigation!");
        }
        else
        {
            Debug.Log("Added " + pathfinder.targets.Count + " targets to pathfinder");
        }
    }
} 