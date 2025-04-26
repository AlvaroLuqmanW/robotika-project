using UnityEngine;
using UnityEngine.AI;

public class TargetController : MonoBehaviour
{
    [Header("Target Settings")]
    public GameObject targetMarker;
    public float markerHeight = 0.1f;
    public RobotPathfinding robotPathfinding;
    public Camera targetCamera; // Allow manual camera assignment
    
    [Header("Visual Settings")]
    public Color validPositionColor = Color.green;
    public Color invalidPositionColor = Color.red;
    
    private Camera mainCamera;
    
    void Start()
    {
        // Try to find a camera if not manually assigned
        if (targetCamera != null) 
        {
            mainCamera = targetCamera;
        }
        else 
        {
            mainCamera = Camera.main;
            
            // If still null, try to find any camera in the scene
            if (mainCamera == null)
            {
                mainCamera = FindObjectOfType<Camera>();
                
                if (mainCamera == null)
                {
                    Debug.LogError("No camera found in the scene! Please assign a camera to the TargetController or add a camera with MainCamera tag.");
                }
                else
                {
                    Debug.LogWarning("No MainCamera tag found. Using the first camera found in the scene.");
                }
            }
        }
        
        // Create a target marker if it doesn't exist
        if (targetMarker == null)
        {
            targetMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            targetMarker.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            targetMarker.name = "TargetMarker";
            
            // Remove the collider to prevent physics interactions
            Destroy(targetMarker.GetComponent<Collider>());
            
            // Set material color
            var renderer = targetMarker.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = validPositionColor;
            }
            
            targetMarker.SetActive(false);
        }
        
        // Find robot kinematics if not assigned
        if (robotPathfinding == null)
        {
            robotPathfinding = FindObjectOfType<RobotPathfinding>();
            if (robotPathfinding == null)
            {
                Debug.LogError("No robotPathfinding component found in the scene! Please assign it manually.");
            }
        }
    }
    
    void Update()
    {
        // Skip if we don't have a camera
        if (mainCamera == null)
            return;
            
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check if the hit point is on the NavMesh
                NavMeshHit navHit;
                bool validPosition = NavMesh.SamplePosition(hit.point, out navHit, 1.0f, NavMesh.AllAreas);
                
                if (validPosition)
                {
                    // Place marker at the valid NavMesh position
                    Vector3 targetPosition = navHit.position;
                    targetPosition.y += markerHeight; // Raise slightly above ground
                    targetMarker.transform.position = targetPosition;
                    
                    // Show the marker
                    targetMarker.SetActive(true);
                    
                    // Update marker color
                    var renderer = targetMarker.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = validPositionColor;
                    }
                    
                    // Update the robot's target
                    if (robotPathfinding != null)
                    {
                        robotPathfinding.target = targetMarker.transform;
                    }
                }
                else
                {
                    // Place marker at the hit position but show invalid color
                    targetMarker.transform.position = hit.point + new Vector3(0, markerHeight, 0);
                    targetMarker.SetActive(true);
                    
                    // Update marker color
                    var renderer = targetMarker.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = invalidPositionColor;
                    }
                    
                    Debug.LogWarning("Position is not on NavMesh!");
                }
            }
        }
    }
} 