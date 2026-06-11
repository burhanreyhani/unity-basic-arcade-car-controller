using UnityEngine;

public class BasicEngine : MonoBehaviour
{
    Rigidbody carBody;

    BasicCarController basicCarController;
    BasicGearBox basicGearBox;
    BasicDrivetrain basicDrivetrain;

    [Header("Engine Stats")]
    [SerializeField] AnimationCurve torqueCurve;
    [SerializeField] float engineTorque = 1500f;
    [SerializeField] float engineMaxRPM = 7000f;
    [SerializeField] float engineIdleRPM = 1000f;

    [Header("Engine Inertia Settings")]
    [SerializeField] float engineInertia = 10f;
    [SerializeField] float baseFriction = 0.7f;
    [SerializeField] float friction = 7f;

    [Header("Engine Jitter")]
    [SerializeField] float minJitter = -3;
    [SerializeField] float maxJitter = 4;

    [Header("Engine Start")]
    [SerializeField] float addTorqueValue = 20f;
    [SerializeField] float addRPMValue = 40f;
    [SerializeField] float startEngineTorque = 300f;
    [SerializeField] float stallRPM = 900f;

    bool isRunning = false;
    bool ignite = false;

    public float currentRPM { get; private set; }

    public float currentEngineTorque { get; private set; }

    void Start()
    {
        carBody = GetComponent<Rigidbody>();
        basicCarController = GetComponent<BasicCarController>();
        basicGearBox = GetComponent<BasicGearBox>();
        basicDrivetrain = GetComponent<BasicDrivetrain>();
    }

    void FixedUpdate()
    {
        float throttleVal = basicCarController.carInputs.Drive.Throttle.ReadValue<float>(); // TODO: Look for more efficient methot.

        float startEngine = basicGearBox.inputMap.Drive.Ignition.ReadValue<float>();
        float killEngine = basicGearBox.inputMap.Drive.KillEngine.ReadValue<float>();

        if (startEngine > 0.1f)
        {
            ignite = true;
        }
        else
        {
            ignite = false;
        }

        if (killEngine > 0.1f)
        {
            KillEngine();
        }

        EnegineAngularAcceleration(throttleVal);
    }

    void EnegineAngularAcceleration(float throttle)
    {
        if (!isRunning)
        {
            IgniteEngine();
            return;   
        }

        currentEngineTorque = CalculateTorque(throttle);
        float frictionTorque = baseFriction + friction * (currentRPM / engineMaxRPM);

        bool disconnected = basicGearBox.currentGear == 0 || basicGearBox.clutchInput >= 0.1f;
        float totalRatio = basicGearBox.TotalRatio();
        float reflectedInertia = (!disconnected && Mathf.Abs(totalRatio) > 0.0001f) ? carBody.mass * basicCarController.driveWheels[0].radius
        * basicCarController.driveWheels[0].radius / (totalRatio * totalRatio) : 0f; //  basicCarController.driveWheels[0].radius * (totalRatio * totalRatio)
        float drivetrainload = disconnected ? 0f : basicDrivetrain.DrivetrainLoad();
        float angularAcceleration = (currentEngineTorque - frictionTorque - drivetrainload) / (engineInertia + reflectedInertia + basicGearBox.GearboxInertia());

        float deltaRPM = angularAcceleration * (60 / (2f * Mathf.PI)) * Time.fixedDeltaTime;

        float scaleJitter = 0.5f;
        float jitterValue = Random.Range(minJitter, maxJitter) * scaleJitter;
        currentRPM += deltaRPM + jitterValue;
        
        if (basicGearBox.currentGear != 0 && basicGearBox.clutchVal < 0.1f && basicGearBox.GetJustShifted())
        {
            float targetRPM = basicGearBox.ApplyGearRatio(basicDrivetrain.avgDrivenWheelRPM) * 60f / (2f * Mathf.PI);
            float rpmSnapSpeed = 10f;
            currentRPM = Mathf.Lerp(currentRPM, targetRPM, Time.fixedDeltaTime * rpmSnapSpeed);
            
            float timeThreshold = 0.2f;
            if (Time.time - basicGearBox.lastShiftTime > timeThreshold)
                basicGearBox.SetJustShifted(false);
        }

        if (currentRPM < engineIdleRPM && throttle < 0.01f && disconnected)
        {
            currentRPM = engineIdleRPM;
        }
        else if (currentRPM < stallRPM)
        {
            isRunning = false;
        }

        if (currentRPM > engineMaxRPM)
        {
            currentRPM = engineMaxRPM;
        }
    }

    float CalculateTorque(float throttle)
    {
        float engineSpeedFactor = Mathf.InverseLerp(0, engineMaxRPM, currentRPM);
        float currentMotorTorque = Mathf.Lerp(engineTorque, 0, engineSpeedFactor);
        float t = Mathf.InverseLerp(0, engineMaxRPM, currentRPM);
        float accelMultiplier = Mathf.Clamp01(torqueCurve.Evaluate(t));

        float idleBoostThreshold = 200f;
        float idleFactor = Mathf.Clamp01(1f - (currentRPM - engineIdleRPM) / idleBoostThreshold);
        float multiplier = 0.1f;
        float idleTorque = currentRPM <= engineIdleRPM + idleBoostThreshold ? engineTorque * multiplier * idleFactor : 0f;

        float totalTorque = throttle * currentMotorTorque * accelMultiplier;
        return totalTorque + idleTorque;
    }

    void IgniteEngine()
    {
        if (isRunning) return;

        if (ignite && !isRunning)
        {
            currentEngineTorque += addTorqueValue;
        }
        else if (!ignite && !isRunning && Mathf.Abs(currentEngineTorque) >= 0)
        {
            currentEngineTorque -= addTorqueValue;
            currentRPM -= addRPMValue;

            if (currentEngineTorque < 0)
                currentEngineTorque = 0f;
            
            if (currentRPM < 0)
                currentRPM = 0;
        }

        if (currentEngineTorque >= startEngineTorque)
        {
            currentRPM += addRPMValue;

            if (currentRPM >= engineIdleRPM)
            {
                isRunning = true;
            }
        }
    }

    void KillEngine()
    {
        float killEngineThreshold = 200f;

        if (currentRPM > engineIdleRPM + killEngineThreshold)
        {
            return;
        }
        
        isRunning = false;
    }

    public float GetMaxRPM()
    {
        return engineMaxRPM;
    }

    public float GetIdleEngineRPM()
    {
        return engineIdleRPM;
    }
}