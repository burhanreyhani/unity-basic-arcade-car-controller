using System;

public class Clutch
{
    public float clutchInput {get; private set; }
    public float clutchTorqueMax {get; private set; }
    public float clutchStiffness {get; private set; }

    public float CalculateClutch(float engineOmega, float gearboxOmega)
    {
        float clutchEngage = 1f - clutchInput;
        float deltaOmega = engineOmega - gearboxOmega;
        float torque = deltaOmega * clutchStiffness;
        float maxTorque = clutchTorqueMax * clutchEngage;

        return Math.Clamp(torque, -maxTorque, maxTorque);
    }

    /*
    TODO: When smooth it, make sure the transition curve is based on clutchEngage in CalculateClutch, not another hardcoded threshold.
    */
    public float AutoClutch(float engineRPM, float idleRPM, int currentGear, bool isShifting) // Not used yet.
    {
        float clutchThreshold = 200f;
        if ((engineRPM < idleRPM + clutchThreshold || isShifting) && currentGear != 0) // TODO: It is bianry rightnow, will be smoothed in the future. Also delete currentGear != 0
        {
            return 1f;
        }
        return 0;
    }

    public void SetClutchInput(float input) // For testing
    {
        clutchInput = input;
    }

    public void SetClutchTorqueMax(float trq)
    {
        clutchTorqueMax = trq;
    }

    public void SetClutchStiffness(float stiffness)
    {
        clutchStiffness = stiffness;
    }
}
