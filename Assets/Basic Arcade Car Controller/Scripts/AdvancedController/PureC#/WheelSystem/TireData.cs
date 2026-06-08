
namespace TireData
{
    public struct Velocity2
    {
        public float x;
        public float z;

        public Velocity2(float x, float z)
        {
            this.x = x;
            this.z = z;
        }    
    }

    public struct TireInput
    {
        public Velocity2 localVelocity;
        public float wheelRadius;
        public float normalForce;
        public float mu;


        public TireInput(Velocity2 localVelocity, float wheelRadius, float normalForce, float mu)
        {
            this.localVelocity = localVelocity;
            this.wheelRadius = wheelRadius;
            this.normalForce = normalForce;
            this.mu = mu;
        }
    }

    public struct TireParams
    {
        public float slipRatioPeak;
        public float slipAnglePeak;
        public float minGrip;
        public float dropRateLong;
		public float dropRateLat;
        public float xPeak;
        public float relaxationLong;
        public float relaxationLat;
        public float rollingResistanceCoeff;

        public TireParams(float slipRatioPeak, float slipAnglePeak, float minGrip, float dropRateLong, float dropRateLat,
        float xPeak, float relaxationLong, float relaxationLat, float rollingResistanceCoeff)
        {
            this.slipRatioPeak = slipRatioPeak;
            this.slipAnglePeak = slipAnglePeak;
            this.minGrip = minGrip;
            this.dropRateLong = dropRateLong;
            this.dropRateLat = dropRateLat;
            this.xPeak = xPeak; // Peak value of tire curve
            this.relaxationLong = relaxationLong;
            this.relaxationLat = relaxationLat;
            this.rollingResistanceCoeff = rollingResistanceCoeff;
        }
    }
}
