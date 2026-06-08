
namespace SuspensionData
{
    public struct SuspensionInput
    {
        public float restLength;
        public float springStiffness;
        public float damping;
        public float antiRollStiffness;
        public float wheelMass;

        public SuspensionInput(float restLength, float springStiffness, float damping, float antiRollStiffness, float wheelMass)
        {
            this.restLength = restLength;
            this.springStiffness = springStiffness;
            this.damping = damping;
            this.antiRollStiffness = antiRollStiffness;
            this.wheelMass = wheelMass;
        }
    }
}
