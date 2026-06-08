using UnityEngine;
using TMPro;

public class Telemetry : MonoBehaviour
{
    //public CarController carController;
    public GameObject UI;
    public TMP_Text engineText;
    public TMP_Text drivetrainText;
    public TMP_Text wheelsTrqText;
    public TMP_Text wheelsRPMText;
    public TMP_Text wheelsFxText;
    public TMP_Text wheelsFyText;
    public TMP_Text slipRatioText;
    public TMP_Text netTrqText;

    /*
    void Update()
    {  
        engineText.text =
        "Engine Info:" +
        "\nRPM: " + carController.CurrentRPM.ToString() +
        "\nEngine Torque: " + carController.CurrentEngineTrq.ToString("F2") +
        "\nEngine Angular Accel: " + carController.engineDebug.angularAcc.ToString("F0") +
        "\nFriction: " + carController.engineDebug.friction.ToString("F1") +
        "\nReflected Inertia: " + carController.engineDebug.reflectedInert.ToString();

        drivetrainText.text =
        "Drivetrain Info:" +
        "\nCurrent Gear: " + ShowGears().ToString() +
        "\nClutch: " + carController.clutchVal.ToString() +
        "\nDrivetrain Load: " + carController.engineDebug.drivetrainLoad.ToString("F2");

        wheelsTrqText.text =
        "Wheel Torque:" +
        "\nFL TRQ: " + carController.flTrq.ToString() +
        "\nFR TRQ: " + carController.frTrq.ToString() +
        "\nRL TRQ: " + carController.rlTrq.ToString() +
        "\nRR TRQ: " + carController.rrTrq.ToString();

        wheelsRPMText.text =
        "Wheel RPM:" +
        "\nFL WRPM: " + carController.frontLeft.wheelRPM.ToString("F1") +
        "\nFR WRPM: " + carController.frontRight.wheelRPM.ToString("F1") +
        "\nRL WRPM: " + carController.rearLeft.wheelRPM.ToString("F1") +
        "\nRR WRPM: " + carController.rearRight.wheelRPM.ToString("F1");

        wheelsFxText.text =
        "Fx:" +
        "\nFL Fx: " + carController.flDebug.Fx.ToString("F1") +
        "\nFR Fx: " + carController.frDebug.Fx.ToString("F1") +
        "\nRL Fx: " + carController.rlDebug.Fx.ToString("F1") +
        "\nRR Fx: " + carController.rrDebug.Fx.ToString("F1");

        wheelsFyText.text =
        "Fy:" +
        "\nFL Fy: " + carController.flDebug.Fy.ToString("F1") +
        "\nFR Fy: " + carController.frDebug.Fy.ToString("F1") +
        "\nRL Fy: " + carController.rlDebug.Fy.ToString("F1") +
        "\nRR Fy: " + carController.rrDebug.Fy.ToString("F1");

        slipRatioText.text =
        "Slip Ratio:" +
        "\nFL SlipRT: " + carController.flDebug.slipRatio.ToString("F1") +
        "\nFR SlipRT: " + carController.frDebug.slipRatio.ToString("F1") +
        "\nRL SlipRT: " + carController.rlDebug.slipRatio.ToString("F1") +
        "\nRR SlipRT: " + carController.rrDebug.slipRatio.ToString("F1");

        netTrqText.text =
        "Net Wheel Torque:" +
        "\nFL Net Torque: " + carController.flDebug.netTorque.ToString("F1") +
        "\nFR Net Torque: " + carController.frDebug.netTorque.ToString("F1") +
        "\nRL Net Torque: " + carController.rlDebug.netTorque.ToString("F1") +
        "\nRR Net Torque: " + carController.rrDebug.netTorque.ToString("F1");
    }

    string ShowGears()
    {
        if (carController.CurrentGear == -1)
        {
            return "R";
        }
        else if (carController.CurrentGear == 0)
        {
            return "N";
        }
        
        return carController.CurrentGear.ToString();
    }
    */
}
