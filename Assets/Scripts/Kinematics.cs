using System;
using UnityEngine;

public class RobotKinematics : MonoBehaviour
{
    [Header("Wheel Components")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider rearLeftWheel;
    public WheelCollider rearRightWheel;
    
    [Header("Wheel Transforms (for visualization)")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;
    
    [Header("Robot Parameters")]
    public float maxMotorTorque = 400f;
    public float maxSteeringAngle = 30f;
    public float wheelBase = 2.0f; // Distance between front and rear axles
    public float trackWidth = 1.6f; // Distance between left and right wheels
    
    [Header("Input Controls")]
    public float linearVelocity = 0f; // Forward/backward movement
    public float angularVelocity = 0f; // Rotation
    
    private Rigidbody robotRigidbody;
    
    void Start()
    {
        robotRigidbody = GetComponent<Rigidbody>();
        
        // Make sure the rigidbody is configured properly
        if (robotRigidbody != null)
        {
            robotRigidbody.centerOfMass = new Vector3(0, 0, -0.5f);
        }
    }
    
    void Update()
    {
        // Empty - input handling moved entirely to RobotController
    }
    
    void FixedUpdate()
    {
        // Apply differential drive kinematics
        ApplyDifferentialDriveModel();
        
        // Update wheel visuals
        UpdateWheelPoses();
    }
    
    void ApplyDifferentialDriveModel()
    {
        if (robotRigidbody == null || 
            frontLeftWheel == null || frontRightWheel == null || 
            rearLeftWheel == null || rearRightWheel == null)
        {
            Debug.LogWarning("Robot components not fully configured!");
            return;
        }
        
        // Calculate individual wheel speeds based on differential drive kinematics
        // Keep the correct turning behavior
        float leftWheelSpeed = linearVelocity + (angularVelocity * trackWidth / 2);
        float rightWheelSpeed = linearVelocity - (angularVelocity * trackWidth / 2);
        
        // Apply motor torques to the wheels
        frontLeftWheel.motorTorque = leftWheelSpeed;
        rearLeftWheel.motorTorque = leftWheelSpeed;
        frontRightWheel.motorTorque = rightWheelSpeed;
        rearRightWheel.motorTorque = rightWheelSpeed;
        
        // Apply steering - use the direct angular velocity for correct visual steering
        float steeringAngle = angularVelocity;
        frontLeftWheel.steerAngle = steeringAngle;
        frontRightWheel.steerAngle = steeringAngle;
    }
    
    void UpdateWheelPoses()
    {
        UpdateWheelPose(frontLeftWheel, frontLeftTransform);
        UpdateWheelPose(frontRightWheel, frontRightTransform);
        UpdateWheelPose(rearLeftWheel, rearLeftTransform);
        UpdateWheelPose(rearRightWheel, rearRightTransform);
    }
    
    void UpdateWheelPose(WheelCollider collider, Transform transform)
    {
        if (collider == null || transform == null)
            return;
            
        // Get wheel pose data from the physics simulation
        Vector3 position;
        Quaternion rotation;
        collider.GetWorldPose(out position, out rotation);
        
        // Apply to visual transform
        transform.position = position;
        transform.rotation = rotation;
    }
    
    // Public methods for external control (can be used by other scripts like path planning)
    public void SetMotion(float linear, float angular)
    {
        linearVelocity = Mathf.Clamp(linear, -maxMotorTorque, maxMotorTorque);
        angularVelocity = Mathf.Clamp(angular, -maxSteeringAngle, maxSteeringAngle);
    }
    
    public void Brake()
    {
        // Apply brakes to all wheels
        frontLeftWheel.brakeTorque = maxMotorTorque;
        frontRightWheel.brakeTorque = maxMotorTorque;
        rearLeftWheel.brakeTorque = maxMotorTorque;
        rearRightWheel.brakeTorque = maxMotorTorque;
        
        // Reset velocities
        linearVelocity = 0f;
        angularVelocity = 0f;
    }
    
    public void ReleaseBrake()
    {
        // Release brakes from all wheels
        frontLeftWheel.brakeTorque = 0f;
        frontRightWheel.brakeTorque = 0f;
        rearLeftWheel.brakeTorque = 0f;
        rearRightWheel.brakeTorque = 0f;
    }
    
    // Helper method to get the current robot velocity
    public Vector3 GetRobotVelocity()
    {
        if (robotRigidbody != null)
            return robotRigidbody.velocity;
        return Vector3.zero;
    }
    
    // Helper method to get the current robot angular velocity
    public Vector3 GetRobotAngularVelocity()
    {
        if (robotRigidbody != null)
            return robotRigidbody.angularVelocity;
        return Vector3.zero;
    }
}
