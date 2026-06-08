using System;
using UnityEngine;

public class SimulateFriction : MonoBehaviour
{
    Rigidbody rb;

    [Tooltip("Object not moving.")]
    [SerializeField] float muStatic = 1.2f;

    [Tooltip("Object moving.")]
    [SerializeField] float muKinetic = 0.5f;

    [SerializeField] float vCritiCal = 100f; // Max speed limit for min friction (muKinetic)
    [SerializeField] float time = 2f;
    [SerializeField] float groundedTime = 0.5f;
    [SerializeField] float rayLength = 0.5f;

    [SerializeField] float timer;
    [SerializeField] float groundedTimer;

    Vector3 normalHit;
    Vector3 hitPoint; // Belki lazım olur.

    float normalForce;
    float friction;

    bool grounded;
    bool isSleepingManual;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        AwakeRB();
        ApplyGrip();
    }

    void ApplyGrip()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, -transform.up, out hit, rayLength))
        {
            float cosTheta = Vector3.Dot(hit.normal, Vector3.up);
            normalForce = rb.mass * Physics.gravity.magnitude * cosTheta;

            grounded = true;
            groundedTimer = 0f;

            normalHit = hit.normal;
            hitPoint = hit.point;

            CalculateFriction();
            ApplyForce();
            //SleepTimer();

            Debug.DrawRay(transform.position, -transform.up * rayLength, Color.green);

            return;
        }

        groundedTimer += Time.fixedDeltaTime;

        if (groundedTimer >= groundedTime)
        {
            grounded = false;
            groundedTimer = groundedTime;    
        }

        isSleepingManual = false;

        Debug.DrawRay(transform.position, -transform.up * rayLength, Color.red);
    }

    void CalculateFriction()
    {
        float speedFactor = Mathf.Clamp01(rb.linearVelocity.magnitude / vCritiCal);
        float dynamicMu = Mathf.Lerp(muStatic, muKinetic, speedFactor);

        friction = dynamicMu * normalForce;
    }

    void ApplyForce()
    {
        if (!grounded) return;
    
        /*
        Vector3 pointVel = rb.GetPointVelocity(transform.position);
        Vector3 localVel = transform.InverseTransformDirection(pointVel);
        
        Vector3 latSlip = transform.TransformDirection(new Vector3(localVel.x, 0, 0));
        Vector3 longSlip = transform.TransformDirection(new Vector3(0, 0, localVel.z));

        Vector3 latSurfaceVelocity = Vector3.ProjectOnPlane(latSlip, normalHit);
        Vector3 longSurfaceVelocity = Vector3.ProjectOnPlane(longSlip, normalHit);

        Vector3 gripLat = -latSurfaceVelocity * friction;
        Vector3 gripLong = -longSurfaceVelocity * friction;

        rb.AddForceAtPosition(gripLat + gripLong, transform.position, ForceMode.Force);

        Debug.DrawRay(transform.position - (transform.up * 0.5f), gripLat * 0.5f, Color.red);
        Debug.DrawRay(transform.position - (transform.up * 0.5f), gripLong * 0.5f, Color.blue);

        /*
        Vector3 right = transform.right;
        Vector3 forward = transform.forward;

        Vector3 lateralVelocity = Vector3.Project(pointVel, right);
        Vector3 forwardVelocity = Vector3.Project(pointVel, forward);
        Vector3 slip = (forwardVelocity + lateralVelocity) / 2;

        float lateralFriction = Vector3.Project(right, slip).magnitude * normalForce / Physics.gravity.magnitude / Time.fixedDeltaTime * muStatic;
        rb.AddForceAtPosition(-Vector3.Project(slip, lateralVelocity).normalized * lateralFriction, hitPoint);
        */

        Vector3 gravityForce = rb.mass * Physics.gravity;
        Vector3 slopeForce = Vector3.ProjectOnPlane(gravityForce, normalHit);

        float maxStaticFric = muStatic * normalForce;

        float stopThreshold = 0.01f;
        if (rb.linearVelocity.magnitude < stopThreshold)
        {
            if (slopeForce.magnitude <= maxStaticFric)
            {
                rb.linearVelocity = Vector3.zero;

                rb.AddForce(-slopeForce, ForceMode.Force);
                return;
            }
        }

        Vector3 pointVel = rb.GetPointVelocity(transform.position);
        Vector3 localVel = transform.InverseTransformDirection(pointVel);

        Vector3 latSlip = transform.TransformDirection(new Vector3(localVel.x, 0, 0));
        Vector3 longSlip = transform.TransformDirection(new Vector3(0, 0, localVel.z));

        Vector3 latSurfaceVelocity = Vector3.ProjectOnPlane(latSlip, normalHit);
        Vector3 longSurfaceVelocity = Vector3.ProjectOnPlane(longSlip, normalHit);

        Vector3 gripLat = -latSurfaceVelocity.normalized * friction;
        Vector3 gripLong = -longSurfaceVelocity.normalized * friction;

        rb.AddForceAtPosition(gripLong, transform.position, ForceMode.Force);
    }

    void SleepTimer()
    {
        if (grounded && rb.linearVelocity.magnitude <= 0.1f && !isSleepingManual)
        {
            timer += Time.fixedDeltaTime;
            
            if (timer >= time && !isSleepingManual)
            {
                timer = 0f;
                isSleepingManual = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
        }
    }

    void AwakeRB()
    {
        if (rb.IsSleeping() && (!grounded || rb.linearVelocity.magnitude >= 0.2f))
        {
            isSleepingManual = false;
            rb.WakeUp();
            timer = 0f;
        }
    }
}
