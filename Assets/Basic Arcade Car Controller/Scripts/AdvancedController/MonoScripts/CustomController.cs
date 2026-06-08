using System;
using SuspensionData;
using UnityEngine;

public class CustomController : MonoBehaviour
{
    [HideInInspector] Rigidbody rb;

    InputMap inputs;

    [Header("Wheel Referances")]
    [SerializeField] Transform frontLeftWheel;
    [SerializeField] Transform frontRightWheel;
    [SerializeField] Transform rearLeftWheel;
    [SerializeField] Transform rearRightWheel;

    [Header("Suspension Referances")]
    [SerializeField] Transform frontLeftSuspension;
    [SerializeField] Transform frontRightSuspension;
    [SerializeField] Transform rearLeftSuspension;
    [SerializeField] Transform rearRightSuspension;

    Suspension fls;
    Suspension frs;
    Suspension rls;
    Suspension rrs;

    bool flGrounded;
    bool frGrounded;
    bool rlGrounded;
    bool rrGrounded;

    Vector3 flHitNormal;
    Vector3 frHitNormal;
    Vector3 rlHitNormal;
    Vector3 rrHitNormal;

    Vector3 flContact;
    Vector3 frContact;
    Vector3 rlContact;
    Vector3 rrContact;

    SuspensionInput frontSuspensionInfo;
    SuspensionInput rearSuspensionInfo;

    [Header("Tire Config")]
    [SerializeField] float frontWheelRadius = 0.4f;
    [SerializeField] float rearWheelRadius = 0.4f;

    [Header("Front Suspension Config")]
    [SerializeField] float frontRestLength = 0.5f;
    [SerializeField] float frontSpringStiffness = 12500f;
    [SerializeField] float frontDamping = 1250f;
    [SerializeField] float frontAntiRollStiffness = 5000f;
    [SerializeField] float frontWheelMass = 20f;

    [Header("Rear Suspension Config")]
    [SerializeField] float rearRestLength = 0.5f;
    [SerializeField] float rearSpringStiffness = 12500f;
    [SerializeField] float rearDamping = 1250f;
    [SerializeField] float rearAntiRollStiffness = 5000f;
    [SerializeField] float rearWheelMass = 20f;

    [Header("Spring Config")]
    [Tooltip("This value is same for rear and front suspensions.")]
    [SerializeField] float springTravel = 0.25f;

    [Header("Suspension Layer Config")]
    [SerializeField] LayerMask Drivable;

    [Header("Grip Config")]
    [SerializeField] float rollingResistanceCo = 0.02f;
    [SerializeField] float latMu = 0.5f;
    [SerializeField] float longMu = 0.8f;

    bool isSleepingManual = false;

    [Header("Adjust Timers")]
    [SerializeField] float time = 2f;
    [SerializeField] float groundedTime = 0.5f;

    [SerializeField] float timer;
    [SerializeField] float groundedTimer;

    float torque = 7500f;
    [SerializeField] float rlxLong = 0.7f;

    void Awake()
    {
        inputs = new InputMap();

        BuildSuspensions();

        fls = new Suspension(frontSuspensionInfo);
        frs = new Suspension(frontSuspensionInfo);
        rls = new Suspension(rearSuspensionInfo);
        rrs = new Suspension(rearSuspensionInfo);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        inputs.Enable();
    }

    void OnDisable()
    {
        inputs.Disable();
    }

    void BuildSuspensions()
    {
        frontSuspensionInfo = new SuspensionInput
        {
            restLength = frontRestLength,
            springStiffness = frontSpringStiffness,
            damping = frontDamping,
            antiRollStiffness = frontAntiRollStiffness,
            wheelMass = frontWheelMass
        };

        rearSuspensionInfo = new SuspensionInput
        {
            restLength = rearRestLength,
            springStiffness = rearSpringStiffness,
            damping = rearDamping,
            antiRollStiffness = rearAntiRollStiffness,
            wheelMass = rearWheelMass
        };
    }

    void FixedUpdate()
    {
        float throttle = inputs.Drive.Throttle.ReadValue<float>();
        float steer = inputs.Drive.Steer.ReadValue<float>();

        ApplyThrottle(throttle, rlContact, rlGrounded);
        ApplyThrottle(throttle, rrContact, rrGrounded);

        Steering(steer);

        UpdateSuspensions();

        //SleepTimer();
        //AwakeRB();
    }

