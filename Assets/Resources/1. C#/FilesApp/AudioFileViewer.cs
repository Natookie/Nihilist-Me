using UnityEngine;
using Nova;

public class AudioFileViewer : MonoBehaviour
{
    [Header("UI")]
    public TextBlock fileNameText;
    public AudioSource audioSource;

    public void Show(AudioClip clip, string fileName){
        if(clip == null) return;

        gameObject.SetActive(true);

        if(fileNameText != null) fileNameText.Text = fileName;
        if(audioSource != null){
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    void OnDisable(){
        if(audioSource != null) audioSource.Stop();
    }
}
