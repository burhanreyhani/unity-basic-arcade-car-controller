
using System;

public class OpenDifferential : IDifferential
{
    float scaleDistribution = 0.5f;

    public void DistributeTorque(float inputTorque, float lWPM, float rWRPM, out float leftTorque, out float rightTorque) // rWRPM, lWPM not needed for open diff.
    {
        leftTorque = inputTorque * scaleDistribution;
        rightTorque = inputTorque * scaleDistribution;
    }

    public float DiffLoad(float leftWheelRPM, float rightWheelRPM)
    {
        return (MathF.Abs(leftWheelRPM) + MathF.Abs(rightWheelRPM)) * 0.5f;
    }
}
