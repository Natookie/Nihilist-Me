using UnityEngine;

public class GoonMonologue : MonoBehaviour, IMouseInteractable
{
    [Header("REFRERENCES")]
    public Sprite[] goonSprites = new Sprite[2];
    public SpriteRenderer sr;

    public AudioSource audioSource;
    public AudioClip princessSFX;
    private bool isPlayed;

    private int counter = 1;
    private string[] monologueText = {
        "I should stop looking... What the fuck.",
        "Australian pop idols… not exactly my thing originally.",
        "But damn… Australian pop idols weren’t supposed to do this to me.",
        "Some nights I catch myself staring… longer than I should.",
        "Her body… too easy to imagine, too tempting.",
        "God, every curve burns in my head… can’t stop picturing it.",
        "\"Ayla River\"… found her by accident, now she’s everywhere in my mind.",
        "Can’t stop imagining her like she’s right here.",
        "I want to reach out, touch, feel… can’t shake it.",
        "It’s twisting inside me… I shouldn’t, but I want it anyway."
    };

    public string GetMonologue(){
        if(counter == monologueText.Length && !isPlayed){
            audioSource.PlayOneShot(princessSFX); 
            isPlayed = true;
            sr.sprite = goonSprites[1];
        }
        return monologueText[counter >= (monologueText.Length) ? 0 : counter++];
    }
}
