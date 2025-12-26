using UnityEngine;

public class Window : MonoBehaviour
{
    [Header("LIGHT SETTING")]
    public Light windowLight;
    public Color32[] dayCyleColor = new Color32[5];

    [Header("RAIN")]
    public AudioSource audioSource;
    public AudioClip calmRain;
    public AudioClip thunderRain;

    [Header("REFERENCES")]
    public SleepingMat sleepingMat;
    public Transform playerPos;

    private enum RainType { None, Calm, Thunder }
    private RainType currentRain = RainType.None;

    void Start(){
        UpdateDay();
    }

    void Update(){
        float dist = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(playerPos.position.x, 0, playerPos.position.z));

        float maxDistance = 10f;
        float volume = Mathf.Clamp01(1f - (dist / maxDistance));
    audioSource.volume = volume;
    }

    public void UpdateDay(){
        if(sleepingMat == null || windowLight == null) return;

        int dayIndex = sleepingMat.dayCount;
        dayIndex = Mathf.Clamp(dayIndex, 0, dayCyleColor.Length-1);

        int colorIndex = Mathf.Clamp(sleepingMat.dayCount - 1, 0, dayCyleColor.Length - 1);
        windowLight.color = dayCyleColor[colorIndex];

        switch(dayIndex){
            case 1: case 2: case 3: SetRain(RainType.None); break;
            case 4: SetRain(RainType.Calm); break;
            case 5: SetRain(RainType.Thunder); break;
        }
    }

    void SetRain(RainType type){
        if(audioSource == null) return;

        currentRain = type;
        audioSource.Stop();

        switch(type){
            case RainType.None:
                audioSource.clip = null;
                audioSource.volume = 0;
                break;
            case RainType.Calm:
                audioSource.clip = calmRain;
                audioSource.volume = 1f;
                break;
            case RainType.Thunder:
                audioSource.clip = thunderRain;
                audioSource.volume = 1f;
                break;
        }

        if(type != RainType.None && audioSource.clip != null) audioSource.Play();
    }
}
