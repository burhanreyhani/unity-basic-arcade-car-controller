using EngineData;
using UnityEngine;

public class AdvancedCarControllers : MonoBehaviour
{
    [HideInInspector] public WheelCollider[] allWheels;

    [SerializeField] WheelCollider[] driveWheels;
    [SerializeField] WheelCollider[] steeringWheels;

    WheelCollider[] rearWheels;

    [Header("Car Stats")]
    [SerializeField] float maxRefSpeed = 180f;

    [Header("Steering Settings")]
    [SerializeField] float steeringSpeed = 50f;
    [SerializeField] float maxSteeringAngle = 30f;
    [SerializeField] float minSteeringAtMaxSpeed = 10f;

    [Header("Reverse Gear Settings")]
    [SerializeField] float maxReverseSpeed = 40f;
    [SerializeField] float reversePower = 500f;

    [Header("Brake Settings")]
    [SerializeField] float brakePower = 1000f;
    [SerializeField] float dragForceForBrakes = 5f;
    [SerializeField] float handBrakePower = 2500f;
    [SerializeField] float minSpeedForHandBrakeDrift = 30f;
    [SerializeField] float handBrakeDriftPower = 200f;

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

    Engine engine;
    EngineInput engineInput;

    [Header("Engine Config")]
    [SerializeField] float maxEngineTorque = 1500f;
    [SerializeField] float engineInertiaVal = 4;
    [SerializeField] float motorBrakeVal = 1000f;
    [SerializeField] float baseFrictionVal = 5f;
    [SerializeField] float frictionVal = 8f;
    [SerializeField] float idleRPMVal = 1000f;
    [SerializeField] float maxRPMVal = 7000f;
    [SerializeField] float[] rpmPointsVal = { 1000, 2000, 3000, 4000, 5000, 6000, 7000 }; // TODO: Change this with animation curve.
    [SerializeField] float[] torquePointsVal = { 0.68f, 0.75f, 0.85f, 1.0f, 0.8f, 0.35f, 0.28f }; // TODO: Change this with animation curve.
    bool igniteEng = false;

    [Header("Thresholds")]
    [SerializeField] float addTorqueValue = 20f;
    [SerializeField] float addRPMValue = 40f;
    [SerializeField] float startEngineTorque = 300f;
    [SerializeField] float stallRPM = 900f;

    Gearbox gearbox;

    [Header("Gearbox Config")]
    [SerializeField] float gearboxMass = 25f;
    [SerializeField] float[] gearRatios = { 0f, 2.5f, 1.8f, 1.3f, 1.0f, 0.8f };
    [SerializeField] float reverseRatio = 0.8f;
    [SerializeField] float finalDrive = 3.5f;

    Clutch clutch;

    [Header("Clutch Config")]
    [SerializeField] float clutchTorqueMax = 100f;
    [SerializeField] float clutchStiffness = 50f;

    Drivetrain drivetrain;
    enum DifferentialType { Open, Locked, LSD }

    [Header("Drivetrain Config")]
    [SerializeField] Drivetrain.DrivetrainType driveType = Drivetrain.DrivetrainType.RWD;
    [SerializeField] DifferentialType diffType = DifferentialType.Open;
    [SerializeField] float drivetrainEfficiency = 0.95f;

    [Header("LSD Settings (Only applies when LSD selected)")]
    [SerializeField] float lockingTorque = 400f;
    [SerializeField] float lockThreshold = 60f;
    [SerializeField] float lockStrength = 0.5f;

    [HideInInspector] public float clutchVal { get; private set; }

    public InputMap InputMap;
    Rigidbody carBody;

    float currentSteerAngle;
    float velocityZ;
    float slipRadAngle;
    float slipAngle;

    int wheelCount;
    int frontCount;
    int rearCount;

    bool wantsToGoForward;
    bool wantsToGoBackward;

    [HideInInspector] public float carSpeedKmh;

    void Awake()
    {
        InputMap = new InputMap();

        BuildEngine();
        BuildGears();
        BuildDrivetrain();

        engine = new Engine(engineInput, gearbox, drivetrain, clutch);
        gearbox.SetEngine(engine);

        engine.SetAddTorqueValue(addTorqueValue);
        engine.SetStartTorque(startEngineTorque);
        engine.SetAddRPMValue(addRPMValue);
        engine.SetStallRPM(stallRPM);
    }

    void Start()
    {
        carBody = GetComponent<Rigidbody>();
        allWheels = GetComponentsInChildren<WheelCollider>();

        FindHbWheels();
    }

    void OnEnable()
    {
        InputMap.Enable();
    }

    void OnDisable()
    {
        InputMap.Disable();
    }

    void Update()
    {
        bool gearUp = InputMap.Drive.GearUp.WasPressedThisFrame();
        bool gearDown = InputMap.Drive.GearDown.WasPressedThisFrame();

        float currentRPMValue = engine.currentRPM;

        if (gearUp)
            gearbox.ShiftUp(6500f, currentRPMValue); // TODO: Parameters won't work bc it is manual right now.

        if (gearDown)
            gearbox.ShiftDown(1800f, currentRPMValue); // TODO: Parameters won't work bc it is manual right now.
    }

