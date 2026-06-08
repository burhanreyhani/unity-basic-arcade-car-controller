
namespace EngineData
{
    /*
    Issues:

    EngineInput is a struct but contains reference type fields (float[] rpmPoints, float[] torquePoints). This partially defeats the purpose of a struct — it won't behave as a true value type because the arrays are shared references. Consider using a class for EngineInput, or switching to a fixed-size approach.
    The constructor parameter lists are very long. Named parameters or a builder pattern would improve readability when constructing these.
    No validation anywhere — nothing stops rpmPoints and torquePoints from having mismatched lengths, which would cause an index out of bounds in TorqueCurve.
    */

    public struct EngineInput
    {
        public float maxTorque;
        public float engineInertia;
        public float motorBrake;
        public float baseFriction;
        public float friction;
        public float idleRPM;
        public float maxRPM;
        public float[] rpmPoints; // TODO: Can be moved?
        public float[] torquePoints; // TODO: Can be moved?

        public EngineInput(float maxTorque, float engineInertia, float motorBrake, float baseFriction, float friction, float idleRPM, float maxRPM, float[] rpmPoints, float[] torquePoints)
        {
            this.maxTorque = maxTorque;
            this.engineInertia = engineInertia;
            this.motorBrake = motorBrake;
            this.baseFriction = baseFriction;
            this.friction = friction;
            this.idleRPM = idleRPM;
            this.maxRPM = maxRPM;
            this.rpmPoints = rpmPoints;
            this.torquePoints = torquePoints;
        }
    }
}
