using System;

public class Gearbox
{
    Engine engine;
    Clutch clutch;
    float gearboxRotMass = 25f;

    public float[] gearRatios { get; private set; }
    public float reverseRatio { get; private set; }
    public float finalDrive { get; private set; }
    public int currentGear { get; private set; }

    public Gearbox(Engine engine, Clutch clutch)
    {
        this.engine = engine;
        this.clutch = clutch;
    }

    public float GearboxInertia()
    {
        if (currentGear == 0 || clutch.clutchInput >= 0.1)
        {
            return 0f;
        }

        float inputOmega = InputOmega();
        float maxOmega = MaxOmega();

        float rpmFactor = inputOmega / maxOmega;
        return gearboxRotMass * rpmFactor;
    }

    public float ApplyGearRatio() // Still not used yet.
    {
        // TODO: Replace with actual wheel RPM once wheels connected
        float inputOmega = InputOmega();

        if (currentGear == 0)
        {
            return 0;
        }

        if (currentGear == -1)
        {
            return -inputOmega / reverseRatio / finalDrive;
        }

        return inputOmega / CurrentGearRatio() / finalDrive;
    }
    
    // ApplyTorque and AppylGearRatio must be used inside another func for calculating WheelReactionSpeed.
    public float ApplyTorque(float wheelRPM)
    {
        if (currentGear == 0)
        {
            return 0;
        }

        float engineOmega = InputOmega();
        float gearboxOmega = wheelRPM * 2f * MathF.PI / 60f * TotalRatio(); // Wheels are not connected yet.
        float clutchTorque = clutch.CalculateClutch(engineOmega, gearboxOmega);

        if (currentGear == -1)
        {
            return -clutchTorque * reverseRatio * finalDrive;
        }

        return clutchTorque * CurrentGearRatio() * finalDrive;
    }

    public float InputOmega()
    {
        return engine.currentRPM * 2 * MathF.PI / 60f;
    }

    public float MaxOmega()
    {
        return engine.MaxRPM * 2f * MathF.PI / 60f;
    }

    public void ShiftUp(float gearUpRPM, float currentRPM)
    {
        if (currentGear >= -1 && currentGear < gearRatios.Length - 1) // && currentRPM > gearUpRPM
        {
            currentGear++;
        }
    }

    public void ShiftDown(float gearDownRPM, float currentRPM)
    {
        if (currentGear > -1) // && currentRPM <= gearDownRPM
        {
            currentGear--;
        }
    }

    public float CurrentGearRatio()
    {
        return gearRatios[currentGear];
    }

    public float TotalRatio()
    {
        if (currentGear == 0) return 0;
        if (currentGear == -1) return reverseRatio * finalDrive;
        return CurrentGearRatio() * finalDrive;
    }

    public void SetGear(int gear)
    {
        if (gearRatios == null) return;
        if (gear < -1 || gear >= gearRatios.Length) return;
        currentGear = gear;
    }

    public void SetGearboxRotMass(float mass)
    {
        gearboxRotMass = mass;
    }

    public void SetEngine(Engine engine)
    {
        this.engine = engine;
    }

    public void SetGearRatios(float[] gearRT)
    {
        gearRatios = gearRT;
    }

    public void SetReverseRatio(float rvrRT)
    {
        reverseRatio = rvrRT;
    }

    public void SetFinalDrive(float fnlDrive)
    {
        finalDrive = fnlDrive;
    }
}
