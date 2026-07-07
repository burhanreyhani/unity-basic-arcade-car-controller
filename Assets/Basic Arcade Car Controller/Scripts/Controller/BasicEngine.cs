using UnityEngine;

public class BasicEngine : MonoBehaviour
{
    Rigidbody carBody;

    BasicCarController basicCarController;
    BasicGearBox basicGearBox;

    [Header("Engine Stats")]
    [SerializeField] AnimationCurve torqueCurve;
    [SerializeField] float engineTorque = 300f;
    [SerializeField] float engineMaxRPM = 7000f;
    [SerializeField] float engineIdleRPM = 1000f;
    [SerializeField] float motorBrake = 1000f;

    [Header("Engine Inertia Settings")]
    [SerializeField] float engineInertia = 10f;
    [SerializeField] float baseFriction = 0.7f;
    [SerializeField] float friction = 7f;

    [Header("Engine Jitter")]
    [SerializeField] float minJitter = -3;
    [SerializeField] float maxJitter = 4;

    [Header("Engine Start")]
    [SerializeField] float addTorqueValue = 5f;
    [SerializeField] float addRPMValue = 40f;
    [SerializeField] float startEngineTorque = 100f;
    [SerializeField] float stallRPM = 900f;

    bool isRunning = false;
    bool ignite = false;

    public float currentRPM { get; private set; }
    public float currentEngineTorque { get; private set; }
    public float currentNetEngTorque { get; private set; }
    public float throttleVal { get; private set; }

    void Start()
    {
        carBody = GetComponent<Rigidbody>();
        basicCarController = GetComponent<BasicCarController>();
        basicGearBox = GetComponent<BasicGearBox>();
    }

    void FixedUpdate()
    {
        throttleVal = basicCarController.carInputs.Drive.Throttle.ReadValue<float>(); // TODO: Look for more efficient methot.
        //float throttleVal = basicGearBox.throttleValue;

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

        bool disconnected = basicGearBox.currentGear == 0 || basicGearBox.clutchVal >= basicGearBox.GetClutchThreshold();

        float totalRatio = basicGearBox.TotalRatio();
        float wheelInertia = 20f; // This value will come from different wheel script. Temporarily here.
        // Car's mass already calculated by the physic engine itself. So instead, adding wheel inertia will be more accurate I guess?
        float reflectedInertia = (!disconnected && Mathf.Abs(totalRatio) > 0.0001f) ? wheelInertia * basicCarController.driveWheels[0].radius
        * basicCarController.driveWheels[0].radius / (totalRatio * totalRatio) : 0f;

        currentNetEngTorque = currentEngineTorque - frictionTorque;

        float clutchTorque = disconnected ? 0f : basicGearBox.clutchTorque;
        float angularAcceleration = currentNetEngTorque - clutchTorque / (engineInertia + reflectedInertia + basicGearBox.GearboxInertia());

        float deltaRPM = angularAcceleration * (60 / (2f * Mathf.PI)) * Time.fixedDeltaTime;

        float scaleJitter = 0.5f;
        float jitterValue = Random.Range(minJitter, maxJitter) * scaleJitter;
        currentRPM += deltaRPM + jitterValue;
        
        float clutchThreshold = 0.5f;
        if (basicGearBox.currentGear != 0 && basicGearBox.clutchVal < clutchThreshold)
        {
            float targetRPM = basicGearBox.ApplyGearRatio(basicCarController.avgWheelRPM);
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
            currentRPM -= 500f;
        }
    }

    float CalculateTorque(float throttle)
    {
        float motorBrakeVal = CalculateMotorBrake(throttle);

        float t = Mathf.InverseLerp(0, engineMaxRPM, currentRPM);
        float accelMultiplier = Mathf.Clamp01(torqueCurve.Evaluate(t));

        float idleBoostThreshold = 200f;
        float idleFactor = Mathf.Clamp01(1f - (currentRPM - engineIdleRPM) / idleBoostThreshold);
        float multiplier = 0.1f;
        float idleTorque = currentRPM <= engineIdleRPM + idleBoostThreshold ? engineTorque * multiplier * idleFactor : 0f;

        float totalTorque = throttle * engineTorque * accelMultiplier;
        return totalTorque + idleTorque - motorBrakeVal;
    }

    // There are also other mechanical resistans that effect the the inertia. CalculateMotorBrake will handle that.
    float CalculateMotorBrake(float throttle)
    {
        float engineBrakeThreshold = 50f;
        if (throttle < 0.1f && currentRPM > engineIdleRPM + engineBrakeThreshold)
        {
            float brakeTorque = motorBrake * (currentRPM / engineMaxRPM);

            return brakeTorque;
        }

        return 0;
    }

    void IgniteEngine()
    {
        if (isRunning) return;

        if (ignite && !isRunning)
        {
            currentNetEngTorque += addTorqueValue;
        }
        else if (!ignite && !isRunning && Mathf.Abs(currentNetEngTorque) >= 0)
        {
            currentNetEngTorque -= addTorqueValue;
            currentRPM -= addRPMValue;

            if (currentNetEngTorque < 0)
                currentNetEngTorque = 0f;
            
            if (currentRPM < 0)
                currentRPM = 0;
        }

        if (currentNetEngTorque >= startEngineTorque)
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

    public float GetMaxEngineTorque()
    {
        return engineTorque;
    }

    public float GetEngineInertia()
    {
        return engineInertia;
    }
}