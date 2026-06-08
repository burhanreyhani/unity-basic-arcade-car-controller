using System;

public class LockedDifferential : IDifferential
{
    float scaleDistribution = 0.5f;

    public void DistributeTorque(float inputTorque, float leftWheelRPM, float rightWheelRPM, out float leftTorque, out float rightTorque)
    {
        float totalRPM = MathF.Abs(leftWheelRPM) + MathF.Abs(rightWheelRPM);
        
        if (totalRPM < 0.0001f)
        {
            leftTorque = inputTorque * scaleDistribution;
            rightTorque = inputTorque * scaleDistribution;
            return;
        }

        float leftShare = MathF.Abs(leftWheelRPM) / totalRPM;
        float rightShare = MathF.Abs(rightWheelRPM) / totalRPM;

        leftTorque = inputTorque * rightShare;
        rightTorque =inputTorque * leftShare;
    }

    public float DiffLoad(float leftWheelRPM, float rightWheelRPM)
    {
        return (MathF.Abs(leftWheelRPM) + MathF.Abs(rightWheelRPM)) * 0.5f;
    }
}
