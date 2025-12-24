using UnityEngine;
using System.Collections;

public class Door : MonoBehaviour, IInteractable
{
    [Header("AUDIO")]
    public AudioSource audioSource;
    public AudioClip tvNoise;
    [Range(0f, 1f)]
    public float baseVolume;

    [Header("TIMING")]
    public Vector2 playDelayRange;
    public float fadeSpeed = 1.5f;
    public float cooldownTime = 600f;

    private bool playerInside;
    private bool isPlaying;
    private bool onCD;

    private Coroutine fadeRoutine;
    private Coroutine playRoutine;

    void Start(){
        if(audioSource != null) audioSource.volume = 0f;
    }

    void OnTriggerEnter2D(Collider2D coll){
        if(!coll.CompareTag("Player")) return;
        playerInside = true;

        if(!isPlaying && !onCD && playRoutine == null) playRoutine = StartCoroutine(PlayTV());
        
        FadeTo(baseVolume);
    }

    void OnTriggerExit2D(Collider2D coll){
        if(!coll.CompareTag("Player")) return;

        playerInside = false;
        FadeTo(0f);
    }

    IEnumerator PlayTV(){
        float delay = Random.Range(playDelayRange.x, playDelayRange.y);
        yield return new WaitForSeconds(delay);

        if(!playerInside){
            playRoutine = null;
            yield break;
        }

        if(tvNoise == null || audioSource == null){
            playRoutine = null;
            yield break;
        }

        isPlaying = true;

        audioSource.clip = tvNoise;
        audioSource.Play();

        StartCoroutine(WaitForFinish());
        playRoutine = null;
    }

    IEnumerator WaitForFinish(){
        yield return new WaitWhile(() => audioSource.isPlaying);

        isPlaying = false;
        onCD = true;

        yield return new WaitForSeconds(cooldownTime);
        onCD = false;
    }

    void FadeTo(float targetVolume){
        if(fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeVolume(targetVolume));
    }

    IEnumerator FadeVolume(float target){
        while(!Mathf.Approximately(audioSource.volume, target)){
            audioSource.volume = Mathf.MoveTowards(
                audioSource.volume,
                target,
                Time.deltaTime * fadeSpeed
            );

            yield return null;
        }

        fadeRoutine = null;
    }

    public void Interact(){
        // intentional
    }

    public string GetPrompt() => "Escape.";
}
