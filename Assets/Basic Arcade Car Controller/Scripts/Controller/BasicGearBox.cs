using UnityEngine;

public class BasicGearBox : MonoBehaviour
{
    BasicEngine basicEngine;

    [Header("Gearbox Inertia Settings")]
    [SerializeField] float gearboxMass = 25f;
    [SerializeField] float gearboxRadius = 0.5f;

    [Header("Gearbox Ratio Settings")]
    [SerializeField] float[] gearRatios = { 1, 3.9f, 3.1f, 2.4f, 1.5f, 0.9f };
    [SerializeField] float reverseRatio = 2.1f;
    [SerializeField] float finalDriveRT = 4.1f;
    [SerializeField] float gearUpRPM = 4500f;
    [SerializeField] float gearDownRPM = 1400f;

    [Header("Clutch Settings")]
    [SerializeField] float clutchStiffness = 16000f;
    [SerializeField] float clutchSpeed = 5f;
    [SerializeField] float clutchResistance = 0.072f;

    public InputMap inputMap;

    public float lastShiftTime { get; private set; }
    public int currentGear { get; private set; }
    public float clutchVal { get; private set; }
    public float clutchTorque { get; private set; }
    
    float clutchInputThresholdForGear = 0.99f; // TODO: this can be one value.
    float clutchThreshold = 0.99f; // TODO: this can be one value.

    bool gearUp;
    bool gearDown;

    bool justShifted;

    float clutchInput;

    void Awake()
    {
        inputMap = new InputMap();
    }

    void OnEnable()
    {
        inputMap.Enable();
    }

    void OnDisable()
    {
        inputMap.Disable();
    }

    void Start()
    {
        basicEngine = GetComponent<BasicEngine>();
    }

    void Update()
    {
        clutchInput = inputMap.Drive.Clutch.ReadValue<float>();
        //clutchInput = clutchValue;

        gearUp = inputMap.Drive.GearUp.WasPressedThisFrame();
        gearDown = inputMap.Drive.GearDown.WasPressedThisFrame();

        if (clutchVal >= clutchInputThresholdForGear && gearUp)
        {
            ShiftUp();
        }

        if (clutchVal >= clutchInputThresholdForGear && gearDown)
        {
            ShiftDown();
        }
    }

    void FixedUpdate()
    {
        /*
        float speed = Vector3.Dot(rb.linearVelocity, transform.forward);

        if (Mathf.Abs(speed) < 0.5f)
        {
            currentGear = 0;
        }
        else if (speed > 0.5f && currentGear == 0)
        {
            currentGear = 1;
            justShifted = false;
        }
        else
        {
            ShiftUp();
            ShiftDown();
        }
        */
    }

    public float GearboxInertia()
    {
        if (currentGear == 0 || clutchVal >= clutchInputThresholdForGear)
        {
            return 0;
        }

        return 0.5f * gearboxMass * (gearboxRadius * gearboxRadius);
    }

    public float ApplyTorque(float wheelRPM)
    {
        clutchTorque = CalculateClutch(wheelRPM);
        
        // This is for power that goes to the drivetrain.
        if (currentGear == 0 || clutchVal >= clutchThreshold)
        {
            return 0;
        }

        if (currentGear == -1)
        {
            return -clutchTorque * reverseRatio * finalDriveRT;
        }

        return clutchTorque * TotalRatio();
    }
    
    public float ApplyGearRatio(float avgWheelRPM)
    {
        // This is for engine RPM snap.
        float wheelOmega = avgWheelRPM * 2f * Mathf.PI / 60f; // TODO: avgWheelRPM will come from drivetrain

        if (currentGear == 0)
        {
            return 0;
        }

        if (currentGear == -1)
        {
            return -wheelOmega * reverseRatio * finalDriveRT;
        }

        return wheelOmega * TotalRatio() * 60f / (2f * Mathf.PI);
    }

    float InputOmega()
    {
        return basicEngine.currentRPM * 2 * Mathf.PI / 60f;
    }
    
    // Not used maybe won't needed. I don't know, we'll see...
    float MaxOmega()
    {
        return basicEngine.GetMaxRPM() * 2 * Mathf.PI / 60f;
    }

    public void ShiftUp()
    {
        //if (Time.time - lastShiftTime < shiftCooldown) return; // TODO: This will be changed with clutch

        if (currentGear >= -1 && currentGear < gearRatios.Length - 1) // && basicEngine.currentRPM > gearUpRPM
        {
            currentGear++;
            lastShiftTime = Time.time;
            justShifted = true;
        }
    }

    public void ShiftDown()
    {
        //if (Time.time - lastShiftTime < shiftCooldown) return; // TODO: This will be changed with clutch

        if (currentGear > -1) // && basicEngine.currentRPM <= gearDownRPM
        {
            currentGear--;
            lastShiftTime = Time.time;
            justShifted = true;
        }
    }

    public float TotalRatio()
    {
        if (currentGear == 0) return 0;
        if (currentGear == -1) return reverseRatio * finalDriveRT;

        return CurrentGearRatio() * finalDriveRT;
    }

    /*
    float AutoClutch()
    {
        float clutchThreshold = 200f;
        if ((basicEngine.currentRPM < basicEngine.GetIdleEngineRPM() + clutchThreshold || justShifted) && currentGear != 0) // TODO: It is bianry rightnow, will be smoothed in the future. Also delete currentGear != 0
        {
            return 1f;
        }
        return 0;
    }
    */

    // TODO: Clutch is broken.
    float CalculateClutch(float wheelRPM)
    {
        float engineOmega = InputOmega();
        float drivetrainOmega = wheelRPM * 2f * Mathf.PI / 60 * TotalRatio();

        if (clutchInput >= 0.1f)
        {
            clutchVal = 1f;
        }
        else
        {
            clutchVal = Mathf.MoveTowards(clutchVal, 0, Time.fixedDeltaTime * clutchSpeed); // TODO: For manual that's better. But don't forget to implement auto gear
        }
        
        if (currentGear == 0 || basicEngine.currentRPM <= 0f)
        {
            return 0f; 
        }

        float clutchEngage = 1 - clutchVal;
        float deltaOmega = engineOmega - drivetrainOmega;

        float clutchTorqueMax = clutchStiffness * clutchEngage * clutchResistance;
        float rawFeedbackForce = deltaOmega * clutchStiffness * clutchEngage;

        return Mathf.Clamp(rawFeedbackForce, -clutchTorqueMax, clutchTorqueMax);
    }

    public float GetFinalDrive()
    {
        return finalDriveRT;
    }

    public float CurrentGearRatio()
    {
        return gearRatios[currentGear];
    }

    public void SetJustShifted(bool shifted)
    {
        justShifted = shifted;
    }

    public bool GetJustShifted()
    {
        return justShifted;
    }

    public float GetClutchThreshold()
    {
        return clutchThreshold;
    }
}