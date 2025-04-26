using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class RobotPathfinding: MonoBehaviour{
    [Header("Robot Kinematics")]
    public RobotKinematics robotKinematics;
    [Header("NavMesh Parameters")]
    public float lookAheadDistance = 2.0f;
    public Transform target;
    public bool debugPath = true;
    [Header("Target Detection")]
    public float arrivalDistance = 1.0f;
    public float slowingDistance = 3.0f;
    public bool showDebugInfo = true;
    private NavMeshPath navPath;
    private Vector3 currentPathPoint;
    private int currentPathIndex = 0;
    private bool targetReached = false;
    private float distanceToTarget = float.MaxValue;


    void Start()
    {
        navPath = new NavMeshPath();

        if (target == null){
            currentPathPoint = transform.position;
        }
    }

    void FixedUpdate() {
        UpdatePath();
        
        if (target) {
            // Calculate distance to target
            distanceToTarget = Vector3.Distance(transform.position, target.position);
            
            // Check if we reached the target
            if (distanceToTarget <= arrivalDistance) {
                if (!targetReached) {
                    targetReached = true;
                    robotKinematics.StopRobot();
                    if (showDebugInfo) Debug.Log("Target reached!");
                }
            } else {
                targetReached = false;
                robotKinematics.ApplySteer();
                robotKinematics.Drive();
            }
        }
    }

    void UpdatePath() {
        if (target != null) {
            NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, navPath);
            
            if (navPath.corners.Length > 0) {
                // Find the appropriate path point to steer towards
                currentPathIndex = 0;
                float distanceSum = 0;
                
                // Find a path point that is at least lookAheadDistance away
                for (int i = 0; i < navPath.corners.Length - 1; i++) {
                    distanceSum += Vector3.Distance(navPath.corners[i], navPath.corners[i + 1]);
                    if (distanceSum >= lookAheadDistance) {
                        currentPathIndex = i + 1;
                        break;
                    }
                }
                
                // If we couldn't find a point at lookAheadDistance, use the last point
                if (currentPathIndex == 0 && navPath.corners.Length > 1) {
                    currentPathIndex = navPath.corners.Length - 1;
                }
                
                currentPathPoint = navPath.corners[currentPathIndex];
            }
        }
    }

    private void OnDrawGizmos() {
        if (!debugPath || navPath == null || navPath.corners.Length == 0)
            return;
            
        // Draw the navigation path
        Gizmos.color = Color.blue;
        for (int i = 0; i < navPath.corners.Length - 1; i++) {
            Gizmos.DrawLine(navPath.corners[i], navPath.corners[i + 1]);
        }
        
        // Mark the current target point
        if (currentPathIndex < navPath.corners.Length) {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentPathPoint, 0.2f);
        }
        
        // Draw arrival distance radius around target
        if (target != null && showDebugInfo) {
            // Arrival zone
            Gizmos.color = new Color(0, 1, 0, 0.3f); // Green transparent
            Gizmos.DrawWireSphere(target.position, arrivalDistance);
            
            // Slowing zone
            Gizmos.color = new Color(1, 1, 0, 0.3f); // Yellow transparent
            Gizmos.DrawWireSphere(target.position, slowingDistance);
        }
    }
    
    void OnGUI() {
        if (showDebugInfo && target != null) {
            GUI.Label(new Rect(10, 10, 200, 20), "Distance to target: " + distanceToTarget.ToString("F2") + "m");
            GUI.Label(new Rect(10, 30, 200, 20), "Status: " + (targetReached ? "ARRIVED" : "MOVING"));
        }
    }
}