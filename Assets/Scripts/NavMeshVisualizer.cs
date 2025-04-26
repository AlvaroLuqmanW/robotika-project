using UnityEngine;
using UnityEngine.AI;

public class NavMeshVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public float gridSize = 0.5f;
    public float visualizationHeight = 0.05f;
    public Color validColor = new Color(0, 1, 0, 0.3f);
    public bool showVisualization = true;
    
    [Header("Boundaries")]
    public Vector3 center = Vector3.zero;
    public Vector3 size = new Vector3(10, 0, 10);
    
    private GameObject visualizationObj;
    
    void OnEnable()
    {
        if (showVisualization)
        {
            CreateVisualization();
        }
    }
    
    void OnDisable()
    {
        DestroyVisualization();
    }
    
    void CreateVisualization()
    {
        DestroyVisualization();
        
        visualizationObj = new GameObject("NavMesh Visualization");
        visualizationObj.transform.SetParent(transform);
        
        // Calculate grid dimensions
        int xCount = Mathf.CeilToInt(size.x / gridSize);
        int zCount = Mathf.CeilToInt(size.z / gridSize);
        
        // Create a single combined mesh
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[xCount * zCount * 4]; // 4 vertices per quad
        int[] triangles = new int[xCount * zCount * 6]; // 6 indices per quad (2 triangles)
        Color[] colors = new Color[vertices.Length];
        
        int vertexIndex = 0;
        int triangleIndex = 0;
        
        // Generate grid quads
        for (int x = 0; x < xCount; x++)
        {
            for (int z = 0; z < zCount; z++)
            {
                // Calculate position
                float xPos = center.x - size.x/2 + x * gridSize;
                float zPos = center.z - size.z/2 + z * gridSize;
                Vector3 pos = new Vector3(xPos, center.y, zPos);
                
                // Check if on NavMesh
                NavMeshHit hit;
                bool onNavMesh = NavMesh.SamplePosition(pos, out hit, gridSize/2, NavMesh.AllAreas);
                
                if (onNavMesh)
                {
                    // Create quad vertices
                    vertices[vertexIndex] = new Vector3(xPos, hit.position.y + visualizationHeight, zPos);
                    vertices[vertexIndex+1] = new Vector3(xPos + gridSize, hit.position.y + visualizationHeight, zPos);
                    vertices[vertexIndex+2] = new Vector3(xPos + gridSize, hit.position.y + visualizationHeight, zPos + gridSize);
                    vertices[vertexIndex+3] = new Vector3(xPos, hit.position.y + visualizationHeight, zPos + gridSize);
                    
                    // Set color
                    colors[vertexIndex] = validColor;
                    colors[vertexIndex+1] = validColor;
                    colors[vertexIndex+2] = validColor;
                    colors[vertexIndex+3] = validColor;
                    
                    // Create triangles
                    triangles[triangleIndex] = vertexIndex;
                    triangles[triangleIndex+1] = vertexIndex+1;
                    triangles[triangleIndex+2] = vertexIndex+2;
                    triangles[triangleIndex+3] = vertexIndex;
                    triangles[triangleIndex+4] = vertexIndex+2;
                    triangles[triangleIndex+5] = vertexIndex+3;
                    
                    triangleIndex += 6;
                    vertexIndex += 4;
                }
            }
        }
        
        // Resize arrays if needed
        if (vertexIndex < vertices.Length)
        {
            System.Array.Resize(ref vertices, vertexIndex);
            System.Array.Resize(ref colors, vertexIndex);
            System.Array.Resize(ref triangles, triangleIndex);
        }
        
        // Set up mesh
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        
        // Add mesh components
        MeshFilter meshFilter = visualizationObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        
        MeshRenderer meshRenderer = visualizationObj.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Transparent/Diffuse"));
    }
    
    void DestroyVisualization()
    {
        if (visualizationObj != null)
        {
            DestroyImmediate(visualizationObj);
            visualizationObj = null;
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && showVisualization)
        {
            CreateVisualization();
        }
        else if (Application.isPlaying && !showVisualization)
        {
            DestroyVisualization();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw bounding box in editor
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
} 