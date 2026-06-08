using System;
using EngineData;

public class Engine
{
    bool isRunning = false;
    bool ignite = false;
    float addTorqueValue = 20f;
    float addRPMValue = 40f;
    float startEngineTorque = 300f;
    float stallRPM = 900f;
    public float engineTorque { get; private set; }

    public float currentRPM { get; private set; }
    EngineInput input;
    Drivetrain drivetrain;
    Gearbox gearbox;
    Clutch clutch;

    Random jitter = new Random();

    public Engine(EngineInput input, Gearbox gearbox, Drivetrain drivetrain, Clutch clutch)
    {
        this.input = input;
        this.gearbox = gearbox;
        this.drivetrain = drivetrain;
        this.clutch = clutch;
        currentRPM = 0;
    }

    public struct EngineDebug
    {
        public float rpm;
        public float friction;
        public float angularAcc;
        public float reflectedInert;
        public float drivetrainLoad;
    }

    // TODO: There is no HP calculation.

    public EngineDebug UpdateEngine(float deltaTime, float throttle, float mass, float wheelRadius)
    {
        IgniteEngine();

        if (!isRunning)
        {
            return CreateDebug(); // It is needed for debug.
        }

        engineTorque = CalculateTorque(currentRPM, throttle);
        float frictionTorque = input.baseFriction + input.friction * (currentRPM / input.maxRPM);

        bool disconnected = gearbox.currentGear == 0 || clutch.clutchInput >= 0.1f;
        float totalRatio = gearbox.TotalRatio();
        float reflectedInertia = (!disconnected && Math.Abs(totalRatio) > 0.0001f) ? mass * wheelRadius * wheelRadius / (totalRatio * totalRatio) : 0f; // Some values might go different places.
        float drivetrainload = disconnected ? 0f : drivetrain.DrivetrainLoad();
        float angularAcceleration = (engineTorque - frictionTorque - drivetrainload) / (input.engineInertia + reflectedInertia + gearbox.GearboxInertia());

        float deltaRPM = angularAcceleration * (60 / (2f * MathF.PI)) * deltaTime;

        float scaleJitter = 0.5f;
        float jitterValue = (float)jitter.Next(-3, 4) * scaleJitter;
        currentRPM += deltaRPM + jitterValue - CalculateMotorBrake(throttle, deltaTime);

        if (currentRPM < input.idleRPM && throttle < 0.01f && disconnected)
        {
            currentRPM = input.idleRPM;
        }
        else if (currentRPM < stallRPM)
        {
            isRunning = false;
        }

        if (currentRPM > input.maxRPM)
        {
            currentRPM = input.maxRPM;
        }

        return new EngineDebug
        {
            rpm = currentRPM,
            friction = frictionTorque,
            angularAcc = angularAcceleration,
            reflectedInert = reflectedInertia,
            drivetrainLoad = drivetrainload
        };
    }

    public float CalculateTorque(float rpm, float throttle)
    {   
        float idleBoostThreshold = 200f;
        float multiplier = 0.1f;
        float idleTorque = rpm <= input.idleRPM + idleBoostThreshold ? input.maxTorque * multiplier : 0f;
        float currentTorque = throttle * input.maxTorque * TorqueCurve(rpm);
        return currentTorque + idleTorque;
    }

    public float TorqueCurve(float rpm)
    {        
        for (int i = 0; i < input.rpmPoints.Length - 1; i++)
        {
            if (rpm >= input.rpmPoints[i] && rpm <= input.rpmPoints[i + 1])
            {
                float t = (rpm - input.rpmPoints[i]) / (input.rpmPoints[i + 1] - input.rpmPoints[i]); // Normalized value
                return input.torquePoints[i] + t * (input.torquePoints[i + 1] - input.torquePoints[i]);
            }
        }
        return input.torquePoints[^1];
    }

    public float CalculateMotorBrake(float throttle, float deltaTime) // TODO: if clutching, this should not affect other parts other than engine while braking.
    {
        if (throttle < 0.1f)
        {
            float brakeTorque = input.motorBrake * (currentRPM / input.maxRPM);
            float deltaRPM = brakeTorque / input.engineInertia * (60f / (2f * MathF.PI)) * deltaTime;

            return deltaRPM;
        }
        return 0;
    }

    public void IgniteEngine()
    {
        if (isRunning) return;

        if (ignite && !isRunning)
        {
            engineTorque += addTorqueValue;
        }
        else if (!ignite && !isRunning && engineTorque >= 0)
        {
            engineTorque -= addTorqueValue;
            currentRPM -= addRPMValue;

            if (engineTorque < 0)
                engineTorque = 0f;

            if (currentRPM < 0)
                currentRPM = 0f;
        }
        
        if (engineTorque >= startEngineTorque)
        {
            currentRPM += addRPMValue;

            if(currentRPM >= input.idleRPM)
            {
                isRunning = true;
            }
        }
    }

    public void KillEngine()
    {
        float killEngineThreshold = 200;
        if (currentRPM > input.idleRPM + killEngineThreshold)
        {
            return;
        }
        isRunning = false;
    }

    public void SetIgnite(bool ignt)
    {
        ignite = ignt;
    }

    public void SetAddTorqueValue(float trq)
    {
        addTorqueValue = trq;
    }

    public void SetStartTorque(float strtTrq)
    {
        startEngineTorque = strtTrq;
    }

    public void SetAddRPMValue(float rpmvl)
    {
        addRPMValue = rpmvl;
    }

    public void SetStallRPM(float stllRPM)
    {
        stallRPM = stllRPM;
    }

    public bool GetIsRunning()
    {
        return isRunning;
    }

    public float MaxRPM => input.maxRPM;

    EngineDebug CreateDebug(float friction = 0f, float angularAcc = 0f, float reflected = 0f, float drivetrain = 0f) // TODO: Delete when you done with this.
    {
        return new EngineDebug
        {
            rpm = currentRPM,
            friction = friction,
            angularAcc = angularAcc,
            reflectedInert = reflected,
            drivetrainLoad = drivetrain
        };
    }
}
