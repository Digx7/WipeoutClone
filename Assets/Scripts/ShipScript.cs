﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipScript : MonoBehaviour {
    
    //gotta set this in-editor to access the ship's transform/camera
    [SerializeField] private Transform ship;
    [SerializeField] private Camera cam;

    //controls how the ship handles
    [SerializeField] private float gravityScalar = 19.8f;
    [SerializeField] private float desiredHeight = 3.0f;
    [SerializeField] private float maxForce = 20.0f;
    [SerializeField] private float castDistance = 30.0f;
    [SerializeField] private float speed = 75.0f;
    [SerializeField] private float steerSpeed = 5.0f;

    private Vector3 newGravity = new Vector3(0.0f, -1.0f, 0.0f);

    //leaning stuff while moving
    private float prevRotate = 0.0f;
    private float prevLean = 0.0f;
    private float rotatePercentage = 0.0f;

    private Rigidbody rb;

    // Use this for initialization
    void Start () {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float vel = rb.velocity.sqrMagnitude;
        vel /= (speed * speed);

        rb.AddForce(newGravity * rb.mass);

        float vert = Input.GetAxis("Vertical");
        if (vert > 1.0f) vert = 1.0f;
        if (vert < -1.0f) vert = -1.0f;

        float horz = Input.GetAxis("Horizontal");

        if (rotatePercentage < vert)
        {
            rotatePercentage += Time.deltaTime * 3.5f;
        }
        else if (rotatePercentage > vert)
        {
            rotatePercentage -= Time.deltaTime * 3.5f;
        }

        ship.RotateAroundLocal(ship.right, -prevRotate);

        Vector3 proj = ship.forward - (Vector3.Dot(ship.forward, -newGravity)) * -newGravity;
        Quaternion newRot = Quaternion.LookRotation(proj, -newGravity);
        ship.rotation = Quaternion.Lerp(ship.rotation, newRot, 1.0f * Time.deltaTime);

        float currentSpeed = Vector3.Dot(rb.velocity, ship.forward);

        prevRotate = (Mathf.Deg2Rad * rotatePercentage) * 10.0f;
        prevLean = (Mathf.Deg2Rad * horz) * (2.5f * (currentSpeed / speed) + 0.5f);
        ship.RotateAroundLocal(ship.right, prevRotate);
        ship.RotateAroundLocal(ship.forward, prevLean);

        Vector3 newPos = transform.position - (15.0f + vel * 12.0f) * ship.forward + 6.0f * ship.up - 1.0f * ship.right;
        Vector3 camVel = Vector3.zero;
        cam.transform.position = Vector3.SmoothDamp(cam.transform.position, newPos, ref camVel, 0.06f);

        Debug.Log(vel);

        Quaternion oldRot = cam.transform.rotation;
        newRot = Quaternion.LookRotation(ship.forward, ship.up);
        cam.transform.rotation = Quaternion.Lerp(oldRot, newRot, 5.0f * (1.0f + vel) * Time.deltaTime);
    }

    // Update is called once per frame
    void Update () {

        //inputs
        float horz = Input.GetAxis("Horizontal");
        float vert = Input.GetAxis("Vertical");
        float accel = Input.GetAxis("Acceleration");
        float drift = Input.GetAxis("Drift");

        RaycastHit hit;
        if(Physics.Raycast(transform.position, -ship.up, out hit, castDistance))
        {
            //Debug.DrawRay(transform.position, newGravity.normalized * castDistance, Color.blue);

            //adjust gravity to new surface
            newGravity = -hit.normal.normalized;
            newGravity *= gravityScalar;

            float currentUp = Vector3.Dot(rb.velocity, ship.up);

            float force = desiredHeight - hit.distance;

            if (hit.distance <= desiredHeight)
            {
                force *= (maxForce / desiredHeight);
                if (force < 0) force *= -1.0f;
                force += 1.0f;

                if (currentUp <= 0.0f)
                {
                    force *= 2.0f;
                }
                else
                {
                    force *= 0.5f;
                }
            }
            else
            {
                if (force > 0) force *= -1.0f;

                if(currentUp <= 0.0f)
                {
                    force = 0.0f;
                }
            }

            rb.AddForce(force * -newGravity * rb.mass);
        }
        else
        {
            //reset to defaults
            newGravity = new Vector3(0.0f, -1.0f, 0.0f);
            newGravity *= gravityScalar;
        }

        //apply the inputs
        ship.RotateAroundLocal(ship.up, horz * Time.deltaTime * steerSpeed);

        float desiredSpeed = speed * accel * 1.25f;
        float currentSpeed = Vector3.Dot(rb.velocity, ship.forward);
        float accelForce = (desiredSpeed - currentSpeed);
        rb.AddForce(ship.forward * accelForce * rb.mass);
        Debug.DrawRay(transform.position + ship.up * 1.0f, ship.forward * 5.0f, Color.red);

        //braking to prevent drifts
        desiredSpeed = 0.0f;
        currentSpeed = Vector3.Dot(rb.velocity, ship.right);
        accelForce = (desiredSpeed - currentSpeed);
        accelForce *= (1.0f - drift);
        rb.AddForce(ship.right * accelForce * rb.mass);

        Debug.DrawRay(transform.position, -ship.up * desiredHeight, Color.green);
        Debug.Log(newGravity);
    }
}
