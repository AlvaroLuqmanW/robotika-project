using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Handles the physical movement of the robot, including steering and motor control.
/// Designed to work with RobotPathfinding script which provides navigation data.
/// </summary>
public class RobotKinematics : MonoBehaviour
{
    [Header("Wheel Components")]
    public WheelCollider frontLeftWheel;
    public WheelCollider frontRightWheel;
    public WheelCollider backRightWheel;
    public WheelCollider backLeftWheel;
    
    [Header("Robot Parameters")]
    public float maxSteeringAngle = 40f;
    public float motorTorque = 10f;
    public float brakeTorque = 100f;
    public float turnSpeed = 20f;
    [Tooltip("Higher values (0-1) make turning more instantaneous")]
    public float turnResponsiveness = 0.8f;

    [Header("Passed Parameters")]
    [Tooltip("Target point to steer towards - set by PathFinding component")]
    public Vector3 currentPathPoint;
    [Tooltip("Current distance to target - set by PathFinding component")]
    public float distanceToTarget = float.MaxValue;
    [Tooltip("Distance at which to start slowing down")]
    public float slowingDistance = 3.0f;

    [Header("Robot Sensor Parameters")]
    public float sensorLength = 5f;
    public Vector3 frontSensorPosition = new Vector3(0, -0.05f, 0);
    public float frontSideSensorPosition = -0.3f;
    public float forntSideSensorAngle = 30;
    [Tooltip("Width of the center sensor in meters")]
    public float centerSensorWidth = 0.5f;
    [Tooltip("Number of raycasts to use for center sensor")]
    public int centerSensorRays = 3;
    private bool isAvoiding = false;
    private float targetSteerAngle = 0f;
    
