using UnityEngine;

public class BasicSuspension : MonoBehaviour
{
    BasicGearBox basicGearBox;

    IInputProvider inputProvider;
    Rigidbody rb;

    [SerializeField] bool isItLeftSuspension;
    [SerializeField] Transform suspension;

    [Tooltip("Leave blank if there is no opposite suspension.")]
    [SerializeField] BasicSuspension oppositeSuspension;
    [SerializeField] GameObject wheelMesh;

    [Header("Suspension Layer Config")]
    [SerializeField] LayerMask Drivable;

    [Header("Suspension Config")]
    [SerializeField] float wheelRadius = 0.37f;
    [SerializeField] float restLength = 0.5f;
    [SerializeField] float springStiffness = 12500f;
    [SerializeField] float damping = 1250f;
    [SerializeField] float antiRollStiffness = 5000f;
    [SerializeField] float wheelMass = 20f; // TODO: Rightnow not used anywhere.
    [SerializeField] float springTravel = 0.25f;

    [SerializeField] float uLong = 1;
    [SerializeField] float uLat = 1;

    [SerializeField] float wheelInertia = 2f; // Test

    public float currentLength { get; private set; }
    public float normalForce { get; private set; }
    public float compressionRatio { get; private set; }
    public float antiRollForceVal { get; private set; }
    public bool grounded { get; private set; }

    Vector3 contactPoint;
    Vector3 contactNormal; // Belki lazım olur.

    Vector3 linearVelocityLocal;
    Vector3 angularVelocityLocal;
    Vector3 longitudinalDir;
    Vector3 lateralDir;

    Vector3 fZ;
    Vector3 fX;
    Vector3 fY;
    Vector3 simpleTireForce;

    float throttle;
    float slipAngle;

    float muX;
    float muY;

    float totalTorque;
    float wheelAngularVelocity;
    float slipSpeed;

