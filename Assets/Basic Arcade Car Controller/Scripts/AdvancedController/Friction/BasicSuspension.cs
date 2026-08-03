using UnityEngine;

public class BasicSuspension : MonoBehaviour
{
    Rigidbody rb;

    [SerializeField] bool isItLeftSuspension;
    [SerializeField] Transform suspension;

    [Tooltip("Skip if there is no opposite suspension.")]
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

    public float currentLength { get; private set; }
    public float normalForce { get; private set; }
    public float compressionRatio { get; private set; }
    public float antiRollForceVal { get; private set; }
    public bool grounded { get; private set; }

    Vector3 contactPoint;
    Vector3 contactNormal; // Belki lazım olur.

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
    }

    void FixedUpdate()
    {
        UpdateSuspension();
        UpdateWheelPos();
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

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(contactPoint, 0.1f);
    }
}
