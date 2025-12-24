using UnityEngine;

public class PlainMonologue : MonoBehaviour, IMouseInteractable
{
    public string[] monologueText;
    int lastIndex = -1;

    // static AudioSource sfxSource;
    // static AudioClip monologueSFX;

    /*
    void Awake(){
        if(sfxSource == null){
            GameObject go = new GameObject("Global_Monologue_SFX");
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.spatialBlend = 0f;
            sfxSource.volume = .25f;
            DontDestroyOnLoad(go);
        }

        if(monologueSFX == null) monologueSFX = Resources.Load<AudioClip>("6. Audio/SFX/UI Click");
    }

    void PlaySFX(){
        if(monologueSFX == null) return;
        sfxSource.pitch = Random.Range(0.95f, 1.05f);
        sfxSource.PlayOneShot(monologueSFX);
    }*/

    public string GetMonologue(){
        // PlaySFX();
        if(monologueText.Length == 1) return monologueText[0];
        
        int idx;
        do{
            idx = Random.Range(0, monologueText.Length);
        }
        while (idx == lastIndex);

        lastIndex = idx;
        return monologueText[idx];
    }
}
