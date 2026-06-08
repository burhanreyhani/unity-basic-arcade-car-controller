using System;
using SuspensionData;

public class Suspension
{
    SuspensionInput input;

    public float currentLength { get; private set; }
    public float normalForce { get; private set; }
    public float compressionRatio { get; private set; }

    public struct SuspensionDebug
    {
        public float springForce;
        public float damperForce;
        public float normalForce;
        public float compressionRatio;
    }

    public Suspension(SuspensionInput input)
    {
        this.input = input;
        currentLength = input.restLength;
    }
    
    // TODO: Do this need dt?
    public SuspensionDebug UpdateSuspension(float raycastLength, float pointVelocity, float springTravel)
    {
        currentLength = raycastLength;

        float compression = (input.restLength - currentLength) / springTravel;
        compression = Math.Clamp(compression, 0f, 1f);
        compressionRatio = compression;

        float springForce = input.springStiffness * compression;
        float damperForce = input.damping * pointVelocity;

        normalForce = Math.Max(0f, springForce - damperForce);

        return new SuspensionDebug
        {
            springForce = springForce,
            damperForce = damperForce,
            normalForce = normalForce,
            compressionRatio = compressionRatio 
        };
    }
    
    public static float AntiRollForce(Suspension left, Suspension right, float stiffness)
    {
        float travelDiff = left.compressionRatio - right.compressionRatio;
        return travelDiff * stiffness;
    }
}
