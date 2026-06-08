
public interface IDifferential
{
    void DistributeTorque(float inputTorque, float leftWheelRPM, float rightWheelRPM, out float leftTorque, out float rightTorque);
    float DiffLoad(float leftWheelRPM, float rightWheelRPM);
}