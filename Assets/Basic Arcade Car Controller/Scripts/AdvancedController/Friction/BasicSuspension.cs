using SuspensionData;
using UnityEngine;

public class BasicSuspension : MonoBehaviour
{
    Rigidbody rb;

    SuspensionInput frontSuspensionInfo;
    SuspensionInput rearSuspensionInfo;

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

    void Awake()
    {
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

    void FixedUpdate()
    {
        UpdateSuspensions();
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

    void UpdateSuspensions()
    {
        float flHit = CastSuspensionRay(frontLeftSuspension, frontRestLength + springTravel, frontWheelRadius, out flGrounded, out flContact, out flHitNormal);
        float frHit = CastSuspensionRay(frontRightSuspension, frontRestLength + springTravel, frontWheelRadius, out frGrounded, out frContact, out frHitNormal);
        float rlHit = CastSuspensionRay(rearLeftSuspension, rearRestLength + springTravel, rearWheelRadius, out rlGrounded, out rlContact, out rlHitNormal);
        float rrHit = CastSuspensionRay(rearRightSuspension, rearRestLength + springTravel, rearWheelRadius, out rrGrounded, out rrContact, out rrHitNormal);

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
        }

        if (frGrounded)
        {
            rb.AddForceAtPosition(frontRightSuspension.up * (frs.normalForce - frontAntiRoll), frontRightSuspension.position);
        }

        if (rlGrounded)
        {
            rb.AddForceAtPosition(rearLeftSuspension.up * (rls.normalForce + rearAntiRoll),  rearLeftSuspension.position);
        }

        if (rrGrounded)
        {
            rb.AddForceAtPosition(rearRightSuspension.up * (rrs.normalForce - rearAntiRoll),  rearRightSuspension.position);
        }
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
}
