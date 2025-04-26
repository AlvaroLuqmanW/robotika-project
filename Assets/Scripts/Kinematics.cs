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
    
    [Header("Robot Parameters")]
    public float maxSteeringAngle = 40f;
    public float motorTorque = 10f;
    public float brakeTorque = 100f;

    [Header("Passed Parameters")]
    [Tooltip("Target point to steer towards - set by PathFinding component")]
    public Vector3 currentPathPoint;
    [Tooltip("Current distance to target - set by PathFinding component")]
    public float distanceToTarget = float.MaxValue;
    [Tooltip("Distance at which to start slowing down")]
    public float slowingDistance = 3.0f;

    /// <summary>
    /// Applies steering to the front wheels based on the target point
    /// </summary>
    public void ApplySteer() {
        // Calculate steering based on path point
        Vector3 relativeVector = transform.InverseTransformPoint(currentPathPoint);
        float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteeringAngle;
        
        // Apply steering to wheels
        frontLeftWheel.steerAngle = newSteer;
        frontRightWheel.steerAngle = newSteer;
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
        
        // Apply zero brake when moving
        frontLeftWheel.brakeTorque = 0f;
        frontRightWheel.brakeTorque = 0f;
    }
    
    /// <summary>
    /// Stops the robot by applying brake torque and zeroing motor torque
    /// </summary>
    public void StopRobot() {
        // Stop motor torque
        frontLeftWheel.motorTorque = 0f;
        frontRightWheel.motorTorque = 0f;
        
        // Apply brakes to stop the robot
        frontLeftWheel.brakeTorque = brakeTorque;
        frontRightWheel.brakeTorque = brakeTorque;
    }
}
