using TMPro;
using UnityEngine;

public class TelemetryScreen : MonoBehaviour
{
    [SerializeField] BasicWheel basicWheel;

    public TMP_Text wheelRPMText;
    public TMP_Text wheelTorqueText;
    /*
    void Update()
    {
        if (wheelRPMText.text != null)
        {
            wheelRPMText.text = "Front Left RPM: " + basicWheel.flRPM.ToString("F1") + "RPM\n" +
                                "Front Right RPM: " + basicWheel.frRPM.ToString("F1") + "RPM\n" +
                                "Rear Left RPM: " + basicWheel.rlRPM.ToString("F1") + "RPM\n" +
                                "Rear Right RPM: " + basicWheel.rrRPM.ToString("F1") + "RPM";
        }

        if (wheelTorqueText.text != null)
        {
            wheelTorqueText.text =  "Front Left Trq: " + basicWheel.frontLeftTRQ.ToString("F1") + "Trq\n" +
                                    "Front Right Trq: " + basicWheel.frontRightTRQ.ToString("F1") + "Trq\n" +
                                    "Rear Left Trq: " + basicWheel.rearLeftTRQ.ToString("F1") + "Trq\n" +
                                    "Rear Right Trq: " + basicWheel.rearRightTRQ.ToString("F1") + "Trq";
        }
    }
    */
}
