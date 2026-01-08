using UnityEngine;

public class DesktopAudioManager : MonoBehaviour
{
    public static DesktopAudioManager Instance;

    [Header("AUDIO CLIPS")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip minimizeSound;
    public AudioClip maximizeSound;
    [SerializeField] private AudioClip clickSound;
    [Space(10)]
    [SerializeField] private AudioClip[] keyPressSound;
    [SerializeField] private AudioClip keyboardPressSpace;
    [SerializeField] private AudioClip keyboardPressEnter;

    [Header("VOLUME VARIATIONS")]
    [SerializeField] float volumeVariation = .1f;
    [SerializeField] float pitchVariation = .05f;

    [Header("REFERENCES")]
    [SerializeField] private GameObject desktopScreen;

    AudioSource audioSource;

    void Awake(){
        if(Instance == null){
            Instance = this;
        }else{
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if(audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update(){
        if(!desktopScreen.activeSelf) return;

        //Type sound
        if(Input.anyKeyDown){
            if(Input.GetKeyDown(KeyCode.Space)) PlaySound(keyboardPressSpace);
            else if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) PlaySound(keyboardPressEnter);
            else PlayRandomKeySound();
        }

        // Click sound
        if(Input.GetMouseButtonDown(0)){
            PlaySound(clickSound);
        }
    }

    public void PlaySound(AudioClip clip){
        if(clip == null) return;
        audioSource.PlayOneShot(clip);
    }

    public void PlayRandomKeySound(){
        if(keyPressSound == null || keyPressSound.Length == 0) return;
        
        AudioClip randomClip = keyPressSound[Random.Range(0, keyPressSound.Length)];
        PlaySoundWithVariation(randomClip);
    }

     public void PlaySoundWithVariation(AudioClip clip){
        if(clip == null) return;
        
        float originalVolume = audioSource.volume;
        float originalPitch = audioSource.pitch;
        
        audioSource.volume = Mathf.Clamp(originalVolume + Random.Range(-volumeVariation, volumeVariation), 0f, 1f);
        audioSource.pitch = Mathf.Clamp(originalPitch + Random.Range(-pitchVariation, pitchVariation), 0.1f, 3f);
        
        audioSource.PlayOneShot(clip);
        
        audioSource.volume = originalVolume;
        audioSource.pitch = originalPitch;
    }
}