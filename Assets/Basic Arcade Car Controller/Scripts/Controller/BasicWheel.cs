using UnityEngine;

public class BasicWheel : MonoBehaviour
{
    Rigidbody rb;

    BasicEngine basicEngine;
    BasicGearBox basicGearBox;

    [SerializeField] float frontWheelRadius = 0.37f;
    [SerializeField] float rearWheeRadius = 0.37f;

    [SerializeField] Transform frontLeftWheel;
    [SerializeField] Transform frontRightWheel;
    [SerializeField] Transform rearLeftWheel;
    [SerializeField] Transform rearRightWheel;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        basicEngine = GetComponent<BasicEngine>();
        basicGearBox = GetComponent<BasicGearBox>();
    }

    void FixedUpdate()
    {
        float wheelRPM = CalculateWheelRPM();

        //float throttle = basicEngine.throttleVal;
    
        //rb.AddForceAtPosition(transform.forward * basicGearBox.ApplyTorque(wheelRPM), frontLeftWheel.position);
        //rb.AddForceAtPosition(transform.forward * basicGearBox.ApplyTorque(wheelRPM), frontRightWheel.position);
    
        float distributedTorque = basicGearBox.ApplyTorque(wheelRPM) / 2;
    
        rb.AddForceAtPosition(transform.forward * distributedTorque * 0.85f, frontLeftWheel.position - frontLeftWheel.up);
        rb.AddForceAtPosition(transform.forward * distributedTorque * 0.85f, frontRightWheel.position - frontRightWheel.up);
    }

    float CalculateWheelRPM()  // TODO: Temporary calculation.
    {
        float avgWheelRPM = rb.linearVelocity.magnitude / frontWheelRadius * 60f / (2f * Mathf.PI);
        /*
        if (basicEngine.currentRPM == 0) return 0;

        float totalRatio = basicGearBox.TotalRatio();

        if (Mathf.Approximately(totalRatio, 0f)) 
        {
            return 0f; 
        }

        return basicEngine.currentRPM / basicGearBox.TotalRatio();
        */
        return avgWheelRPM;
    }
}
