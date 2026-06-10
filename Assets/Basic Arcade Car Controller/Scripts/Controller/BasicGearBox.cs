using UnityEngine;

public class BasicGearBox : MonoBehaviour
{
    //Rigidbody rb;
    BasicCarController basicCarController;
    BasicEngine basicEngine;

    [Header("Gearbox Inertia Settings")]
    [SerializeField] float gearboxMass = 25f;
    [SerializeField] float gearboxRadius = 0.5f;

    [Header("Gearbox Ratio Settings")]
    [SerializeField] float[] gearRatios = { 0, 3.9f, 3.1f, 2.4f, 1.5f, 0.9f };
    [SerializeField] float reverseRatio = 2.1f;
    [SerializeField] float finalDriveRT = 4.1f;
    [SerializeField] float gearUpRPM = 4500f;
    [SerializeField] float gearDownRPM = 1400f;

    [Header("Clutch Settings")]
    [SerializeField] float clutchStiffness = 50f;
    [SerializeField] float clutchTorqueMax = 250f;
    [SerializeField] float clutchSpeed = 5f;

    public InputMap inputMap;

    public float lastShiftTime { get; private set; }
    public float clutchInput { get; private set; }
    public int currentGear { get; private set; }

    float clutchTorque;
    float clutchVal;
    float clutchInputThreshold = 0.9f;

    bool gearUp;
    bool gearDown;

    bool C;

    bool justShifted;

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
        //rb = GetComponent<Rigidbody>();
        basicEngine = GetComponent<BasicEngine>();
        basicCarController = GetComponent<BasicCarController>();
    }

    void Update()
    {
        clutchInput = inputMap.Drive.Clutch.ReadValue<float>();

        gearUp = inputMap.Drive.GearUp.WasPressedThisFrame();
        gearDown = inputMap.Drive.GearDown.WasPressedThisFrame();

        if (clutchInput >= clutchInputThreshold && gearUp)
        {
            ShiftUp();
        }

        if (clutchInput >= clutchInputThreshold && gearDown)
        {
            ShiftDown();
        }
    }

    void FixedUpdate()
    {
        clutchTorque = CalculateClutch();
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
        if (currentGear == 0 || clutchInput >= clutchInputThreshold)
        {
            return 0;
        }

        return 0.5f * gearboxMass * (gearboxRadius * gearboxRadius);
    }

    public float ApplyTorque()
    {
        // This is for power that goes to the drivetrain.
        if (currentGear == 0)
        {
            return 0;
        }

        if (currentGear == -1)
        {
            return -clutchTorque * reverseRatio * finalDriveRT;
        }

        return clutchTorque * CurrentGearRatio() * finalDriveRT;
    }

    public float ApplyGearRatio(float avgWheelRPM)
    {
        // This is for engine RPM snap.
        float wheelOmega = avgWheelRPM * 2f * Mathf.PI / 60f;

        if (currentGear == 0)
        {
            return 0;
        }

        if (currentGear == -1)
        {
            return -wheelOmega * reverseRatio * finalDriveRT;
        }

        return wheelOmega * CurrentGearRatio() * finalDriveRT;
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
    
    float CalculateClutch()
    {
        float engineOmega = InputOmega();
        float gearboxOmega = basicCarController.avgWheelRPM * 2f * Mathf.PI / 60f * TotalRatio();

        clutchVal = Mathf.MoveTowards(clutchVal, clutchInput, Time.deltaTime * clutchSpeed);

        float clutchEngage = 1f - clutchVal;
        float deltaOmega = engineOmega - gearboxOmega;
        float torque = deltaOmega * clutchStiffness;
        float maxTorque = clutchTorqueMax * clutchEngage;

        return Mathf.Clamp(torque, 0, maxTorque);
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
}