    void FixedUpdate()
    {
        ResetWheelForces();

        float throttle = InputMap.Drive.Throttle.ReadValue<float>();
        float brakeReverse = InputMap.Drive.Brake.ReadValue<float>();
        float steer = InputMap.Drive.Steer.ReadValue<float>();
        float handBrake = InputMap.Drive.Handbrake.ReadValue<float>();

        float ignite = InputMap.Drive.Ignition.ReadValue<float>();
        float killEngine = InputMap.Drive.KillEngine.ReadValue<float>();
        clutchVal = InputMap.Drive.Clutch.ReadValue<float>();
        clutch.SetClutchInput(clutchVal); // TODO: This might change in the future.

        IgniteEngine(ignite, killEngine);

        carSpeedKmh = carBody.linearVelocity.magnitude * 3.6f;

        velocityZ = transform.InverseTransformDirection(carBody.linearVelocity).z;
        const float directionDeadZone = 0.2f;
        wantsToGoForward = throttle > 0.1f && velocityZ < -directionDeadZone;
        wantsToGoBackward = brakeReverse > 0.1f && velocityZ > directionDeadZone;

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

        Debug.Log("RPM: " + engine.currentRPM);
        Debug.Log("Speed: " + carSpeedKmh);
        Debug.Log("Gear:" + gearbox.currentGear);
    }

    void IgniteEngine(float ignite, float killEngine)
    {
        if (ignite > 0.1)
        {
            igniteEng = true;
        }
        else
        {
            igniteEng = false;
        }

        if (killEngine > 0.1f)
        {
            engine.KillEngine();
        }

        engine.SetIgnite(igniteEng);
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

    void Accelerate(float throttle)
    {
        drivetrain.CalculateDownstreamTrq(allWheels[0].rpm, allWheels[1].rpm, allWheels[2].rpm, allWheels[3].rpm);
        float drivenRadius = drivetrain.DrivenWheelRadius(allWheels[0].radius, allWheels[2].radius);
        engine.UpdateEngine(Time.fixedDeltaTime, throttle, carBody.mass, drivenRadius);
        drivetrain.DistributeTorque(allWheels[0].rpm, allWheels[1].rpm, allWheels[2].rpm, allWheels[3].rpm, out float lfTorque, out float rfTorque, out float rlTorque, out float rrTorque);
        float[] wTorque = { lfTorque, rfTorque, rlTorque, rrTorque };

        if (throttle > 0 && carSpeedKmh <= maxRefSpeed && !wantsToGoForward)
        {
            for (int i = 0; i < driveWheels.Length; i++)
            {
                driveWheels[i].motorTorque = wTorque[i];
                Debug.Log("Tork: " + wTorque[i]);
            }
        }
        else if (wantsToGoForward)
        {
            foreach (var brakeWheel in allWheels)
                brakeWheel.brakeTorque = brakePower;
            
            carBody.AddForce(transform.forward * dragForceForBrakes, ForceMode.Acceleration);
        }
    }

    float CalculateSteerAngle(float steer)
    {
        float steeringSpeedFactor = Mathf.InverseLerp(0f, maxRefSpeed, carSpeedKmh);
        return Mathf.Lerp(maxSteeringAngle, minSteeringAtMaxSpeed, steeringSpeedFactor) * steer;
    }

    void Steering(float angle)
    {  
        foreach (var steeringWheel in steeringWheels)
                steeringWheel.steerAngle = angle;
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
                brakeWheel.brakeTorque = motorBrakeVal;
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

        float t = Mathf.InverseLerp(0f, maxRefSpeed, carSpeedKmh);
        float comZ = Mathf.Lerp(slowingZ, speedingZ, t);

        Vector3 newCOM = carBody.centerOfMass;
        newCOM.x = comX;
        newCOM.z = comZ;
        if (carSpeedKmh > 1f)
            carBody.centerOfMass = newCOM;
        else
            carBody.centerOfMass = new Vector3(0, defaultY, 0);
    }

    void ResetWheelForces()
    {
        foreach (var w in driveWheels)
            w.motorTorque = 0;

        foreach (var w in allWheels)
            w.brakeTorque = 0;
    }

    void BuildEngine()
    {
        engineInput = new EngineInput(
            maxTorque: maxEngineTorque,
            engineInertia: engineInertiaVal,
            motorBrake: motorBrakeVal,
            baseFriction: baseFrictionVal,
            friction: frictionVal,
            idleRPM: idleRPMVal,
            maxRPM: maxRPMVal,
            rpmPoints: rpmPointsVal,
            torquePoints: torquePointsVal
        );
    }

        void BuildGears()
    {
        clutch = new Clutch();
        clutch.SetClutchStiffness(clutchStiffness);
        clutch.SetClutchTorqueMax(clutchTorqueMax);

        gearbox = new Gearbox(null, clutch);
        gearbox.SetGear(0);
        gearbox.SetFinalDrive(finalDrive);
        gearbox.SetGearRatios(gearRatios);
        gearbox.SetReverseRatio(reverseRatio);
        gearbox.SetGearboxRotMass(gearboxMass);
    }

    void BuildDrivetrain()
    {
        drivetrain = new Drivetrain(gearbox, driveType);

        IDifferential diff = diffType switch
        {
            DifferentialType.Open => new OpenDifferential(),
            DifferentialType.Locked => new LockedDifferential(),
            DifferentialType.LSD => new LimitedSlipDifferential(lockingTorque, lockThreshold, lockStrength),
            _ => new OpenDifferential()
        };

        if (driveType == Drivetrain.DrivetrainType.AWD)
        {
            drivetrain.SetDifferential(new OpenDifferential(), Drivetrain.DiffPosition.Center);
            drivetrain.SetDifferential(diff, Drivetrain.DiffPosition.Front);
            drivetrain.SetDifferential(diff, Drivetrain.DiffPosition.Rear);
        }
        else
        {
            Drivetrain.DiffPosition pos = driveType == Drivetrain.DrivetrainType.FWD
                ? Drivetrain.DiffPosition.Front
                : Drivetrain.DiffPosition.Rear;

            drivetrain.SetDifferential(diff, pos);
        }

        drivetrain.SetEfficiency(drivetrainEfficiency);
    }
}
