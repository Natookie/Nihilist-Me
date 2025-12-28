using UnityEngine;
using System.Collections;
using UnityEngine.Audio;

public enum AudioChannel
{
    Master,
    Music,
    SFX,
    Ambience,
    UI
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    public AudioMixer mixer;

    [Header("SOUND SETTING")]
    public AudioSource backgroundMusic;
    public AudioMixerGroup musicGroup;

    [Header("FADE")]
    public float fadeSpeed = .5f;

    Coroutine musicFade;
    Coroutine sfxFade;
    Coroutine ambienceFade;
    Coroutine uiFade;

    bool musicEnabled = true;
    bool sfxEnabled = true;
    bool ambienceEnabled = true;
    bool uiEnabled = true;

    public AudioSource[] sources;

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start(){
        sources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        if(backgroundMusic != null){
            if(musicGroup != null)
                backgroundMusic.outputAudioMixerGroup = musicGroup;

            backgroundMusic.loop = true;
            backgroundMusic.Play();
            SetVolume(AudioChannel.Music, 1f);
        }
    }

    public bool CanPlay(AudioChannel channel){
        switch(channel){
            case AudioChannel.Music: return musicEnabled;
            case AudioChannel.SFX: return sfxEnabled;
            case AudioChannel.Ambience: return ambienceEnabled;
            case AudioChannel.UI: return uiEnabled;
            default: return true;
        }
    }

    public void EnableChannel(AudioChannel channel, bool enabled){
        switch(channel){
            case AudioChannel.Music:
                musicEnabled = enabled;
                StartFade(AudioChannel.Music, enabled);
                break;

            case AudioChannel.SFX:
                sfxEnabled = enabled;
                StartFade(AudioChannel.SFX, enabled);
                break;

            case AudioChannel.Ambience:
                ambienceEnabled = enabled;
                StartFade(AudioChannel.Ambience, enabled);
                break;

            case AudioChannel.UI:
                uiEnabled = enabled;
                StartFade(AudioChannel.UI, enabled);
                break;
        }
    }

    public void ResetAllChannel()
    {
        // Clear all coroutine
        musicFade = null;
        sfxFade = null;
        ambienceFade = null;
        uiFade = null;
        // Set all channels to enable
        musicEnabled = true;
        sfxEnabled = true;
        ambienceEnabled = true;
        uiEnabled = true;
        // Reset all mixer volumes
        mixer.SetFloat("MusicVolume", 1.0f);
        mixer.SetFloat("MasterVolume", 1.0f);
        mixer.SetFloat("SFXVolume", 1.0f);
        mixer.SetFloat("AmbienceVolume", 1.0f);
        mixer.SetFloat("UIVolume", 1.0f);
    }

    public void SetVolume(AudioChannel channel, float value){
        float db = LinearToDb(Mathf.Clamp01(value));

        switch(channel){
            case AudioChannel.Music: mixer.SetFloat("MusicVolume", db); break;
            case AudioChannel.Master: mixer.SetFloat("MasterVolume", db); break;
            case AudioChannel.SFX: mixer.SetFloat("SFXVolume", db); break;
            case AudioChannel.Ambience: mixer.SetFloat("AmbienceVolume", db); break;
            case AudioChannel.UI: mixer.SetFloat("UIVolume", db); break;
        }
    }

    void StartFade(AudioChannel channel, bool enabled){
        switch(channel){
            case AudioChannel.Music:
                if(musicFade != null) StopCoroutine(musicFade);
                musicFade = StartCoroutine(FadeMixer("MusicVolume", enabled));
                break;

            case AudioChannel.SFX:
                if(sfxFade != null) StopCoroutine(sfxFade);
                sfxFade = StartCoroutine(FadeMixer("SFXVolume", enabled));
                break;

            case AudioChannel.Ambience:
                if(ambienceFade != null) StopCoroutine(ambienceFade);
                ambienceFade = StartCoroutine(FadeMixer("AmbienceVolume", enabled));
                break;

            case AudioChannel.UI:
                if(uiFade != null) StopCoroutine(uiFade);
                uiFade = StartCoroutine(FadeMixer("UIVolume", enabled));
                break;
        }
    }

    IEnumerator FadeMixer(string param, bool enabled){
        mixer.GetFloat(param, out float currentDb);

        float currentLinear = DbToLinear(currentDb);
        float targetLinear = enabled ? 1f : 0f;

        while(!Mathf.Approximately(currentLinear, targetLinear)){
            currentLinear = Mathf.MoveTowards(
                currentLinear,
                targetLinear,
                Time.deltaTime * fadeSpeed
            );

            mixer.SetFloat(param, LinearToDb(currentLinear));
            yield return null;
        }

        mixer.SetFloat(param, LinearToDb(targetLinear));
    }

    float LinearToDb(float value){
        return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
    }

    float DbToLinear(float db){
        return Mathf.Pow(10f, db / 20f);
    }
}
