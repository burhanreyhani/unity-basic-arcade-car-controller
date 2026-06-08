using System;

public class LimitedSlipDifferential : IDifferential
{
    float lockingTorque; // Max torque bias between wheels
    float lockThreshold; // RPM difference that starts locking
    float lockStrength; // How aggressively it locks 0-1

    float scaleDistribution = 0.5f;

    public LimitedSlipDifferential(float lockingTorque, float lockThreshold, float lockStrength)
    {
        this.lockingTorque = lockingTorque;
        this.lockThreshold = lockThreshold;
        this.lockStrength = lockStrength;
    }

    public void DistributeTorque(float inputTorque, float leftWheelRPM, float rightWheelRPM, out float leftTorque, out float rightTorque)
    {
        float baseTorque = inputTorque * scaleDistribution;

        float rpmDiff = leftWheelRPM - rightWheelRPM;
        float absDiff = MathF.Abs(rpmDiff);

        float lockFactor = Math.Clamp((absDiff - lockThreshold) / lockThreshold * lockStrength, 0f, 1f);
        float transferTorque = lockingTorque * lockFactor;

        if (rpmDiff > 0)
        {
            leftTorque = baseTorque - transferTorque;
            rightTorque = baseTorque + transferTorque;
        }
        else
        {
            leftTorque = baseTorque + transferTorque;
            rightTorque = baseTorque - transferTorque;
        }
    }

    public float DiffLoad(float leftWheelRPM, float rightWheelRPM)
    {
        return (MathF.Abs(leftWheelRPM) + MathF.Abs(rightWheelRPM)) * 0.5f;
    }
}
