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
    private bool isAvoiding = false;
    private float targetSteerAngle = 0f;

    /// <summary>
    /// Applies steering to the front wheels based on the target point
    /// </summary>
    public void ApplySteer() {
        if (isAvoiding) return;
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
            // Front center sensor
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength)){
                if (hit.collider.CompareTag("Obstacles")){
                    Debug.DrawLine(sensorStartPos, hit.point);
                    isAvoiding = true;
                    if (hit.normal.x < 0){
                        avoidMultiplier = 1;
                    } 
                    else {
                        avoidMultiplier = -1;
                    }
                }
            }
        }

        if (isAvoiding){
            targetSteerAngle = maxSteeringAngle * avoidMultiplier;
        }     
    }

    public void LerpToSteerAngle(){
        float lerpFactor = Mathf.Clamp01(Time.deltaTime * turnSpeed * (1 + turnResponsiveness * 5));
        
        frontLeftWheel.steerAngle = Mathf.Lerp(frontLeftWheel.steerAngle, targetSteerAngle, lerpFactor);
        frontRightWheel.steerAngle = Mathf.Lerp(frontRightWheel.steerAngle, targetSteerAngle, lerpFactor);
    }
}
