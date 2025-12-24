using UnityEngine;

public class AdvancedFlickerLight : MonoBehaviour
{
    [Header("References")]
    public Light targetLight;

    [Header("Base Settings")]
    public float baseIntensity = 2f;
    public float maxIntensity = 2.5f;

    [Header("Micro Flicker (Perlin)")]
    public float microSpeed = 12f;
    public float microAmplitude = 0.25f;

    [Header("Random Spikes")]
    public float spikeChance = 0.35f;
    public float spikeStrength = 1.5f;
    public float spikeDuration = 0.05f;

    [Header("Dim Outs")]
    public float dimChance = 0.05f;
    public float dimMin = 0.2f;
    public float dimMax = 0.5f;
    public float dimTime = 0.3f;

    [Header("Color Drift")]
    public float colorDrift = 0.03f;

    private float spikeTimer;
    private float dimTimer;
    private float microT;
    private bool isDimming;
    private float dimTarget;

    void Update(){
        if(!targetLight) return;

        microT += Time.deltaTime * microSpeed;

        float n = Mathf.PerlinNoise(microT, 0f) * microAmplitude;
        float finalIntensity = baseIntensity + n;

        if(Random.value < spikeChance * Time.deltaTime) spikeTimer = spikeDuration;
        if(spikeTimer > 0){
            finalIntensity += spikeStrength;
            spikeTimer -= Time.deltaTime;
        }

        if(!isDimming && Random.value < dimChance * Time.deltaTime){
            isDimming = true;
            dimTarget = Random.Range(dimMin, dimMax);
            dimTimer = dimTime;
        }

        if(isDimming){
            dimTimer -= Time.deltaTime;
            finalIntensity = Mathf.Lerp(finalIntensity, dimTarget, 1f - (dimTimer / dimTime));

            if(dimTimer <= 0) isDimming = false;
        }

        finalIntensity = Mathf.Clamp(finalIntensity, 0f, maxIntensity);
        targetLight.intensity = finalIntensity;

        float warmShift = (Mathf.PerlinNoise(microT * 0.5f, 10f) - 0.5f) * colorDrift;
        //targetLight.color = new Color(1f, 0.95f - warmShift, 0.9f - warmShift);
    }
}
