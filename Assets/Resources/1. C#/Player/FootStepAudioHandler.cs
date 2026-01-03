using UnityEngine;

public class FootstepAudioHandler : MonoBehaviour
{
    [Header("REFERENCES")]
    [SerializeField]private AudioSource footstepSource;
    [SerializeField]private Collider2D futonCollider;

    [Header("WOODEN STEPS")]
    [SerializeField]private AudioClip[] woodenFootstepSounds;
    [SerializeField]private AudioClip woodenCreakingSound;

    [Header("FUTON STEP")]
    [SerializeField]private AudioClip futonFootstepSound;

    [Header("SETTINGS")]
    [Range(0f, 1f)] public float baseFootstepVolume = .6f;
    [SerializeField]private float footstepInterval = .4f;
    [SerializeField]private float pitchVariation = .15f;
    [SerializeField]private float volumeVariation = .1f;

    bool isOnFuton;
    float footstepTimer;
    bool wasMoving;
    IMovable mover;

    void Awake(){
        mover = GetComponent<IMovable>();
    }

    void Update(){
        if(GameManager.Instance.isPaused) return;

        bool moving = mover.IsMoving;

        if(moving && !wasMoving) footstepTimer = footstepInterval * .5f;
        if(moving){
            footstepTimer += Time.deltaTime;
            if(footstepTimer >= footstepInterval){
                PlayFootstep();
                footstepTimer = 0f;
            }
        }

        wasMoving = moving;
    }

    void PlayFootstep(){
        if(!footstepSource || footstepSource.isPlaying) return;

        AudioClip clip = isOnFuton ? futonFootstepSound : GetRandomWoodenStep();
        if(!clip) return;

        footstepSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        footstepSource.volume = Mathf.Clamp(baseFootstepVolume * Random.Range(1f - volumeVariation, 1f + volumeVariation), 0f, 1f);
        footstepSource.PlayOneShot(clip);

        TryPlayWoodCreak();
    }

    AudioClip GetRandomWoodenStep(){
        if(woodenFootstepSounds == null || woodenFootstepSounds.Length == 0) return null;
        return woodenFootstepSounds[Random.Range(0, woodenFootstepSounds.Length)];
    }

    void TryPlayWoodCreak(){
        if(isOnFuton || !woodenCreakingSound) return;
        if(Random.value > .15f) return;

        footstepSource.pitch = Random.Range(.85f, .95f);
        footstepSource.volume = baseFootstepVolume * Random.Range(.5f, .65f);
        footstepSource.PlayOneShot(woodenCreakingSound);
    }

    void OnTriggerEnter2D(Collider2D other){
        if(other == futonCollider) isOnFuton = true;
    }

    void OnTriggerExit2D(Collider2D other){
        if(other == futonCollider) isOnFuton = false;
    }
}