using UnityEngine;

public class PlayerInputHandler : MonoBehaviour, IInputProvider
{
    InputMap carInputs;

    public float Throttle { get; private set; }
    public float Steering { get; private set; }
    public float Brake { get; private set; }
    public float Handbrake { get; private set; }
    public float Ignition { get; private set; }
    public float KillEngine { get; private set; }
    public float Clutch { get; private set; }

    public bool GearUp { get; private set; }
    public bool GearDown { get; private set; }

    void Awake()
    {
        carInputs = new InputMap();
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
        Throttle = carInputs.Drive.Throttle.ReadValue<float>();;
        Steering = carInputs.Drive.Steer.ReadValue<float>();
        Brake = carInputs.Drive.Brake.ReadValue<float>();
        Handbrake = carInputs.Drive.Handbrake.ReadValue<float>();
        Ignition = carInputs.Drive.Ignition.ReadValue<float>();
        KillEngine = carInputs.Drive.KillEngine.ReadValue<float>();
        Clutch = carInputs.Drive.Clutch.ReadValue<float>();
    }

    void Update()
    {
        GearUp = carInputs.Drive.GearUp.WasPressedThisFrame();
        GearDown = carInputs.Drive.GearDown.WasPressedThisFrame();
    }
}