    [Header("Collision Recovery")]
    public float minVelocityThreshold = 0.1f;
    public float reverseTorque = 8f;
    private bool isReversing = false;
    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("Rigidbody component was missing and has been added automatically.");
        }
    }

    void Update()
    {
        // No longer need the timer-based approach
        // We'll continue reversing until sensors don't detect obstacles
    }

    /// <summary>
    /// Applies steering to the front wheels based on the target point
    /// </summary>
    public void ApplySteer() {
        if (isAvoiding || isReversing) return;
        // Calculate steering based on path point
        Vector3 relativeVector = transform.InverseTransformPoint(currentPathPoint);
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteeringAngle;
        
        // Apply steering to wheels
        targetSteerAngle = newSteer;
    }
    
    /// <summary>
    /// Applies motor torque to the wheels with speed reduction based on distance to target
    /// </summary>
    public void Drive() {
        if (isReversing) {
            ReverseRobot();
            return;
        }
        
        // Apply speed reduction when approaching target
        float speedFactor = 1.0f;
        
        if (distanceToTarget < slowingDistance) {
            // Calculate a factor between 0 and 1 based on distance
            speedFactor = Mathf.Clamp01(distanceToTarget / slowingDistance);
            
            // Apply a curve to make deceleration smoother
            speedFactor = speedFactor * speedFactor;
        }
        
        // Apply torque with speed factor
        frontLeftWheel.motorTorque = motorTorque * speedFactor;
        frontRightWheel.motorTorque = motorTorque * speedFactor;
        backLeftWheel.motorTorque = motorTorque * speedFactor;
        backRightWheel.motorTorque = motorTorque * speedFactor;
        
        // Apply zero brake when moving
        frontLeftWheel.brakeTorque = 0f;
        frontRightWheel.brakeTorque = 0f;
        backLeftWheel.brakeTorque = 0f;
        backRightWheel.brakeTorque = 0f;
    }
    
    /// <summary>
    /// Applies reverse torque to move the robot backward
    /// </summary>
    public void ReverseRobot() {
        // Apply negative torque to move backward
        frontLeftWheel.motorTorque = -reverseTorque;
        frontRightWheel.motorTorque = -reverseTorque;
        backLeftWheel.motorTorque = -reverseTorque;
        backRightWheel.motorTorque = -reverseTorque;
        
        // Apply zero brake when moving
        frontLeftWheel.brakeTorque = 0f;
        frontRightWheel.brakeTorque = 0f;
        backLeftWheel.brakeTorque = 0f;
        backRightWheel.brakeTorque = 0f;
    }
    
    /// <summary>
    /// Stops the robot by applying brake torque and zeroing motor torque
    /// </summary>
    public void StopRobot() {
        // Stop motor torque
        frontLeftWheel.motorTorque = 0f;
        frontRightWheel.motorTorque = 0f;
        backLeftWheel.motorTorque = 0f;
        backRightWheel.motorTorque = 0f;
        
        // Apply brakes to stop the robot
        frontLeftWheel.brakeTorque = brakeTorque;
        frontRightWheel.brakeTorque = brakeTorque;
        backLeftWheel.brakeTorque = brakeTorque;
        backLeftWheel.brakeTorque = brakeTorque;
    }

    /// <summary>
    /// Raycast sensors mainly to avoid obstacles
    /// </summary>
    public void Sensors(){
        RaycastHit hit;
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.z;
        sensorStartPos += transform.up * frontSensorPosition.y;
        float avoidMultiplier = 0;
        isAvoiding = false;
        bool centerSensorHit = false;

        // Front right sensor
        sensorStartPos -= transform.right * frontSideSensorPosition;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength)){
            if (hit.collider.CompareTag("Obstacles")){
                Debug.DrawLine(sensorStartPos, hit.point);
                isAvoiding = true;
                avoidMultiplier += 1;
            }
        }
        
        // Front right angle sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-forntSideSensorAngle, transform.up) *transform.forward, out hit, sensorLength)){
            if (hit.collider.CompareTag("Obstacles")){
                Debug.DrawLine(sensorStartPos, hit.point);
                isAvoiding = true;
                avoidMultiplier += 0.5f;
            }
        }
        
        // Front left sensor
        sensorStartPos += transform.right * frontSideSensorPosition * 2;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength)){
            if (hit.collider.CompareTag("Obstacles")){
                Debug.DrawLine(sensorStartPos, hit.point);
                isAvoiding = true;
                avoidMultiplier -= 1f;
            }
        }
        
        // Front left angle sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(forntSideSensorAngle, transform.up) *transform.forward, out hit, sensorLength)){
            if (hit.collider.CompareTag("Obstacles")){
                Debug.DrawLine(sensorStartPos, hit.point);
                isAvoiding = true;
                avoidMultiplier -= 0.5f;
            }
        }

        if (avoidMultiplier == 0){
            // Front center sensor - multiple raycasts for width
            float raySpacing = centerSensorWidth / (centerSensorRays - 1);
            Vector3 centerStartPos = transform.position - transform.right * (centerSensorWidth * 0.5f);
            
            for (int i = 0; i < centerSensorRays; i++) {
                Vector3 rayPos = centerStartPos + transform.right * (raySpacing * i);
                if (Physics.Raycast(rayPos, transform.forward, out hit, sensorLength)) {
                    if (hit.collider.CompareTag("Obstacles")) {
                        Debug.DrawLine(rayPos, hit.point);
                        isAvoiding = true;
                        centerSensorHit = true;
                        
                        // Check if robot is stuck by checking velocity when center sensor detects obstacle
                        float currentSpeed = rb.velocity.magnitude;
                        
                        if (currentSpeed < minVelocityThreshold && !isReversing) {
                            // Robot appears to be stuck, start reversing
                            isReversing = true;
                            return;
                        }
                        
                        if (hit.normal.x < 0) {
                            avoidMultiplier = 1;
                        } else {
                            avoidMultiplier = -1;
                        }
                        break; // Exit loop after first hit
                    }
                }
            }
        }

        // Check if we should stop reversing when all sensors don't detect obstacles
        if (!isAvoiding && isReversing) {
            isReversing = false;
            StopRobot(); // Brief stop before resuming normal operation
        }

        if (isAvoiding && !isReversing){
            targetSteerAngle = maxSteeringAngle * avoidMultiplier;
        }     
    }

    public void LerpToSteerAngle(){
        float lerpFactor = Mathf.Clamp01(Time.deltaTime * turnSpeed * (1 + turnResponsiveness * 5));
        
        frontLeftWheel.steerAngle = Mathf.Lerp(frontLeftWheel.steerAngle, targetSteerAngle, lerpFactor);
        frontRightWheel.steerAngle = Mathf.Lerp(frontRightWheel.steerAngle, targetSteerAngle, lerpFactor);
    }
}
