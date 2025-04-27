using System.Collections;
using UnityEngine;

public class CarWheel: MonoBehaviour {

    [Header("Car Wheel")]
    public WheelCollider targetWheel;

    private Vector3 wheelPosition = new Vector3();
    private Quaternion wheelRotation = new Quaternion();
    private float rotationAngle = 0f;
    private Quaternion initialRotation;
    private Vector3 rotationAxis = Vector3.right;

    private void Start()
    {
        initialRotation = transform.localRotation;
    }
    private void Update()
    {
        targetWheel.GetWorldPose(out wheelPosition, out wheelRotation);
        transform.position = wheelPosition;   
        rotationAngle += targetWheel.rpm * Time.deltaTime * 6.0f; // 6.0 = 360/60 to convert from RPM to degrees per second        
        transform.localRotation = initialRotation * Quaternion.AngleAxis(rotationAngle, rotationAxis);
    }
}
