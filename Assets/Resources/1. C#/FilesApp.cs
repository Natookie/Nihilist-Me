using UnityEngine;
using Nova;
using Nova.TMP;
using TMPro;
using System.Collections.Generic;

public class FilesApp : MonoBehaviour
{
    [Header("REFERENCES")]
    public AppGestureHandler files;
    public GameObject[] pinnedChild;
    public GameObject[] tagsChild;

    private Dictionary<TextBlock, TextCache> cache = new Dictionary<TextBlock, TextCache>();

    [Header("GESTURE HANDLER")]
    public UIBlock2D pinnedBlock;
    public UIBlock2D tagsBlock;
    public UIBlock2D sideBar;

    [Range(0, 1)] public float minSideBar;
    [Range(0, 1)] public float maxSideBar;
    private bool cachedIsFullscreen = false;

    void Start(){
        pinnedBlock.AddGestureHandler<Gesture.OnPress>(OnPinnedPress);
        pinnedBlock.AddGestureHandler<Gesture.OnHover>(OnPinnedHover);
        pinnedBlock.AddGestureHandler<Gesture.OnUnhover>(OnPinnedUnhover);

        tagsBlock.AddGestureHandler<Gesture.OnPress>(OnTagsPress);
        tagsBlock.AddGestureHandler<Gesture.OnHover>(OnTagsHover);
        tagsBlock.AddGestureHandler<Gesture.OnUnhover>(OnTagsUnhover);

        CacheText(pinnedChild);
        CacheText(tagsChild);
        ApplySizeTag(files.isFullscreen);
    }

    void CacheText(GameObject[] parents){
        foreach(var go in parents){
            var texts = go.GetComponentsInChildren<TextBlock>(true);
            foreach(var tb in texts){
                if(cache.ContainsKey(tb)) continue;

                TMP_Text tmp = tb.GetComponent<TMP_Text>();
                if(tmp == null) continue;

                float baseSize = tmp.enableAutoSizing ? tmp.fontSizeMax : tmp.fontSize;

                cache.Add(tb, new TextCache{
                    rawText = tb.Text,
                    baseFontSize = baseSize
                });
            }
        }
    }

    void ApplySizeTag(bool fullscreen){
        foreach(var kvp in cache){
            TextBlock tb = kvp.Key;
            TextCache data = kvp.Value;

            float size = (fullscreen) ? data.baseFontSize * 1.4f : data.baseFontSize;
            tb.Text = $"<size={size}>{data.rawText}</size>";
        }

        sideBar.Size.X.Percent = (fullscreen) ? minSideBar : maxSideBar;
    }

    void Update(){
        if(files.isFullscreen != cachedIsFullscreen){
            cachedIsFullscreen = files.isFullscreen;
            ApplySizeTag(cachedIsFullscreen);
        }
    }

    bool pinnedVisible = true;
    bool tagsVisible = true;
    #region PINNED
    void OnPinnedPress(Gesture.OnPress evt){
        pinnedVisible ^= true;
        SetChildrenActive(pinnedChild, pinnedVisible);
    }
    void OnPinnedHover(Gesture.OnHover evt){
        pinnedBlock.Color = new Color32(128, 128, 128, 20);
    }
    void OnPinnedUnhover(Gesture.OnUnhover evt){
        pinnedBlock.Color = new Color32(128, 128, 128, 0);
    }
    #endregion

    #region TAGS
    void OnTagsPress(Gesture.OnPress evt){
        tagsVisible ^= true;
        SetChildrenActive(tagsChild, tagsVisible);
    }
    void OnTagsHover(Gesture.OnHover evt){
        tagsBlock.Color = new Color32(128, 128, 128, 20);
    }
    void OnTagsUnhover(Gesture.OnUnhover evt){
        tagsBlock.Color = new Color32(128, 128, 128, 0);
    }
    #endregion

    void SetChildrenActive(GameObject[] children, bool state){
        for(int i = 0; i < children.Length; i++){
            if(children[i] != null){
                if(i == 0) continue;
                children[i].SetActive(state);
            }
        }
    }
}

class TextCache
{
    public string rawText;
    public float baseFontSize;
}
