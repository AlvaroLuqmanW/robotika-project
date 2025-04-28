using UnityEngine;
using System.Collections.Generic;

public class TrilaterationLocalization : MonoBehaviour
{
    [Header("Landmark Settings")]
    public List<Transform> landmarks = new List<Transform>();
    public float measurementNoise = 0.1f; // Standard deviation of distance measurement noise

    [Header("Debug Settings")]
    public bool showDebugInfo = true;
    public Color debugColor = Color.yellow;
    public float debugSphereRadius = 0.2f;

    private Vector3 estimatedPosition;
    private List<float> measuredDistances = new List<float>();
    private bool isInitialized = false;

    void Start()
    {
        if (landmarks.Count < 3)
        {
            Debug.LogError("Trilateration requires at least 3 landmarks!");
            enabled = false;
            return;
        }

        estimatedPosition = transform.position;
        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return;

        // Measure distances to landmarks
        measuredDistances.Clear();
        foreach (var landmark in landmarks)
        {
            float distance = Vector3.Distance(transform.position, landmark.position);
            // Add noise to simulate real-world measurement
            distance += Random.Range(-measurementNoise, measurementNoise);
            measuredDistances.Add(distance);
        }

        // Perform trilateration
        estimatedPosition = CalculatePosition();

        // Update debug visualization
        if (showDebugInfo)
        {
            Debug.DrawLine(transform.position, estimatedPosition, debugColor);
            // Draw a cross at the estimated position
            Debug.DrawRay(estimatedPosition, Vector3.up * debugSphereRadius, debugColor);
            Debug.DrawRay(estimatedPosition, Vector3.down * debugSphereRadius, debugColor);
            Debug.DrawRay(estimatedPosition, Vector3.left * debugSphereRadius, debugColor);
            Debug.DrawRay(estimatedPosition, Vector3.right * debugSphereRadius, debugColor);
            Debug.DrawRay(estimatedPosition, Vector3.forward * debugSphereRadius, debugColor);
            Debug.DrawRay(estimatedPosition, Vector3.back * debugSphereRadius, debugColor);
        }
    }

    private Vector3 CalculatePosition()
    {
        if (landmarks.Count < 3) return transform.position;

        // Use the first three landmarks for basic trilateration
        Vector3 p1 = landmarks[0].position;
        Vector3 p2 = landmarks[1].position;
        Vector3 p3 = landmarks[2].position;

        float r1 = measuredDistances[0];
        float r2 = measuredDistances[1];
        float r3 = measuredDistances[2];

        // Calculate the unit vectors
        Vector3 ex = (p2 - p1).normalized;
        float i = Vector3.Dot(ex, p3 - p1);
        Vector3 ey = (p3 - p1 - i * ex).normalized;
        Vector3 ez = Vector3.Cross(ex, ey);

        // Calculate the distances between landmarks
        float d = Vector3.Distance(p1, p2);
        float j = Vector3.Dot(ey, p3 - p1);

        // Calculate the coordinates
        float x = (r1 * r1 - r2 * r2 + d * d) / (2 * d);
        float y = (r1 * r1 - r3 * r3 + i * i + j * j) / (2 * j) - (i / j) * x;

        // Calculate z coordinate
        float zSquared = r1 * r1 - x * x - y * y;
        float z = zSquared > 0 ? Mathf.Sqrt(zSquared) : 0;

        // Convert back to world coordinates
        Vector3 result = p1 + x * ex + y * ey + z * ez;

        // If we have more than 3 landmarks, use least squares to improve accuracy
        if (landmarks.Count > 3)
        {
            result = RefinePositionWithLeastSquares(result);
        }

        return result;
    }

    private Vector3 RefinePositionWithLeastSquares(Vector3 initialGuess)
    {
        Vector3 currentPosition = initialGuess;
        int maxIterations = 10;
        float tolerance = 0.001f;

        for (int iteration = 0; iteration < maxIterations; iteration++)
        {
            // Calculate Jacobian matrix and residual vector
            Matrix4x4 jacobian = new Matrix4x4();
            Vector4 residual = Vector4.zero;

            for (int i = 0; i < landmarks.Count; i++)
            {
                Vector3 landmark = landmarks[i].position;
                float measuredDistance = measuredDistances[i];
                float calculatedDistance = Vector3.Distance(currentPosition, landmark);

                // Calculate partial derivatives
                Vector3 partialDerivatives = (currentPosition - landmark) / calculatedDistance;

                // Update Jacobian
                jacobian.SetRow(i, new Vector4(partialDerivatives.x, partialDerivatives.y, partialDerivatives.z, 0));

                // Update residual
                residual[i] = measuredDistance - calculatedDistance;
            }

            // Solve for position update
            Vector4 update = jacobian.inverse * residual;
            currentPosition += new Vector3(update.x, update.y, update.z);

            // Check convergence
            if (update.magnitude < tolerance)
                break;
        }

        return currentPosition;
    }

    public Vector3 GetEstimatedPosition()
    {
        return estimatedPosition;
    }

    void OnDrawGizmos()
    {
        if (!showDebugInfo || !isInitialized) return;

        Gizmos.color = debugColor;
        Gizmos.DrawSphere(estimatedPosition, debugSphereRadius);

        // Draw lines to landmarks
        for (int i = 0; i < landmarks.Count; i++)
        {
            if (landmarks[i] != null)
            {
                Gizmos.DrawLine(estimatedPosition, landmarks[i].position);
            }
        }
    }
} 