    void ApplyThrottle(float throttle, Vector3 contactPoint, bool grounded)
    {
        if (throttle > 0.1f && grounded)
        {
            rb.AddForceAtPosition(transform.forward * torque, contactPoint, ForceMode.Force);
        }
    }

    void Steering(float steer)
    {
        rb.AddTorque(Vector3.up * steer * 10000, ForceMode.Force);
    }

    void SimulateGrip(Suspension suspension, Vector3 contactPoint, Vector3 normalHit)
    {
        Vector3 wheelVel = rb.GetPointVelocity(contactPoint);
        
        Vector3 gravityForce = rb.mass * Physics.gravity;
        Vector3 slopeForce = Vector3.ProjectOnPlane(gravityForce, normalHit);

        float maxStaticFric = latMu * suspension.normalForce;

        int totalWheelCount = 4;

        float stopThreshold = 0.3f;
        if (rb.linearVelocity.magnitude < stopThreshold)
        {
            Vector3 distributedSlopeForce = slopeForce / totalWheelCount;

            if (slopeForce.magnitude <= maxStaticFric)
            {
                rb.linearVelocity = Vector3.zero;

                rb.AddForce(-distributedSlopeForce, ForceMode.Force);
                return;
            }
        }
        rb.angularVelocity = Vector3.zero;

        Vector3 localVel = transform.InverseTransformDirection(wheelVel);

        float latFriction = latMu * suspension.normalForce;
        float longFriction = longMu * suspension.normalForce;

        Vector3 latVelocity = transform.TransformDirection(new Vector3(localVel.x, 0, 0));
        Vector3 longVelocity = transform.TransformDirection(new Vector3(0, 0, localVel.z));
        Vector3 longSurfaceVelocity = Vector3.ProjectOnPlane(longVelocity, normalHit);
        Vector3 latSurfaceVelocity = Vector3.ProjectOnPlane(latVelocity, normalHit);

        float latForceMag = Mathf.Min(latFriction * Time.fixedDeltaTime, rb.mass * latSurfaceVelocity.magnitude);
        Vector3 latGrip  = latSurfaceVelocity.magnitude  > 0.0001f ? -latSurfaceVelocity * latForceMag : Vector3.zero;

        Debug.DrawRay(contactPoint, latGrip, Color.blue);

        float longForceMag = Mathf.Min(longFriction * Time.fixedDeltaTime, rb.mass * longSurfaceVelocity.magnitude);
        Vector3 longGrip = longSurfaceVelocity.magnitude > 0.0001f ? -longSurfaceVelocity * longForceMag : Vector3.zero;

        Debug.DrawRay(contactPoint, longGrip, Color.darkRed);

        float rollingResistance = rollingResistanceCo * suspension.normalForce;
        Vector3 rollingForce = -wheelVel.normalized * Mathf.Min(rollingResistance * Time.fixedDeltaTime, rb.mass * wheelVel.magnitude);;

        rb.AddForceAtPosition(longGrip + latGrip + rollingForce, contactPoint, ForceMode.Impulse);
    }

