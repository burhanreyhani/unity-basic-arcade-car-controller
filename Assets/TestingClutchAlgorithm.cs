using UnityEngine;

public class TestingClutchAlgorithm : MonoBehaviour
{
    Rigidbody rb;

    [Range(0, 300)]
    [SerializeField] float engineOmega = 300f;

    [Range(0, 1)]
    [SerializeField] float clutch = 0;

    float gearboxOmega;

    float firstGearRT = 3.9f;
    float finalDriveRT = 4.1f;

    float wheelInertia = 25f;
    float dirvetrainInertia = 20f;

    float clutchLoadTorque = 30; // TODO: I don't know how to calculate this yet. But this must affect the engine.

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float clutchEngage = 1 - clutch;
        float deltaOmega = engineOmega - gearboxOmega;

        //float totalInertia = clutch < 1 ? wheelInertia + dirvetrainInertia * clutchEngage : 0;
        
        if (clutch == 1)
        {            
            gearboxOmega = rb.linearVelocity.magnitude;
        }

        if (deltaOmega < 1 && clutch < 1)
        {
            gearboxOmega = (engineOmega - clutchLoadTorque) * clutchEngage;
        }
        else if (clutch < 1 && engineOmega > 0)
        {
            gearboxOmega += (engineOmega * clutchEngage); // - clutchLoadTorque) / totalInertia;
        }

        float totalRatio = firstGearRT * finalDriveRT;
        rb.AddForce(transform.forward * gearboxOmega * totalRatio);

        Debug.Log("EO: " + engineOmega);
        Debug.Log("DO: " + deltaOmega);
        Debug.Log("GO: " + gearboxOmega);
        Debug.Log("Speed: " + rb.linearVelocity.magnitude * 3.6f);
    }
}
