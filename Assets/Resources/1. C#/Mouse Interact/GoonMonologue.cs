using UnityEngine;

public class GoonMonologue : MonoBehaviour, IMouseInteractable
{
    public AudioSource audioSource;
    public AudioClip princessSFX;
    private bool isPlayed;

    private int counter = 1;
    private string[] monologueText = {
        "I should stop looking...",
        "Australian pop idols… not exactly my thing originally.",
        "Some nights I just stare at it longer than I should.",
        "When the nights got lonely, she was the easiest distraction to reach for",
        "Yeah, this poster… Let’s just say teenage hormones and a sexy body were a dangerous combo.",
        "\"Ayla River\". Found her by accident during one of those insomnia nights.",
        "Tes1",
        "Tes2",
        "Tes3"
    };

    public string GetMonologue(){
        if(counter == monologueText.Length && !isPlayed) {audioSource.PlayOneShot(princessSFX); isPlayed = true;}
        return monologueText[counter >= (monologueText.Length) ? 0 : counter++];
    }
}
