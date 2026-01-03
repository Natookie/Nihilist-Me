using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Computer : MonoBehaviour, IInteractable
{
    [Header("ANIMATION")]
    public Renderer screenRender;
    public float minCRTEmission = .6f;
    public float maxCRTEmission = 1.2f;
    public float pulseSpeed = .4f;
    public float noiseSpeed = .8f;
    [Space(10)]
    public Renderer greenRender;
    public Renderer redRender;
    public float minOrnamentIntensity = .5f;
    public float maxOrnamentIntensity = 2.5f;

    [Header("SUBTLE VARIATION")]
    public float scanIntensityVariance = .05f;
    public float lineDensityVariance = .03f;

    float noiseOffset;

    [Header("SOUND EFFECT")]
    public AudioSource audioSource;
    public AudioClip computerHumming;

    [Header("REFERENCES")]
    public CamFoll camFoll;
    public Chair chair;
    GameManager gm;

    bool isOpen = false;
    Material screenMat;
    Material greenMat;
    Material redMat;

    Color baseGreenEmission;
    Color baseRedEmission;

    void Awake(){
        noiseOffset = Random.Range(0f, 100f);
    }

    void Start(){
        gm = GameManager.Instance;

        if(screenRender != null) screenMat = screenRender.material;

        if(greenRender != null){
            greenMat = greenRender.material;
            greenMat.EnableKeyword("_EMISSION");
            baseGreenEmission = greenMat.GetColor("_EmissionColor");
        }

        if(redRender != null){
            redMat = redRender.material;
            redMat.EnableKeyword("_EMISSION");
            baseRedEmission = redMat.GetColor("_EmissionColor");
        }

        if(audioSource != null && computerHumming != null){
            audioSource.clip = computerHumming;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
        }
    }

    void Update(){
        AnimateScreen();
        AnimateOrnament();
        HandleHumming();
    }

    void AnimateScreen(){
        if(screenMat == null) return;

        float pulse = Mathf.Sin(Time.time * pulseSpeed) * .5f + .5f;

        float noise = Mathf.PerlinNoise(
            Time.time * noiseSpeed,
            noiseOffset
        );

        float t = pulse * noise;

        float emission = Mathf.Lerp(minCRTEmission, maxCRTEmission, t);
        screenMat.SetFloat("_EmmisionStrength", emission);

        screenMat.SetFloat(
            "_ScanIntensity",
            1f + (noise - .5f) * scanIntensityVariance
        );

        screenMat.SetFloat(
            "_LineDensity",
            1f + (pulse - .5f) * lineDensityVariance
        );
    }

    void AnimateOrnament(){
        if(greenMat == null || redMat == null) return;

        float time = Time.time;

        float greenPulse = Mathf.Sin(time * (pulseSpeed * .6f)) * .5f + .5f;
        float greenNoise = Mathf.PerlinNoise(
            time * (noiseSpeed * .5f),
            noiseOffset + 10f
        );

        float greenT = greenPulse * greenNoise;

        float greenEmission = Mathf.Lerp(
            minOrnamentIntensity,
            maxOrnamentIntensity * .7f,
            greenT
        );

        greenMat.SetColor("_EmissionColor", baseGreenEmission * greenEmission);

        float redPulse = Mathf.Sin(time * (pulseSpeed * .9f) + 1.3f) * .5f + .5f;
        float redNoise = Mathf.PerlinNoise(
            time * noiseSpeed,
            noiseOffset + 37f
        );

        float redT = Mathf.Clamp01(redPulse * redNoise);

        float redEmission = Mathf.Lerp(
            minOrnamentIntensity * .8f,
            maxOrnamentIntensity,
            redT
        );

        redMat.SetColor("_EmissionColor", baseRedEmission * redEmission);
    }

    void HandleHumming(){
        if(audioSource == null || computerHumming == null) return;
         bool canPlay = AudioManager.Instance.CanPlay(AudioChannel.Ambience);

        if(canPlay){ if(!audioSource.isPlaying) audioSource.Play(); }
        else{ if(audioSource.isPlaying) audioSource.Pause(); }
    }

    public void InteractImmediately()
    {
        isOpen ^= true;
        gm.isAnyUiActive = isOpen;
        chair.ResetRot();
        chair.Move(isOpen);
        camFoll.TransistionImmediately();
    }

    public bool CanInteract() => true;

    public void Interact(){
        isOpen ^= true;
        gm.isAnyUiActive = isOpen;
        camFoll.StartTransition(isOpen);
        chair.ResetRot();
        chair.Move(isOpen);
    }

    public string GetPrompt() => "Use Computer";
}
