using System;

public class Drivetrain
{
    Gearbox gearbox;

    IDifferential rearDiff;
    IDifferential frontDiff;
    IDifferential centerDiff; // This for AWD

    float efficiency;
    float downstreamTorque;
    float avgDrivenWheelRPM;

    public DrivetrainType driveType;

    public enum DrivetrainType { RWD, FWD, AWD }

    public Drivetrain(Gearbox gearbox, DrivetrainType driveType)
    {
        this.gearbox = gearbox;
        this.driveType = driveType;
    }

    public void SetDifferential(IDifferential diff, DiffPosition position)
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

    public enum DiffPosition { Rear, Front, Center }

    public void DistributeTorque(float leftFrontRPM, float rightFrontRPM, float leftRearRPM, float rightRearRPM,
out float leftFrontTorque, out float rightFrontTorque, out float leftRearTorque, out float rightRearTorque)
    {
        float wheelTorque = DrivetrainEfficiency(gearbox.ApplyTorque(avgDrivenWheelRPM));
        leftFrontTorque = rightFrontTorque = leftRearTorque = rightRearTorque = 0f;

        float distEvenly = 0.5f;
        float distQuarter = 0.25f;

        // TODO: If avgDrivenWheelRPM calc creates a visible bug, then calculate it in different switch statement right before gearbox.ApplyTorque() methot.
        switch (driveType)
        {
            case DrivetrainType.RWD:
                avgDrivenWheelRPM = (leftRearRPM + rightRearRPM) * distEvenly;
                rearDiff.DistributeTorque(wheelTorque, leftRearRPM, rightRearRPM, out leftRearTorque, out rightRearTorque);
                break;
            
            case DrivetrainType.FWD:
                avgDrivenWheelRPM = (leftFrontRPM + rightFrontRPM) * distEvenly;
                frontDiff.DistributeTorque(wheelTorque, leftFrontRPM, rightFrontRPM, out leftFrontTorque, out rightFrontTorque);
                break;

            case DrivetrainType.AWD: // This calls three methots because center distributes front and rear, then those methots split torque evenly.
                float frontTorque, rearTorque;
                avgDrivenWheelRPM = (leftFrontRPM + rightFrontRPM + leftRearRPM + rightRearRPM) * distQuarter;
                centerDiff.DistributeTorque(wheelTorque, leftFrontRPM + rightFrontRPM, leftRearRPM + rightRearRPM, out frontTorque, out rearTorque);
                frontDiff.DistributeTorque(frontTorque, leftFrontRPM, rightFrontRPM, out leftFrontTorque, out rightFrontTorque);
                rearDiff.DistributeTorque(rearTorque, leftRearRPM, rightRearRPM, out leftRearTorque, out rightRearTorque);
                break;
            
            default:
                avgDrivenWheelRPM = 0f;
                break;
        }
    }

    public float CalculateDownstreamTrq(float leftFrontRPM, float rightFrontRPM, float leftRearRPM, float rightRearRPM)
    {
        switch (driveType)
        {
            case DrivetrainType.RWD:
                downstreamTorque = rearDiff.DiffLoad(leftRearRPM, rightRearRPM);
                return downstreamTorque;
            
            case DrivetrainType.FWD:
                downstreamTorque = frontDiff.DiffLoad(leftFrontRPM, rightFrontRPM);
                return downstreamTorque;
            
            case DrivetrainType.AWD:
                float rearLoad = rearDiff.DiffLoad(leftRearRPM, rightRearRPM);
                float frontLoad = frontDiff.DiffLoad(leftFrontRPM, rightFrontRPM);
                downstreamTorque = centerDiff.DiffLoad(frontLoad, rearLoad);
                return downstreamTorque;

            default:
                return 0f;
        }
    }

    public float DrivenWheelRadius(float frontRadius, float rearRadius)
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

    public float DrivetrainEfficiency(float inputTorque)
    {
        return inputTorque * efficiency;
    }

    public float DrivetrainLoad()
    {
        if (gearbox.currentGear == 0)
        {
            return 0f;
        }

        float totalRatio = gearbox.TotalRatio();

        if (MathF.Abs(totalRatio) < 0.0001f)
        {
            return 0f;
        } 
        
        return downstreamTorque / totalRatio;
    }

    public void SetEfficiency(float drvEff)
    {
        efficiency = drvEff;
    }
}
