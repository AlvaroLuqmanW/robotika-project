using UnityEngine;
using UnityEngine.AI;

[ExecuteInEditMode]
public class NavMeshPathTester : MonoBehaviour
{
    [Header("Setup Instructions")]
    [TextArea(5, 10)]
    public string instructions = 
        "1. Make sure your scene has a NavMesh baked\n" +
        "2. Attach this script to any GameObject\n" +
        "3. Attach TargetController script to this GameObject or another in the scene\n" +
        "4. Optional: Attach NavMeshVisualizer to see the walkable areas\n" +
        "5. Ensure your robot has the RobotKinematics script attached\n" +
        "6. Play the scene and click on the ground to set a destination";

    [Header("Debug Settings")]
    public bool showSetupHelpers = true;
    public bool createComponents = true;

    // Reference to created GameObjects
    private GameObject targetControllerObj;
    private GameObject visualizerObj;

    public void OnEnable()
    {
        if (!Application.isPlaying && showSetupHelpers)
        {
            Debug.Log("NavMesh Path Tester: Follow instructions in the Inspector");
        }
    }

    public void OnValidate()
    {
        if (Application.isPlaying || !createComponents)
            return;

        // Create target controller object if it doesn't exist
        if (GetComponent<TargetController>() == null && 
            FindObjectOfType<TargetController>() == null)
        {
            targetControllerObj = new GameObject("Target Controller");
            targetControllerObj.transform.SetParent(transform);
            targetControllerObj.AddComponent<TargetController>();
            Debug.Log("Created Target Controller GameObject");
        }

        // Create visualizer if it doesn't exist
        if (FindObjectOfType<NavMeshVisualizer>() == null)
        {
            visualizerObj = new GameObject("NavMesh Visualizer");
            visualizerObj.transform.SetParent(transform);
            NavMeshVisualizer visualizer = visualizerObj.AddComponent<NavMeshVisualizer>();
            
            // Set appropriate size based on scene
            Bounds sceneBounds = new Bounds();
            bool boundsInitialized = false;
            
            // Find renderer bounds in scene
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (!boundsInitialized)
                {
                    sceneBounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    sceneBounds.Encapsulate(renderer.bounds);
                }
            }
            
            if (boundsInitialized)
            {
                visualizer.center = sceneBounds.center;
                Vector3 size = sceneBounds.size;
                size.y = 0.1f; // Reduce height for visualization
                visualizer.size = size;
            }
            
            Debug.Log("Created NavMesh Visualizer GameObject");
        }
    }

    [ContextMenu("Check NavMesh Setup")]
    void CheckNavMeshSetup()
    {
        // Check if NavMesh exists in the scene
        NavMeshHit hit;
        bool hasNavMesh = NavMesh.SamplePosition(Vector3.zero, out hit, 1000, NavMesh.AllAreas);
        
        if (hasNavMesh)
        {
            Debug.Log("NavMesh found in the scene!");
        }
        else
        {
            Debug.LogError("No NavMesh found! Please bake a NavMesh in Window > AI > Navigation.");
        }
        
        // Check if robot with RobotKinematics exists
        RobotKinematics robot = FindObjectOfType<RobotKinematics>();
        if (robot != null)
        {
            Debug.Log("Robot with RobotKinematics found!");
        }
        else
        {
            Debug.LogError("No Robot with RobotKinematics script found in the scene!");
        }
    }
} 