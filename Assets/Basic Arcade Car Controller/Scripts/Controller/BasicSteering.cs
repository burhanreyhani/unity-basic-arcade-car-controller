using UnityEngine;

public class BasicSteering : MonoBehaviour
{
    IInputProvider inputProvider;
    Rigidbody rb;

    [SerializeField] GameObject[] steeringWheels;
    [Tooltip("This is needed for ackerman steering calculations")]
    [SerializeField] GameObject[] allWheels;
    [Tooltip("This number must be at least 1 less than the number of rear wheels.")]
    [SerializeField] int frontWheelCount = 2;
    [SerializeField] float maxSpeedForMinSpeedAngle = 140f;
    [SerializeField] int steeringSpeed = 10;
    [SerializeField] float maxSteerAngle = 30f;
    [SerializeField] float minSteerAngle = 10f;

    [Header("Counter-Steering Settings")]
    [SerializeField] float maxSlipAngleDeg = 30f;
    [SerializeField] float counterSteerResponse = 1.5f;
    [SerializeField] float minSlipForCounter = 5f;
    [SerializeField] float counterSteerStrength = 15f;

    GameObject[] rearWheels;
    GameObject[] frontWheels;

    GameObject[] leftSteering;
    GameObject[] rightSteering;

    Quaternion[] leftInitialRotations;
    Quaternion[] rightInitialRotations;

    int wheelCount;
    int rearCount;
    int steerWheelsHalf;

    public float currentSteerAngle { get; private set; }

    float wheelBase;
    float trackWidth;

    float slipRadAngle;
    float slipAngle;

    void Awake()

    {
        inputProvider = GetComponentInChildren<IInputProvider>();

        CalculateWheelCount();
        SteeringWheelPos();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (frontWheelCount >= wheelCount)
        {
            Debug.Log("Front wheel number is wrong.");
        }

        wheelBase = CalculateWheelBase();
        trackWidth = CalculateTrackWidth();
    }

    void FixedUpdate()
    {
        float steering = inputProvider.Steering;
        float targetSteerAngle = CalculateMaxSteerAngle(steering);

        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle + CounterSteering(), Time.fixedDeltaTime * steeringSpeed);

