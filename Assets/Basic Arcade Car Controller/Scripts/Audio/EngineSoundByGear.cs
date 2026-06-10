using UnityEngine;

public class EngineSoundByGear : MonoBehaviour
{
    BasicCarController car;
    BasicGearBox gearbox;
    BasicEngine engine;
    [SerializeField] AudioSource gearSource;

    [Header("Pitch Settings")]
    public float idlePitch = 0.9f;
    public float minGearPitch = 0.95f;
    public float maxGearPitch = 2.4f;
    public float pitchSmooth = 3f;

    [Header("Volume Settings")]
    public float maxVolume = 0.4f;
    public float fadeSpeed = 5f;

    void Start()
    {
        engine = GetComponentInParent<BasicEngine>();

        gearSource.volume = 0;
        gearSource.loop = true;
        gearSource.Play();
    }

    void Update()
    {
        
        float speed01 = Mathf.Clamp01(engine.currentRPM / engine.GetMaxRPM());
        float targetPitch = Mathf.Lerp(minGearPitch, maxGearPitch, speed01);
        float targetVolume = Mathf.Lerp(0, maxVolume, speed01);

        gearSource.pitch = Mathf.Lerp(gearSource.pitch, targetPitch, pitchSmooth * Time.deltaTime);
        gearSource.volume = Mathf.Lerp(gearSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
    }
}