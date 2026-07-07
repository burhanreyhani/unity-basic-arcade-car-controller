using UnityEngine;

public class BasicDrivetrain : MonoBehaviour
{
    Rigidbody rb;

    BasicGearBox basicGearBox;
    BasicCarController basicCarController;

    IDifferential rearDiff;
    IDifferential frontDiff;
    IDifferential centerDiff;

    [Header("Drivetrain config")]
    [SerializeField] DrivetrainType driveType = DrivetrainType.RWD;
    [SerializeField] DifferentialType diffType = DifferentialType.Open;
    [SerializeField] float efficiency = 0.85f;

    [Header("LSD Settings (Only applies when LSD selected)")]
    [SerializeField] float lockingTorque = 400f;
    [SerializeField] float lockThreshold = 60f;
    [SerializeField] float lockStrength = 0.5f;

    enum DrivetrainType { RWD, FWD, AWD }
    enum DiffPosition { Rear, Front, Center }
    enum DifferentialType { Open, Locked, LSD }

    public float avgDrivenWheelRPM { get; private set; }
    public float downstreamTorque { get; private set; }

    void Awake()
    {
        BuildDriveTrain();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        basicGearBox = GetComponent<BasicGearBox>();
        basicCarController = GetComponent<BasicCarController>();
    }

    void SetDifferential(IDifferential diff, DiffPosition position)
    {
        switch (position)
        {
            case DiffPosition.Rear:
                rearDiff = diff;
                break;

            case DiffPosition.Front:
                frontDiff = diff;
                break;
            
            case DiffPosition.Center:
                centerDiff = diff;
                break;
            
            default:
                rearDiff = diff;
                break;
        }
    }
    public void DistributeTorque(float leftFrontRPM, float rightFrontRPM, float leftRearRPM, float rightRearRPM,
out float leftFrontTorque, out float rightFrontTorque, out float leftRearTorque, out float rightRearTorque)
    {
        float wheelTorque = DrivetrainEfficiency(basicGearBox.ApplyTorque(avgDrivenWheelRPM));
        leftFrontTorque = rightFrontTorque = leftRearTorque = rightRearTorque = 0f;

        float distEvenly = 0.5f;
        float distQuarter = 0.25f;

        // TODO: If avgDrivenWheelRPM calc creates a visible bug, then calculate it in different switch statement right before gearbox.ApplyTorque() methot.
        switch (driveType)
        {
            case DrivetrainType.RWD:
                avgDrivenWheelRPM = (leftRearRPM + rightRearRPM) * distEvenly;
                rearDiff.DistributeTorque(wheelTorque, leftRearRPM, rightRearRPM, out leftRearTorque, out rightRearTorque);
                downstreamTorque = leftRearTorque + rightRearTorque;
                break;
            
            case DrivetrainType.FWD:
                avgDrivenWheelRPM = (leftFrontRPM + rightFrontRPM) * distEvenly;
                frontDiff.DistributeTorque(wheelTorque, leftFrontRPM, rightFrontRPM, out leftFrontTorque, out rightFrontTorque);
                downstreamTorque = leftFrontTorque + rightFrontTorque;
                break;

            case DrivetrainType.AWD: // This calls three methots because center distributes front and rear, then those methots split torque evenly.
                float frontTorque, rearTorque;
                avgDrivenWheelRPM = (leftFrontRPM + rightFrontRPM + leftRearRPM + rightRearRPM) * distQuarter;
                centerDiff.DistributeTorque(wheelTorque, leftFrontRPM + rightFrontRPM, leftRearRPM + rightRearRPM, out frontTorque, out rearTorque);
                frontDiff.DistributeTorque(frontTorque, leftFrontRPM, rightFrontRPM, out leftFrontTorque, out rightFrontTorque);
                rearDiff.DistributeTorque(rearTorque, leftRearRPM, rightRearRPM, out leftRearTorque, out rightRearTorque);
                downstreamTorque = leftFrontTorque + rightFrontTorque + leftRearTorque + rightRearTorque;
                break;
            
            default:
                avgDrivenWheelRPM = 0f;
                break;
        }
    }

    public float DrivenWheelRadius(float frontRadius, float rearRadius) // TODO: Not used yet
    {
        float divide = 0.5f;
        return driveType switch
        {
            DrivetrainType.RWD => rearRadius,
            DrivetrainType.FWD => frontRadius,
            DrivetrainType.AWD => (frontRadius + rearRadius) * divide,
            _ => rearRadius
        };
    }

    float DrivetrainEfficiency(float inputTorque)
    {
        // inputTorque is ApplyTorque().
        return inputTorque * efficiency;
    }

    void BuildDriveTrain()
    {
        IDifferential diff = diffType switch
        {
            DifferentialType.Open => new OpenDifferential(),
            DifferentialType.Locked => new LockedDifferential(),
            DifferentialType.LSD => new LimitedSlipDifferential(lockingTorque, lockThreshold, lockStrength),
            _ => new OpenDifferential()
        };

        if (driveType == DrivetrainType.AWD)
        {
            SetDifferential(new OpenDifferential(), DiffPosition.Center);
            SetDifferential(diff, DiffPosition.Front);
            SetDifferential(diff, DiffPosition.Rear);
        }
        else
        {
            DiffPosition pos = driveType == DrivetrainType.FWD
                ? DiffPosition.Front
                : DiffPosition.Rear;

            SetDifferential(diff, pos);
        }
    }
}