        CalculateSlip();
        ApplySteer(currentSteerAngle);
    }

    void ApplySteer(float angle)
    {
        if (Mathf.Abs(angle) < 0.01f)
        {
            ResetWheelRotations();
            return;
        }

        float outerAngle = ApplyAckerman(angle);

        float rightAngle = angle > 0 ? angle : outerAngle;
        float leftAngle = angle < 0 ? angle : outerAngle;

        for (int i = 0; i < rightSteering.Length; i++)
        {
            rightSteering[i].transform.localRotation = rightInitialRotations[i] * Quaternion.Euler(0f, rightAngle, 0f);
        }

        for (int i = 0; i < leftSteering.Length; i++)
        {
            leftSteering[i].transform.localRotation = leftInitialRotations[i] * Quaternion.Euler(0f, leftAngle, 0f);
        }

        //Debug.Log("Outer: " + outerAngle);
        //Debug.Log("Inner: " + angle);
    }

    void ResetWheelRotations()
    {
        for (int i = 0; i < rightSteering.Length; i++)
            rightSteering[i].transform.localRotation = rightInitialRotations[i];

        for (int i = 0; i < leftSteering.Length; i++)
            leftSteering[i].transform.localRotation = leftInitialRotations[i];
    }

    float CalculateMaxSteerAngle(float steer)
    {
        float steeringSpeedFactor = Mathf.InverseLerp(0f, maxSpeedForMinSpeedAngle, rb.linearVelocity.magnitude * 3.6f); // TODO: Hızı tek bir yerden etkili bir şekilde nası çekcem ya?
        return Mathf.Lerp(maxSteerAngle, minSteerAngle, steeringSpeedFactor) * steer;
    }

    float ApplyAckerman(float angle)
    {
        if (Mathf.Abs(angle) < 0.01f) return 0f;

            float absInner = Mathf.Abs(angle);
            float innerRad = absInner * Mathf.Deg2Rad;

            float denominator = (wheelBase / Mathf.Tan(innerRad)) + trackWidth;
            float outerRad = Mathf.Atan(wheelBase / denominator);

            return outerRad * Mathf.Rad2Deg * Mathf.Sign(angle);
    }

    void CalculateSlip()
    {
        slipRadAngle = maxSlipAngleDeg * Mathf.Deg2Rad;
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        slipAngle = Mathf.Atan2(localVel.x, Mathf.Abs(localVel.z));

    }

    float CounterSteering()
    {
        float minSlipValue = minSlipForCounter * Mathf.Deg2Rad;

        float minCounterSteerSpeed = 15f;
        if (rb.linearVelocity.magnitude * 3.6f < minCounterSteerSpeed || Mathf.Abs(slipAngle) < minSlipValue) // TODO: No gorunded check yet.
            return 0f;

        float normalizedSlip = Mathf.Clamp(slipAngle / slipRadAngle, -1f, 1f);
        float counterSteer = Mathf.Sign(normalizedSlip) * Mathf.Pow(Mathf.Abs(normalizedSlip), counterSteerResponse);

        return counterSteer * counterSteerStrength;
    }

    float CalculateTrackWidth()
    {
        if (leftSteering.Length == 0 || rightSteering.Length == 0) return 0f;

        Vector3 leftPos = leftSteering[0].transform.position;
        Vector3 rightPos = rightSteering[0].transform.position;
        
        return Vector3.Distance(leftPos, rightPos);
    }

    float CalculateWheelBase()
    {
        Vector3 frontAxleCenter = Vector3.zero;
        Vector3 rearAxleCenter = Vector3.zero;

        for (int i = 0; i < frontWheelCount; i++)
            frontAxleCenter += frontWheels[i].transform.position;

        for(int i = 0; i < rearCount; i++)
            rearAxleCenter += rearWheels[i].transform.position;

        frontAxleCenter /= frontWheelCount;
        rearAxleCenter /= rearCount;

        return Vector3.Distance(frontAxleCenter, rearAxleCenter);
    }

    void CalculateWheelCount()
    {
        wheelCount = allWheels.Length;
        rearCount = wheelCount - frontWheelCount;

        frontWheels = new GameObject[frontWheelCount];
        rearWheels = new GameObject[rearCount];

        for (int i = 0; i < frontWheelCount; i++)
            frontWheels[i] = allWheels[i];

        for (int i = 0; i < rearCount; i++)
            rearWheels[i] = allWheels[i + frontWheelCount];
    }

    void SteeringWheelPos()
    {
        steerWheelsHalf = steeringWheels.Length / 2;
        rightSteering = new GameObject[steerWheelsHalf];
        leftSteering = new GameObject[steerWheelsHalf];

        rightInitialRotations = new Quaternion[steerWheelsHalf];
        leftInitialRotations = new Quaternion[steerWheelsHalf];

        for (int i = 0; i < steerWheelsHalf; i++)
        {
            leftSteering[i] = steeringWheels[i * 2];
            rightSteering[i] = steeringWheels[i * 2 + 1];

            leftInitialRotations[i] = leftSteering[i].transform.localRotation;
            rightInitialRotations[i] = rightSteering[i].transform.localRotation;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || leftSteering == null || rightSteering == null) return;

        Vector3 leftCrossDir = currentSteerAngle > 0 ? leftSteering[0].transform.right : -leftSteering[0].transform.right;
        Vector3 rightCrossDir = currentSteerAngle > 0 ? rightSteering[0].transform.right : -rightSteering[0].transform.right;

        Debug.DrawRay(leftSteering[0].transform.position, leftCrossDir * 15f, Color.yellow);
        Debug.DrawRay(rightSteering[0].transform.position, rightCrossDir * 15f, Color.yellow);

        Vector3 rearAxleCenter = (rearWheels[0].transform.position + rearWheels[1].transform.position) * 0.5f;
        Debug.DrawRay(rearAxleCenter - transform.right * 10f, transform.right * 20f, Color.cyan);
    }
}
