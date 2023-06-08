using System;
using System.Collections;
using System.Collections.Generic;
using Collidable;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class CarController : MonoBehaviour
{

    public static UnityEvent<AbstractCollidableObject> onHitCollidable = new UnityEvent<AbstractCollidableObject>();
    
    [Header("Refs")]
    [SerializeField] private WheelCollider backLeft;
    [SerializeField] private WheelCollider backRight;
    [SerializeField] private WheelCollider frontLeft;
    [SerializeField] private WheelCollider frontRight;

    [SerializeField] private Transform backLeftTransform;
    [SerializeField] private Transform backRightTransform;
    [SerializeField] private Transform frontLeftTransform;
    [SerializeField] private Transform frontRightTransform;

    [SerializeField] private GameObject brakeLights;
    [SerializeField] private Rigidbody rb;

    [Space] [Header("Settings")]
    [SerializeField, Range(0, 400)] private float polloMultiplier = 200;
    [SerializeField, Range(0, 1)] private float boostMultiplier = .25f;
    [SerializeField, Range(0, 1500)] private float acceleration = 750f;
    [SerializeField, Range(0, 500)] private float breakingForce = 300f;
    [SerializeField, Range(0, 180)] private float maxTurnAngle = 15f;
    [SerializeField] private Vector3 carSpawn;

    private float currentAcceleration = 0f;
    private float currentBreakForce = 0f;
    private float currentTurnAngle = 0f;
    private Quaternion wheelShift;
    private float speed;

    private void Awake()
    {
        onHitCollidable.AddListener((AbstractCollidableObject collidable) => { PlayerData.eggCount += collidable.eggsWhenHit; });
    }
    
    private void Start()
    {
        transform.position = carSpawn;
        brakeLights.SetActive(false);
    }

    private void FixedUpdate()
    {
        UpdateCarForces();
    }

    private void UpdateCarForces()
    {
        currentAcceleration = acceleration * Input.GetAxisRaw("Vertical");
        speed = rb.velocity.magnitude * 3.6f;

        backRight.motorTorque = currentAcceleration;
        backLeft.motorTorque = currentAcceleration;

        frontRight.brakeTorque = currentBreakForce;
        frontLeft.brakeTorque = currentBreakForce;
        backRight.brakeTorque = currentBreakForce;
        backLeft.brakeTorque = currentBreakForce;

        currentTurnAngle = maxTurnAngle * Input.GetAxisRaw("Horizontal");

        frontRight.steerAngle = currentTurnAngle;
        frontLeft.steerAngle = currentTurnAngle;

        UpdateWheel(frontRight, frontRightTransform, false);
        UpdateWheel(backRight, backRightTransform, false);

        UpdateWheel(frontLeft, frontLeftTransform, true);
        UpdateWheel(backLeft, backLeftTransform, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentBreakForce = breakingForce;
            brakeLights.SetActive(true);
        }
        else if (Input.GetKeyUp(KeyCode.Space))
        {
            currentBreakForce = 0f;
            brakeLights.SetActive(false);
        }
        
        if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.Delete))
            Respawn();

        UIManager.instance.UpdateSpeedText(speed);
    }

    void Respawn()
    {
        transform.position = carSpawn;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        rb.velocity = Vector3.zero;
    }

    void Boost()
    {
        rb.AddForce(transform.forward * boostMultiplier * 3.6f, ForceMode.VelocityChange);
    }

    void UpdateWheel(WheelCollider col, Transform trans, Boolean left)
    {
        col.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheelShift = Quaternion.Euler(0, left ? 270 : 90, 0);
        Quaternion newRotation = rotation * wheelShift;

        trans.position = position;
        trans.rotation = newRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.TryGetComponent<AbstractCollidableObject>(out AbstractCollidableObject collidableObject))
        {
            if (!collidableObject.onHitByPlayer())
                return;
            
            onHitCollidable.Invoke(collidableObject);

            if (collidableObject.boostWhenHit)
                Boost();
            
            Vector3 carVel = rb.velocity;
            collision.transform.GetComponent<Rigidbody>().AddForce(carVel * polloMultiplier 
                    + transform.up * (polloMultiplier/2*carVel.magnitude) );
        }
    }
}
