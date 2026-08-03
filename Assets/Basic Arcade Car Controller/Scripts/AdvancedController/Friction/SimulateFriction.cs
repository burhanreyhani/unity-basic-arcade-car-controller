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

        if (!grounded) timer = 0f;
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
            SleepTimer();

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

        Vector3 pointVel = rb.GetPointVelocity(transform.position);

        Vector3 latSlipDir = transform.right;
        Vector3 longSlipDir = transform.forward;

        Vector3 latSurfaceDir = Vector3.ProjectOnPlane(latSlipDir, normalHit).normalized;
        Vector3 longSurfaceDir = Vector3.ProjectOnPlane(longSlipDir, normalHit).normalized;

        float latSpeed = Vector3.Dot(pointVel, latSurfaceDir);
        float longSpeed = Vector3.Dot(pointVel, longSurfaceDir);

        float maxLatForce = (latSpeed * rb.mass) / Time.fixedDeltaTime;
        float maxLongForce = (longSpeed * rb.mass) / Time.fixedDeltaTime;

        float totalSlip = Mathf.Sqrt(latSpeed * latSpeed + longSpeed * longSpeed);
        float latRatio = totalSlip > 0 ? Mathf.Abs(latSpeed) / totalSlip : 0;
        float longRatio = totalSlip > 0 ? Mathf.Abs(longSpeed) / totalSlip : 0;

        float allocatedLatFriction = friction * latRatio;
        float allocatedLongFriction = friction * longRatio;

        float finalLatForceMagnitude = Mathf.Min(allocatedLatFriction, Mathf.Abs(maxLatForce)) * -Mathf.Sign(latSpeed);
        float finalLongForceMagnitude = Mathf.Min(allocatedLongFriction, Mathf.Abs(maxLongForce)) * -Mathf.Sign(longSpeed);

        Vector3 gripLat = latSurfaceDir * finalLatForceMagnitude;
        Vector3 gripLong = longSurfaceDir * finalLongForceMagnitude;

        rb.AddForceAtPosition(gripLong + gripLat, transform.position, ForceMode.Force);

        Debug.DrawLine(transform.position, transform.position + gripLong, Color.blue);
        Debug.DrawLine(transform.position, transform.position + gripLat, Color.red);
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
