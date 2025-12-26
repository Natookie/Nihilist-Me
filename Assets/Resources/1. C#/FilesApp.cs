using UnityEngine;
using Nova;
using Nova.TMP;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class FilesApp : MonoBehaviour
{
    [Header("REFERENCES")]
    public AppGestureHandler files;
    public GameObject[] pinnedChild;
    public GameObject[] tagsChild;

    private Dictionary<GameObject, RowCache> cache = new Dictionary<GameObject, RowCache>();

    [Header("GESTURE HANDLER")]
    public UIBlock2D pinnedBlock;
    public UIBlock2D tagsBlock;
    [Space(5)]
    public Transform pinnedArrow;
    public Transform tagsArrow;
    [Space(10)]
    public UIBlock2D sideBar;

    [Header("STYLING")]
    public Color hoverBlockColor;
    public Color unhoverBlockColor;
    private float arrowRotateDuration = 0.15f;

    Coroutine pinnedArrowRoutine;
    Coroutine tagsArrowRoutine;

    [Range(0, 1)] public float minSideBar;
    [Range(0, 1)] public float maxSideBar;
    private bool cachedIsFullscreen = false;

    bool pinnedVisible = true;
    bool tagsVisible = true;
    bool isOpen = false;

    void Start(){
        BindGestures();
        CacheAllRows();
        ApplySizeTag(files.isFullscreen);
    }

    void Update(){
        HandleFullScreen();
        if(files.State == AppWindowState.Open){
            if(!isOpen){
                StartCoroutine(ResetHoverColors());
                isOpen = true;
            }
        }else isOpen = false;
    }

    #region SETUP
    void BindGestures(){
        pinnedBlock.AddGestureHandler<Gesture.OnPress>(OnPinnedPress);
        pinnedBlock.AddGestureHandler<Gesture.OnHover>(OnPinnedHover);
        pinnedBlock.AddGestureHandler<Gesture.OnUnhover>(OnPinnedUnhover);

        tagsBlock.AddGestureHandler<Gesture.OnPress>(OnTagsPress);
        tagsBlock.AddGestureHandler<Gesture.OnHover>(OnTagsHover);
        tagsBlock.AddGestureHandler<Gesture.OnUnhover>(OnTagsUnhover);
    }

    void CacheAllRows(){
        CacheRows(pinnedChild);
        CacheRows(tagsChild);
    }

    IEnumerator ResetHoverColors(){
        yield return null;
        pinnedBlock.Color = unhoverBlockColor;
        tagsBlock.Color = unhoverBlockColor;
    }
    #endregion

    #region CACHING
    void CacheRows(GameObject[] rows){
        foreach(var row in rows){
            if(row == null) continue;
            if(cache.ContainsKey(row)) continue;
            if(row.transform.childCount < 2) continue;

            UIBlock2D icon = row.transform.GetChild(0).GetComponent<UIBlock2D>();
            TextBlock text = row.transform.GetChild(1).GetComponent<TextBlock>();

            if(icon == null || text == null) continue;

            TMP_Text tmp = text.GetComponent<TMP_Text>();
            if(tmp == null) continue;

            float baseSize = tmp.enableAutoSizing
                ? tmp.fontSizeMax
                : tmp.fontSize;

            cache.Add(row, new RowCache{
                icon = icon,
                baseIconSize = icon.Size.Value,
                text = text,
                rawText = text.Text,
                baseFontSize = baseSize
            });
        }
    }
    #endregion

    #region SIZE LOGIC
    void HandleFullScreen(){
        if(files.isFullscreen == cachedIsFullscreen) return;

        cachedIsFullscreen = files.isFullscreen;
        ApplySizeTag(cachedIsFullscreen);
    }

    void ApplySizeTag(bool fullscreen){
        float scale = (fullscreen) ? 1.4f : 1f;
        foreach(var kvp in cache){
            RowCache data = kvp.Value;

            data.icon.Size.Value = data.baseIconSize * scale;

            float fontSize = data.baseFontSize * scale;
            data.text.Text = $"<size={fontSize}>{data.rawText}</size>";
        }
        sideBar.Size.X.Percent = fullscreen ? minSideBar : maxSideBar;

        float titleSize = (fullscreen) ? 240 : 140;
        float arrowSize = (fullscreen) ? 20 : 10;

        pinnedBlock.GetChild(0).GetComponent<TextBlock>().Text = $"<size={titleSize}>Pinned</size>";
        tagsBlock.GetChild(0).GetComponent<TextBlock>().Text = $"<size={titleSize}>Tags</size>";

        pinnedBlock.GetChild(1).GetComponent<UIBlock2D>().Size.X = arrowSize;
        tagsBlock.GetChild(1).GetComponent<UIBlock2D>().Size.X = arrowSize;
    }
    #endregion

    #region PINNED
    void OnPinnedPress(Gesture.OnPress evt){
        pinnedVisible ^= true;
        RotateArrow(pinnedArrow, pinnedVisible, ref pinnedArrowRoutine);
        SetChildrenActive(pinnedChild, pinnedVisible);
    }

    void OnPinnedHover(Gesture.OnHover evt) => pinnedBlock.Color = hoverBlockColor;
    void OnPinnedUnhover(Gesture.OnUnhover evt) => pinnedBlock.Color = unhoverBlockColor;
    #endregion

    #region TAGS
    void OnTagsPress(Gesture.OnPress evt){
        tagsVisible ^= true;
        RotateArrow(tagsArrow, tagsVisible, ref tagsArrowRoutine);
        SetChildrenActive(tagsChild, tagsVisible);
    }

    void OnTagsHover(Gesture.OnHover evt) => tagsBlock.Color = hoverBlockColor;
    void OnTagsUnhover(Gesture.OnUnhover evt) => tagsBlock.Color = unhoverBlockColor;
    #endregion

    #region HELPERS
    void SetChildrenActive(GameObject[] children, bool state){
        for(int i = 0; i < children.Length; i++){
            if(children[i] == null) continue;
            if(i == 0) continue;

            children[i].SetActive(state);
        }
    }

    void RotateArrow(Transform arrow, bool expanded, ref Coroutine routine){
        if(arrow == null) return;
        if(routine != null) StopCoroutine(routine);

        routine = StartCoroutine(RotateArrowLerp(
            arrow,
            expanded ? Quaternion.Euler(0, 0, 180f) : Quaternion.identity
        ));
    }

    IEnumerator RotateArrowLerp(Transform arrow, Quaternion targetRot){
        Quaternion startRot = arrow.localRotation;
        float t = 0f;

        while(t < 1f){
            t += Time.deltaTime / arrowRotateDuration;
            arrow.localRotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        arrow.localRotation = targetRot;
    }

    #endregion
}

class RowCache
{
    public UIBlock2D icon;
    public Vector2 baseIconSize;

    public TextBlock text;
    public string rawText;
    public float baseFontSize;
}
