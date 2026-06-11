using UnityEngine;

public class BasicWheel : MonoBehaviour
{
    Rigidbody rb;
    BasicDrivetrain basicDrivetrain;

    [SerializeField] float frontWheelRadius = 0.4f;
    [SerializeField] float rearWheelRadius = 0.4f;
    [SerializeField] float frontWheelInertia = 25f;
    [SerializeField] float rearWheelInertia = 25f;

    float frontLeftRPM;
    float frontRightRPM;
    float rearLeftRPM;
    float rearRightRPM;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        basicDrivetrain = GetComponent<BasicDrivetrain>();
    }

    void FixedUpdate()
    {
        basicDrivetrain.DistributeTorque(frontLeftRPM, frontRightRPM, rearLeftRPM, rearRightRPM,
        out float frontLeftTorque, out float frontRightTorque, out float rearLeftTorque, out float rearRightTorque);
        basicDrivetrain.CalculateDownstramTorque(frontLeftRPM, frontRightRPM, rearLeftRPM, rearRightRPM);
        basicDrivetrain.DrivenWheelRadius(frontWheelRadius, rearWheelRadius);

        CaluclateWheelRPM(frontLeftRPM, frontWheelInertia, frontLeftTorque);
        CaluclateWheelRPM(frontRightRPM, frontWheelInertia, frontRightTorque);
        CaluclateWheelRPM(rearLeftRPM, frontWheelInertia, rearLeftTorque);
        CaluclateWheelRPM(rearRightRPM, frontWheelInertia, rearRightTorque);

        rb.AddForce(transform.forward * frontLeftTorque);
        rb.AddForce(transform.forward * frontRightTorque);
        rb.AddForce(transform.forward * rearLeftTorque);
        rb.AddForce(transform.forward * rearRightTorque);
    }

    float CaluclateWheelRPM(float wheelRPM, float wheelInertia, float netTorque)
    {
        float wheelAlpha = netTorque / wheelInertia;
        float deltaRPM = wheelAlpha * 60f / (2f * Mathf.PI) * Time.fixedDeltaTime;
        wheelRPM += deltaRPM;

        float clampRPM = 10000f;
        wheelRPM = Mathf.Clamp(wheelRPM, -clampRPM, clampRPM);
        return wheelRPM;
    }
}
