using System;
using UnityEngine;
using UnityEngine.AI;

public class RobotPathfinding : MonoBehaviour
{
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
    public float distanceToTarget { get; private set; } = float.MaxValue;
    private bool targetReached = false;
    private RobotController robotController;

    void Start()
    {
        navPath = new NavMeshPath();
        robotController = GetComponent<RobotController>();

        if (target == null)
        {
            currentPathPoint = transform.position;
        }

        if (robotKinematics != null)
        {
            robotKinematics.currentPathPoint = transform.position;
            robotKinematics.slowingDistance = slowingDistance;
        }
        else
        {
            Debug.LogError("RobotKinematics reference is missing! Please assign it in the inspector.");
        }
    }

    void FixedUpdate()
    {
        if (robotKinematics == null) return;

        UpdatePath();
        robotKinematics.Sensors();
        robotKinematics.LerpToSteerAngle();

        if (target)
        {
            Vector3 currentPosition = robotController.GetEstimatedPosition();
            distanceToTarget = Vector3.Distance(currentPosition, target.position);

            robotKinematics.currentPathPoint = currentPathPoint;
            robotKinematics.distanceToTarget = distanceToTarget;

            if (distanceToTarget <= arrivalDistance)
            {
                if (!targetReached)
                {
                    targetReached = true;
                    robotKinematics.StopRobot();

                    if (showDebugInfo) Debug.Log("Target reached!");
                }
            }
            else
            {
                targetReached = false;
                robotKinematics.ApplySteer();
                robotKinematics.Drive();
            }
        }
    }

    void UpdatePath()
    {
        if (target != null)
        {
            Vector3 currentPosition = robotController.GetEstimatedPosition();
            NavMesh.CalculatePath(currentPosition, target.position, NavMesh.AllAreas, navPath);

            if (navPath.corners.Length > 0)
            {
                currentPathIndex = 0;
                float distanceSum = 0;

                for (int i = 0; i < navPath.corners.Length - 1; i++)
                {
                    distanceSum += Vector3.Distance(navPath.corners[i], navPath.corners[i + 1]);
                    if (distanceSum >= lookAheadDistance)
                    {
                        currentPathIndex = i + 1;
                        break;
                    }
                }

                if (currentPathIndex == 0 && navPath.corners.Length > 1)
                {
                    currentPathIndex = navPath.corners.Length - 1;
                }

                currentPathPoint = navPath.corners[currentPathIndex];
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!debugPath || navPath == null || navPath.corners.Length == 0)
            return;

        Gizmos.color = Color.blue;
        for (int i = 0; i < navPath.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(navPath.corners[i], navPath.corners[i + 1]);
        }

        if (currentPathIndex < navPath.corners.Length)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentPathPoint, 0.2f);
        }

        if (target != null && showDebugInfo)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(target.position, arrivalDistance);

            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(target.position, slowingDistance);
        }
    }

    void OnGUI()
    {
        if (showDebugInfo && target != null)
        {
            GUI.Label(new Rect(10, 10, 200, 20), "Distance to target: " + distanceToTarget.ToString("F2") + "m");
            GUI.Label(new Rect(10, 30, 200, 20), "Status: " + (targetReached ? "ARRIVED" : "MOVING"));
        }
    }
}
