
public interface IInputProvider
{
    float Throttle { get; }
    float Steering { get; }
    float Brake { get; }
    float Handbrake { get; }
    float Ignition { get; }
    float KillEngine { get; }
    float Clutch { get; }

    bool GearUp { get; }
    bool GearDown { get; }
}
