using Unity.Mathematics;
using UnityEngine;

public class BasicWheel : MonoBehaviour
{
    Rigidbody rb;
    BasicGearBox basicGearBox;
    BasicEngine basicEngine;

    [SerializeField] float frontWheelRadius = 0.37f;
    [SerializeField] float rearWheeRadius = 0.37f;

    [SerializeField] Transform frontLeftWheel;
    [SerializeField] Transform frontRightWheel;
    [SerializeField] Transform rearLeftWheel;
    [SerializeField] Transform rearRightWheel;

    public float avgWheelRPM { get; private set; }

    float distributedTorque;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        basicGearBox = GetComponent<BasicGearBox>();
        basicEngine = GetComponent<BasicEngine>();
    }

    void FixedUpdate()
    {
        CalculateWheelRPM();

        //float throttle = basicEngine.throttleVal;
    
        //rb.AddForceAtPosition(transform.forward * basicGearBox.ApplyTorque(wheelRPM), frontLeftWheel.position);
        //rb.AddForceAtPosition(transform.forward * basicGearBox.ApplyTorque(wheelRPM), frontRightWheel.position);

        distributedTorque = basicGearBox.ApplyTorque(avgWheelRPM) / 2;
    
        rb.AddForceAtPosition(frontLeftWheel.forward * distributedTorque * 0.85f, frontLeftWheel.position - frontLeftWheel.up);
        rb.AddForceAtPosition(frontRightWheel.forward * distributedTorque * 0.85f, frontRightWheel.position - frontRightWheel.up);
    }

    void CalculateWheelRPM()  // TODO: Temporary calculation.
    {
        avgWheelRPM = rb.linearVelocity.magnitude / frontWheelRadius * 60f / (2f * Mathf.PI);
        /*
        if (basicEngine.currentRPM == 0) return 0;

        float totalRatio = basicGearBox.TotalRatio();

        if (Mathf.Approximately(totalRatio, 0f)) 
        {
            return 0f; 
        }

        return basicEngine.currentRPM / basicGearBox.TotalRatio();
        
        //return avgWheelRPM;
        float wheelAlpha = netTorque / wheelInertia;
        float deltaRPM = wheelAlpha * 60f / (2f * math.PI) * Time.fixedDeltaTime;
        avgWheelRPM += deltaRPM;

        float clampRPM = 10000f; // TODO: Test this. Delete or change if necessary.
        avgWheelRPM = Mathf.Clamp(avgWheelRPM, -clampRPM, clampRPM);
        */
    }
    /*
    public float CalculateWheelCircumferentialSpeed()
    {
        float omega = avgWheelRPM * 2f * Mathf.PI / 60f;
        return Mathf.Abs(omega * frontWheelRadius);
    }
    */
}
