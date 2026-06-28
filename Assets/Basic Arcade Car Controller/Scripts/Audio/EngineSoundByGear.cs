using UnityEngine;

public class EngineSoundByGear : MonoBehaviour
{
    BasicCarController car;
    BasicGearBox gearbox;
    BasicEngine engine;
    [SerializeField] AudioSource gearSource;

    [Header("Pitch Settings")]
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
        float targetPitch = Mathf.MoveTowards(minGearPitch, maxGearPitch, speed01);
        float targetVolume = Mathf.MoveTowards(0, maxVolume, speed01);

        gearSource.pitch = Mathf.MoveTowards(gearSource.pitch, targetPitch, pitchSmooth * Time.deltaTime);
        gearSource.volume = Mathf.MoveTowards(gearSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
    }
}