    void Awake()
    {
        inputProvider = GetComponentInParent<IInputProvider>();
    }

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        basicGearBox = GetComponentInParent<BasicGearBox>();
    }

    void FixedUpdate()
    {
        throttle = inputProvider.Throttle;

        UpdateSuspension();
        UpdateWheelPos();
        
        if (grounded)
        {
            GetWheelMotionOnGround();
            CalculateLateralFriction();
            CalculateLongitudinalFriction();
            ApplyFrictionForce();
            //GetSimpleTireForce();
            //ApplySimpleTireForce();
        }
        else
        {
            ResetValues();
        }
    }

    void CalculateLateralFriction()
    {
        float slipAnglePeak = 8.0f;

        slipAngle = 0.0f;
        if (linearVelocityLocal.z != 0)
        {
            slipAngle = Mathf.Atan(-linearVelocityLocal.x / Mathf.Abs(linearVelocityLocal.z)) * Mathf.Rad2Deg;
        }

        muX = MapRangeClamped(Mathf.Abs(slipAngle), 0.0f, slipAnglePeak, 0.0f, 1.0f) * Mathf.Sign(slipAngle);
    }

    void CalculateLongitudinalFriction()
    {
        // TODO: This part will be refactored
        float wheelRPM = rb.linearVelocity.magnitude / wheelRadius * 60f / (2f * Mathf.PI);
        float distributeTorque = basicGearBox.ApplyTorque(wheelRPM) / 4;

        float driveTorque = throttle * distributeTorque;
        float frictionTorque = muY * Mathf.Max(fZ.y, 0.0f) * wheelRadius;

        totalTorque = driveTorque - frictionTorque;

        float wheelAngularAcceleration = totalTorque / wheelInertia;
        wheelAngularVelocity += wheelAngularAcceleration * Time.fixedDeltaTime;

        float slipSpeedPeak = 4.0f;
        slipSpeed = (wheelAngularVelocity * wheelRadius) - linearVelocityLocal.z;

        muY = MapRangeClamped(Mathf.Abs(slipSpeed), 0.0f, slipSpeedPeak, 0.0f, 1.0f) * Mathf.Sign(slipSpeed);
    }

    void ApplyFrictionForce()
    {
        float normalForce = Mathf.Max(fZ.y, 0.0f);

        fX = lateralDir * muX * normalForce;
        fY = longitudinalDir * muY * normalForce;
        rb.AddForceAtPosition(fX + fY, contactPoint);

        //Debug.Log("Fx: " + fX);
        //Debug.Log("Fy: " + fY);
    }
    
    float MapRangeClamped(float value, float inRangeA, float inRangeB, float outRangeA, float outRangeB) //Maps a value from one range to another
    {
        float result = Mathf.Lerp(outRangeA, outRangeB, Mathf.InverseLerp(inRangeA, inRangeB, value));
        return (result);
    }

    void UpdateSuspension()
    {
        float suspensionHit = CastSuspensionRay();
        float suspensionVelocity = Vector3.Dot(rb.GetPointVelocity(suspension.transform.position), suspension.transform.up);

        CalculateSuspension(suspensionHit, suspensionVelocity, springTravel);

        if (isItLeftSuspension)
            AntiRollForce();

        if (grounded)
        {
            if (oppositeSuspension != null)
            {
                if (isItLeftSuspension)
                    rb.AddForceAtPosition(suspension.transform.up * (normalForce + antiRollForceVal), suspension.transform.position);
                else
                    rb.AddForceAtPosition(suspension.transform.up * (normalForce - oppositeSuspension.antiRollForceVal), suspension.transform.position);
            }
            else
            {
                rb.AddForceAtPosition(suspension.transform.up * normalForce, suspension.transform.position);
            }
        }
    }

    float CastSuspensionRay()
    {
        Vector3 origin = suspension.transform.position - suspension.transform.up * wheelRadius;

        float maxLength = restLength + springTravel;

        if (Physics.Raycast(origin, -suspension.transform.up, out RaycastHit hit, maxLength, Drivable))
        {
            Debug.DrawRay(origin, -suspension.transform.up * hit.distance, Color.green);
            grounded = true;
            contactPoint = hit.point;
            contactNormal = hit.normal;

            return hit.distance;
        }
        else
        {
            Debug.DrawRay(origin, -suspension.transform.up * maxLength, Color.red);
            grounded = false;

            contactPoint = Vector3.one * float.NaN;
            contactNormal = Vector3.up;

            return maxLength;
        }
    }

    float CalculateSuspension(float raycastLength, float pointVelocity, float springTravel)
    {
        currentLength = raycastLength;

        float compression = (restLength - currentLength) / springTravel;
        compression = Mathf.Clamp(compression, 0f, 1f);
        compressionRatio = compression;

        float springForce = springStiffness * compression;
        float damperForce = damping * pointVelocity;

        normalForce = Mathf.Max(0f, springForce - damperForce);

        fZ = contactNormal.normalized * normalForce;

        return normalForce;
    }

    float AntiRollForce()
    {
        if (oppositeSuspension == null || !isItLeftSuspension || (!grounded && oppositeSuspension.grounded)) return 0;

        float travelDiff = compressionRatio - oppositeSuspension.compressionRatio;
        antiRollForceVal = travelDiff * antiRollStiffness;
        return antiRollForceVal;
    }

    void UpdateWheelPos()
    {
        wheelMesh.transform.position = suspension.transform.position - suspension.up * currentLength;
    }

    void GetWheelMotionOnGround()
    {
        linearVelocityLocal = transform.InverseTransformDirection(rb.GetPointVelocity(contactPoint));
        angularVelocityLocal = linearVelocityLocal / wheelRadius;

        longitudinalDir = Vector3.ProjectOnPlane(transform.forward, contactNormal).normalized;
        lateralDir = Vector3.ProjectOnPlane(transform.right, contactNormal).normalized;
    }

    void ResetValues()
    {
        slipAngle = slipSpeed = 0.0f; //Set wheel slip to zero
        muX = muY = 0.0f; //Set friction coefficients to zero
        fX = fY = fZ = Vector3.zero; //Set forces to zero
    }
    /*
    void GetSimpleTireForce()
    {
        Vector3 longitudinalTireForce = (throttle * uLong) * Mathf.Max(0.0f, fZ.y) * longitudinalDir; // F_long = u * N * -longDir
        Vector3 lateralTireForce = (Mathf.Clamp(linearVelocityLocal.x, -1.0f, 1.0f) * uLat) * Mathf.Max(0.0f, fZ.y) * - lateralDir; //F_lat = u * N * -latDir
        simpleTireForce = longitudinalTireForce + lateralTireForce;
    }

    void ApplySimpleTireForce()
    {
        rb.AddForceAtPosition(simpleTireForce, contactPoint);
    }
    */
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(contactPoint, 0.1f);
    }
}
