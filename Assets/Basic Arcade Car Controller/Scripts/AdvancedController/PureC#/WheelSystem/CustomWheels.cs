using System;
using TireData;

public class CustomWheels
{
    public float wheelRPM { get; private set; }

    float relaxedSlipRatio;
    float relaxedSlipAngle;

    float prevFx;
    float prevFy;

    public float CalculateSlipAngle(in TireInput input)
    {
        float vz = MathF.Max(MathF.Abs(input.localVelocity.z), 0.1f);
        return MathF.Atan2(-input.localVelocity.x, vz);
    }

    public float CalculateSlipRatio(in TireInput input)
    {
        float v = input.localVelocity.z;
        float vAbs = MathF.Abs(input.localVelocity.z);
        float wheelCircumferentialSpeed = CalculateWheelCircumferentialSpeed(input);

        if (vAbs < 0.5f && MathF.Abs(wheelCircumferentialSpeed) < 0.5f)
        {
            return 0f;
        }

        float denom = MathF.Max(MathF.Max(vAbs, MathF.Abs(wheelCircumferentialSpeed)), 0.1f); // 0 check

        float slipRatio = (wheelCircumferentialSpeed - v) / denom;
        float clampValue = 10f;
        return Math.Clamp(slipRatio, -clampValue, clampValue);
    }

    public float CalculateWheelRPM(float wheelInertia, float netTorque, float deltaTime)
    {
        float wheelAlpha = netTorque / wheelInertia;
        float deltaRPM = wheelAlpha * 60f / (2f * MathF.PI) * deltaTime;
        wheelRPM += deltaRPM;

        float clampRPM = 10000f; // TODO: Test this. Delete or change if necessary.
        wheelRPM = Math.Clamp(wheelRPM, -clampRPM, clampRPM);
        return wheelRPM;
    }

    public float CalculateWheelCircumferentialSpeed(in TireInput input)
    {
        float omega = wheelRPM * 2f * MathF.PI / 60f;
        return omega * input.wheelRadius;
    }

    public float KappaN(in TireParams param)
    {
        float slipRatio = relaxedSlipRatio;
        return slipRatio / param.slipRatioPeak;
    }

    public float AlphaN(in TireParams param)
    {
        float slipAngle = relaxedSlipAngle;
        return slipAngle / param.slipAnglePeak;
    }

    public float CalculateCombinedSlip(float kappaN, float alphaN)
    {
        float combined = MathF.Sqrt(kappaN * kappaN + alphaN * alphaN);
        combined = MathF.Max(combined, 0.0001f); // 0 check

        return combined;
    }

    // latW is complement of longw. Thus not used directly right now. It will be necessary for asymetric falloff in the future.
    public float TireCurve(float combinedSlip, float longW, float latW, in TireParams param)
    {
        float peakValue = 2f / MathF.PI * MathF.Atan(param.xPeak);
        float dropRate = Lerp(param.dropRateLat, param.dropRateLong, longW); // DropRate mixture according to longitudinal weight.
        float smoothSlip = combinedSlip / (1f + combinedSlip);

        if (combinedSlip <= param.xPeak)
        {
            return 2f / MathF.PI * MathF.Atan(smoothSlip);
        }
 
        float excess = combinedSlip - param.xPeak;
        float drop = peakValue * MathF.Exp(-dropRate * excess);

        return MathF.Max(peakValue * param.minGrip, drop);
    }

    public void TireForces(in TireParams param, in TireInput input, out float Fx, out float Fy, float dt)
    {
        if (param.slipRatioPeak <= 0f || param.slipAnglePeak <= 0f)
        {
            Fx = 0f;
            Fy = 0f;
            return;
        }

        relaxedSlipRatio = RelaxedSlip(relaxedSlipRatio, CalculateSlipRatio(input), dt, param.relaxationLong, input);
        relaxedSlipAngle = RelaxedSlip(relaxedSlipAngle, CalculateSlipAngle(input), dt, param.relaxationLat, input);

        float kappaN = relaxedSlipRatio / param.slipRatioPeak;
        float alphaN  = relaxedSlipAngle / param.slipAnglePeak;

        float combinedSlip = CalculateCombinedSlip(kappaN, alphaN);;

        float longWeight = MathF.Abs(kappaN) / combinedSlip;
        float latWeight  = MathF.Abs(alphaN) / combinedSlip;

        float grip = TireCurve(combinedSlip, longWeight, latWeight, param);

        float fMax = input.normalForce * input.mu;
        float fTotal = fMax * grip;

        Fx = fTotal * (kappaN / combinedSlip);
        Fy = fTotal * (alphaN / combinedSlip);

        float scaleTime = 10f;
        float alpha = 1f - MathF.Exp(-dt * scaleTime);

        Fx = Lerp(prevFx, Fx, alpha);
        Fy = Lerp(prevFy, Fy, alpha);

        prevFx = Fx;
        prevFy = Fy;
    }

    public float CalculateRollingResistance(TireParams param, float normalForce)
    {
        float rollingResistance = param.rollingResistanceCoeff * normalForce;
        return rollingResistance;
    }

    public float RelaxedSlip(float current, float target, float dt, float relaxation, TireInput input)
    {
        float wheelSpeed = CalculateWheelCircumferentialSpeed(input);
        float speed = MathF.Max(MathF.Abs(wheelSpeed), 0.1f); // 0 check
        float tau = relaxation / MathF.Max(speed, 1f);

        float alpha = 1f - MathF.Exp(-dt / tau);
        return Lerp(current, target, alpha);
    }
    public void ResetRelaxedSlip()
    {
        relaxedSlipRatio = 0f;
        relaxedSlipAngle = 0f;
    }

    float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
