using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BasicCarController : MonoBehaviour
{
    [HideInInspector] public WheelCollider[] allWheels;

    BasicEngine basicEngine;
    BasicGearBox basicGearBox;

    public WheelCollider[] driveWheels;
    public WheelCollider[] steeringWheels;

    WheelCollider[] rearWheels;

    [Header("Car Stats")]
    [SerializeField] AnimationCurve accelerationCurve;
    [SerializeField] float maxSpeed = 180f;
    [SerializeField] float motorPower = 1200f;

    [Header("Reverse Gear Settings")]
    [SerializeField] float maxReverseSpeed = 40f;
    [SerializeField] float reversePower = 500f;

    [Header("Brake Settings")]
    [SerializeField] float brakePower = 1000f;
    [SerializeField] float dragForceForBrakes = 5f;
    [SerializeField] float handBrakePower = 2500f;
    [SerializeField] float motorBrakePower = 5f;
    [SerializeField] float minSpeedForHandBrakeDrift = 30f;
    [SerializeField] float handBrakeDriftPower = 200f;

    [Header("Steering Settings")]
    [SerializeField] float steeringSpeed = 50f;
    [SerializeField] float maxSteeringAngle = 30f;
    [SerializeField] float minSteeringAtMaxSpeed = 10f;

    [Header("Counter-Steering Settings")]
    [SerializeField] float maxSlipAngleDeg = 30f;
    [SerializeField] float counterSteerResponse = 1.5f;
    [SerializeField] float minSlipForCounter = 5f;
    [SerializeField] float counterSteerStrength = 15f;

    [Header("Weight Distribution Settings")]
    [SerializeField] float xCOMValue = 0.5f;
    [SerializeField] float speedingZ = -0.05f;
    [SerializeField] float slowingZ = 0.1f;
    [SerializeField] float defaultY = 0.3f;

    public Controls carInputs { get; private set; }
    public float avgWheelRPM { get; private set; }
    Rigidbody carBody;

    float forwardSpeed;
    float speedFactor;
    float currentSteerAngle;
    float velocityZ;
    float slipRadAngle;
    float slipAngle;

    int wheelCount;
    int frontCount;
    int rearCount;

    bool wantsToGoForward;
    bool wantsToGoBackward;

    public float carSpeedKmh { get; private set; }

    void Awake()
    {
        carInputs = new Controls();
    }

    void Start()
    {
        carBody = GetComponent<Rigidbody>();
        allWheels = GetComponentsInChildren<WheelCollider>();
        basicEngine = GetComponent<BasicEngine>();
        basicGearBox = GetComponent<BasicGearBox>();

        FindHbWheels();
    }

    void OnEnable()
    {
        carInputs.Enable();
    }

    void OnDisable()
    {
        carInputs.Disable();
    }
    
    void FixedUpdate()
    {
        ResetWheelForces();

        float throttle = carInputs.Drive.Throttle.ReadValue<float>();
        float brakeReverse = carInputs.Drive.BrakeReverse.ReadValue<float>();
        float steer = carInputs.Drive.Steering.ReadValue<float>();
        float handBrake = carInputs.Drive.Handbrake.ReadValue<float>();

        carSpeedKmh = carBody.linearVelocity.magnitude * 3.6f;

        velocityZ = transform.InverseTransformDirection(carBody.linearVelocity).z;
        const float directionDeadZone = 0.2f;
        wantsToGoForward = throttle > 0.1f && velocityZ < -directionDeadZone;
        wantsToGoBackward = brakeReverse > 0.1f && velocityZ > directionDeadZone;

        forwardSpeed = Vector3.Dot(transform.forward, carBody.linearVelocity);
        speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed));

        float targetSteerAngle = CalculateSteerAngle(steer);
        currentSteerAngle = Mathf.MoveTowards(currentSteerAngle, targetSteerAngle + CounterSteering(), steeringSpeed * Time.fixedDeltaTime);

        Accelerate(throttle);
        Brake(brakeReverse);
        Reverse(brakeReverse, throttle);
        Steering(currentSteerAngle);
        HandBrake(handBrake, currentSteerAngle);
        MotorBrake(throttle, brakeReverse, handBrake);

        CalculateSlip();

        AdjustCOM();

        CalculateWheelRPM();
    }

    void Accelerate(float throttle)
    {
        float currentMotorTorque = Mathf.Lerp(motorPower, 0, speedFactor);
        float t = Mathf.InverseLerp(0, maxSpeed, carSpeedKmh);
        float accelMultiplier = Mathf.Clamp01(accelerationCurve.Evaluate(t));

        float torque = basicGearBox.ApplyTorque(avgWheelRPM) / 2;

        if (throttle > 0 && carSpeedKmh <= maxSpeed && !wantsToGoForward)
        {
            foreach (var driveWheel in driveWheels)
            {
                //driveWheel.motorTorque = currentMotorTorque * accelMultiplier;
                //driveWheel.motorTorque = throttle > 0.1 ? basicEngine.currentRPM : 0;
                //driveWheel.motorTorque = torque;
                carBody.AddForce(transform.forward * torque);
                driveWheel.motorTorque = 0.001f;
            }
        }
        else if (wantsToGoForward)
        {
            foreach (var brakeWheel in allWheels)
                brakeWheel.brakeTorque = brakePower;
            
            carBody.AddForce(transform.forward * dragForceForBrakes, ForceMode.Acceleration);
        }
    }

    void Brake(float brake)
    {
        if (brake > 0 && wantsToGoBackward)
        {
            foreach (var brakeWheel in allWheels)
                brakeWheel.brakeTorque = brakePower;

            carBody.AddForce(-transform.forward * dragForceForBrakes, ForceMode.Acceleration);
        }
    }

    void Reverse(float reverse, float throttle)
    {
        if (throttle < 0.1f && reverse > 0 && carSpeedKmh <= maxReverseSpeed && !wantsToGoBackward)
        {
            foreach (var driveWheel in driveWheels)
                driveWheel.motorTorque = -reversePower;
        }
    }

    float CalculateSteerAngle(float steer)
    {
        float steeringSpeedFactor = Mathf.InverseLerp(0f, maxSpeed, carSpeedKmh);
        return Mathf.Lerp(maxSteeringAngle, minSteeringAtMaxSpeed, steeringSpeedFactor) * steer;
    }

    void Steering(float angle)
    {  
        foreach (var steeringWheel in steeringWheels)
            steeringWheel.steerAngle = angle;
    }

    void FindHbWheels()
    {
        wheelCount = allWheels.Length;

        frontCount = Mathf.Min(2, wheelCount);
        rearCount = wheelCount - frontCount;

        rearWheels = new WheelCollider[rearCount];

        for (int i = 0; i < rearCount; i++)
            rearWheels[i] = allWheels[i + frontCount];
    }

    void HandBrake(float hb, float steer)
    {
        if (hb > 0 && IsGrounded())
        {
            foreach (var hbWheels in rearWheels)
                hbWheels.brakeTorque = handBrakePower;

            if (carSpeedKmh > minSpeedForHandBrakeDrift)
                carBody.AddTorque(Vector3.up * steer * handBrakeDriftPower, ForceMode.Force);
        }
    }

    public bool IsGrounded()
    {
        foreach (var wheel in rearWheels)
        {
            if (wheel.GetGroundHit(out WheelHit hit))
                return true;
        }

        return false;
    }

    void MotorBrake(float throttle, float brake, float hb)
    {
        if (brake == 0 && throttle == 0 && hb == 0)
        {
            foreach (var driveWheel in driveWheels)
                driveWheel.motorTorque = 0;

            foreach (var brakeWheel in allWheels)
                brakeWheel.brakeTorque = motorBrakePower;
        }
    }

    void CalculateSlip()
    {
        slipRadAngle = maxSlipAngleDeg * Mathf.Deg2Rad;
        Vector3 localVel = transform.InverseTransformDirection(carBody.linearVelocity);
        slipAngle = Mathf.Atan2(localVel.x, Mathf.Abs(localVel.z));
    }

    float CounterSteering()
    {
        float minSlipValue = minSlipForCounter * Mathf.Deg2Rad;

        float normalizedSlip = Mathf.Clamp(slipAngle / slipRadAngle, -1f, 1f);
        float counterSteer = Mathf.Sign(normalizedSlip) * Mathf.Pow(Mathf.Abs(normalizedSlip), counterSteerResponse);

        if (carSpeedKmh < 15f || Mathf.Abs(slipAngle) < minSlipValue || !IsGrounded()) return 0f;

        return counterSteer * counterSteerStrength;
    }

    void AdjustCOM()
    {
        Vector3 localVel = transform.InverseTransformDirection(carBody.linearVelocity);
        float slipAngle = Mathf.Atan2(localVel.x, Mathf.Abs(localVel.z));
        float normalizedSlip = Mathf.Clamp(slipAngle / (Mathf.PI / slipRadAngle), -1f, 1f);
        float comX = Mathf.Lerp(xCOMValue, -xCOMValue, (normalizedSlip + 1f) * 0.5f);

        float t = Mathf.InverseLerp(0f, maxSpeed, carSpeedKmh);
        float comZ = Mathf.Lerp(slowingZ, speedingZ, t);

        Vector3 newCOM = carBody.centerOfMass;
        newCOM.x = comX;
        newCOM.z = comZ;
        if (carSpeedKmh > 1f)
            carBody.centerOfMass = newCOM;
        else
            carBody.centerOfMass = new Vector3(0, defaultY, 0);
    }

    void CalculateWheelRPM() // TODO: Will change for test only.
    {
        float wheelRadius = driveWheels[0].radius;
        avgWheelRPM = carBody.linearVelocity.magnitude / wheelRadius * 60f / (2f * Mathf.PI);
        //avgWheelRPM = carBody.linearVelocity.magnitude;
        
        //avgWheelRPM = basicEngine.currentRPM / basicGearBox.TotalRatio();
        //Debug.Log("WheelRPM: " + avgWheelRPM);
    }

    void ResetWheelForces()
    {
        foreach (var w in driveWheels)
            w.motorTorque = 0;

        foreach (var w in allWheels)
            w.brakeTorque = 0;
    }

    public float GetCarMaxSpeed()
    {
        return maxSpeed;
    }
}
