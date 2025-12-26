using UnityEngine;
using Nova;
using Nova.TMP;
using System.Collections.Generic;

public class AsianBabyGirl : MonoBehaviour
{
    [Header("REFERENCES")]
    public string resourcePath = "txt/AsianBabyGirl";
    public AppGestureHandler corner;
    public TextBlock targetText;

    private float fps = 30f;
    private bool loop = true;

    private List<string> frames = new List<string>();
    private int currentFrame = 0;
    private float timer = 0f;

    private bool cachedIsFullscreen = false;

    void Start(){
        LoadFrames();
        if(frames.Count > 0) targetText.Text = frames[0];
    }

    void Update(){
        if(frames.Count == 0) return;
        if(corner.State == AppWindowState.Closed) return;

        timer += Time.deltaTime;
        if(timer >= 1f / fps){
            timer = 0f;
            AdvanceFrame();
        }
    }

    void LoadFrames(){
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if(textAsset == null) return;

        string raw = textAsset.text.Replace("\r\n", "\n");
        string[] rawFrames = raw.Split(new string[] { "\n\n" }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach(string frame in rawFrames) frames.Add(frame.TrimEnd());
    }

    void AdvanceFrame(){
        currentFrame++;

        if(currentFrame >= frames.Count){
            if(loop) currentFrame = 0;
            else currentFrame = frames.Count-1;
        }

        int newSize = (!corner.isFullscreen) ? 160 : 320;
        if(corner.isFullscreen != cachedIsFullscreen) cachedIsFullscreen = corner.isFullscreen;
        targetText.Text = $"<size={newSize}>{frames[currentFrame]}</size>";
    }
}

