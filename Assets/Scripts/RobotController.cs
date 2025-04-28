using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Main robot controller that helps set up and manage the relationship
/// between robot pathfinding and kinematics.
/// </summary>
public class RobotController : MonoBehaviour
{
    [Header("Required Components")]
    [Tooltip("Reference to the robot's pathfinding component")]
    public RobotPathfinding pathfinding;
    
    [Tooltip("Reference to the robot's kinematics component")]
    public RobotKinematics kinematics;
    
    [Header("Setup Options")]
    [Tooltip("If true, will automatically connect the components on Start")]
    public bool autoConnect = true;
    
    [Tooltip("If true, will attempt to find components if not assigned")]
    public bool autoFind = true;
    
    void Start()
    {
        if (autoFind)
        {
            FindComponents();
        }
        
        if (autoConnect)
        {
            ConnectComponents();
        }
    }
    
    /// <summary>
    /// Auto-finds the pathfinding and kinematics components if they're not assigned
    /// </summary>
    public void FindComponents()
    {
        if (pathfinding == null)
        {
            pathfinding = GetComponent<RobotPathfinding>();
            
            if (pathfinding == null)
            {
                pathfinding = GetComponentInChildren<RobotPathfinding>();
                
                if (pathfinding == null)
                {
                    Debug.LogWarning("No RobotPathfinding component found on this GameObject or its children");
                }
            }
        }
        
        if (kinematics == null)
        {
            kinematics = GetComponent<RobotKinematics>();
            
            if (kinematics == null)
            {
                kinematics = GetComponentInChildren<RobotKinematics>();
                
                if (kinematics == null)
                {
                    Debug.LogWarning("No RobotKinematics component found on this GameObject or its children");
                }
            }
        }
    }
    
    /// <summary>
    /// Connects the pathfinding and kinematics components together
    /// </summary>
    public void ConnectComponents()
    {
        if (pathfinding != null && kinematics != null)
        {
            // Connect pathfinding to kinematics
            pathfinding.robotKinematics = kinematics;
            
            // Sync initial values
            pathfinding.slowingDistance = kinematics.slowingDistance;
            
            Debug.Log("Connected RobotPathfinding and RobotKinematics components successfully");
        }
        else
        {
            Debug.LogError("Cannot connect components - one or both are missing");
        }
    }
    
    /// <summary>
    /// Helper to set the target for the robot to navigate to
    /// </summary>
    public void SetTarget(Transform target)
    {
        if (pathfinding != null)
        {
            pathfinding.target = target;
        }
    }
    
    /// <summary>
    /// Visualize the connections in the Editor
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Draw a line connecting the pathfinding and kinematics components
        if (pathfinding != null && kinematics != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                pathfinding.transform.position + Vector3.up * 0.1f,
                kinematics.transform.position + Vector3.up * 0.1f
            );
        }
    }
} 