    void UpdateSuspensions()
    {

        float flHit = CastSuspensionRay(frontLeftSuspension, frontRestLength + springTravel, frontWheelRadius, out flGrounded, out flContact, out flHitNormal);
        float frHit = CastSuspensionRay(frontRightSuspension, frontRestLength + springTravel, frontWheelRadius, out frGrounded, out frContact, out frHitNormal);
        float rlHit = CastSuspensionRay(rearLeftSuspension, rearRestLength + springTravel, rearWheelRadius, out rlGrounded, out rlContact, out rlHitNormal);
        float rrHit = CastSuspensionRay(rearRightSuspension, rearRestLength + springTravel, rearWheelRadius, out rrGrounded, out rrContact, out rrHitNormal);

        if (rb.IsSleeping()) return;

        float frontLeftVelocity = Vector3.Dot(rb.GetPointVelocity(frontLeftSuspension.position), frontLeftSuspension.up);
        float frontRightVelocity = Vector3.Dot(rb.GetPointVelocity(frontRightSuspension.position), frontRightSuspension.up);
        float rearLeftVelocity = Vector3.Dot(rb.GetPointVelocity(rearLeftSuspension.position), rearLeftSuspension.up);
        float rearRightVelocity = Vector3.Dot(rb.GetPointVelocity(rearRightSuspension.position), rearRightSuspension.up);

        fls.UpdateSuspension(flHit, frontLeftVelocity, springTravel);
        frs.UpdateSuspension(frHit, frontRightVelocity, springTravel);
        rls.UpdateSuspension(rlHit, rearLeftVelocity, springTravel);
        rrs.UpdateSuspension(rrHit, rearRightVelocity, springTravel);

        float frontAntiRoll = Suspension.AntiRollForce(fls, frs, frontAntiRollStiffness);
        float rearAntiRoll  = Suspension.AntiRollForce(rls, rrs, rearAntiRollStiffness);

        if (flGrounded)
        {
            rb.AddForceAtPosition(frontLeftSuspension.up * (fls.normalForce + frontAntiRoll), frontLeftSuspension.position);            
            SimulateGrip(fls, flContact, flHitNormal);
        }

        if (frGrounded)
        {
            rb.AddForceAtPosition(frontRightSuspension.up * (frs.normalForce - frontAntiRoll), frontRightSuspension.position);
            SimulateGrip(frs, frContact, frHitNormal);
        }

        if (rlGrounded)
        {
            rb.AddForceAtPosition(rearLeftSuspension.up * (rls.normalForce + rearAntiRoll),  rearLeftSuspension.position);
            SimulateGrip(rls, rlContact, rlHitNormal);
        }

        if (rrGrounded)
        {
            rb.AddForceAtPosition(rearRightSuspension.up * (rrs.normalForce - rearAntiRoll),  rearRightSuspension.position);
            SimulateGrip(rrs, rrContact, rrHitNormal);
        }

        WheelPosition(frontLeftWheel, frontLeftSuspension, fls);
        WheelPosition(frontRightWheel, frontRightSuspension, frs);
        WheelPosition(rearLeftWheel, rearLeftSuspension, rls);
        WheelPosition(rearRightWheel, rearRightSuspension, rrs);
    }

    float CastSuspensionRay(Transform suspensionTransform, float maxLength, float wheelRad, out bool isGrounded, out Vector3 contactPoint, out Vector3 contactNormal)
    {
        Vector3 origin = suspensionTransform.position - suspensionTransform.up * wheelRad;

        if (Physics.Raycast(origin, -suspensionTransform.up, out RaycastHit hit, maxLength, Drivable))
        {
            Debug.DrawRay(origin, -suspensionTransform.up * hit.distance, Color.green);
            isGrounded = true;
            contactPoint = hit.point;
            contactNormal = hit.normal;

            return hit.distance;
        }
        else
        {
            Debug.DrawRay(origin, -suspensionTransform.up * maxLength, Color.red);
            isGrounded = false;

            contactPoint = Vector3.zero;
            contactNormal = Vector3.up;
            return maxLength;
        }
    }

    void WheelPosition(Transform wheelPos, Transform suspension, Suspension susp)
    {
        wheelPos.transform.position = suspension.position - suspension.up * susp.currentLength;
    }

    void SleepTimer()
    {
        bool wheelGrounded = IsWheelGrounded();

        if (wheelGrounded && rb.linearVelocity.magnitude <= 0.1f && !isSleepingManual)
        {
            timer += Time.fixedDeltaTime;
            
            if (timer >= time && !isSleepingManual)
            {
                rb.angularVelocity = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
                isSleepingManual = true;
                timer = 0f;
                rb.Sleep();
            }
        }
    }

    void AwakeRB()
    {
        bool wheelGrounded = IsWheelGrounded();

        if (!wheelGrounded || rb.linearVelocity.magnitude > 0.1f)
        {
            rb.WakeUp();
            isSleepingManual = false;
            timer = 0f;
        }
        Debug.Log("isSleepingManual: " + isSleepingManual);
    }

    bool IsWheelGrounded()
    {
        bool[] wheelsGrounded = { flGrounded, frGrounded, rlGrounded, rrGrounded };

        foreach (var wheel in wheelsGrounded)
        {
            if (!wheel)
            {
                return false;
            }
        }
        return true;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(flContact, 0.1f);
        Gizmos.DrawSphere(frContact, 0.1f);
        Gizmos.DrawSphere(rlContact, 0.1f);
        Gizmos.DrawSphere(rrContact, 0.1f);
    }
}
