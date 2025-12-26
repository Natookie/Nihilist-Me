using UnityEngine;
using System.Collections;
using Nova;

public class CommentData : MonoBehaviour
{
    public int upCount, downCount;
    public UIBlock2D upVote, downVote;
    public TextBlock upText, downText;
    private float duration = 0;
    private float tick;
    private float downvoteChance;

    public void SetData(int a, string type = "whatavercuh"){
        upCount = 0;
        downCount = 0;

        float t = Mathf.Clamp01(a / 30f);
        downvoteChance = Mathf.Lerp(.9f, .1f, t);
        if(type == "AI") downvoteChance = (1 - downvoteChance);

        duration = Random.Range(10f, 25f);
    }

    void Update(){
        if(duration <= 0) return;

        duration -= Time.deltaTime;
        tick -= Time.deltaTime;
        if(tick > 0) return;
        tick = Random.Range(.1f, .6f);

        if(Random.value < downvoteChance) downCount++;
        else upCount++;

        upText.Text = upCount.ToString();
        downText.Text = downCount.ToString();
    }
}
