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
    public PanelNavigator navigator;

    public RowPair[] pinnedChild;
    public RowPair[] tagsChild;

    private Dictionary<GameObject, RowCache> cache = new Dictionary<GameObject, RowCache>();
    private Dictionary<UIBlock2D, Coroutine> hoverRoutines = new Dictionary<UIBlock2D, Coroutine>();

    private RowPair activeRow;

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
    private float hoverFadeDuration = 0.12f;
    private float hoverOffsetX = 7f;

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
        BindChildGestures();
        CacheAllRows();
        ResetAllHoverState();
        InitFirstActiveChild();
        ApplySizeTag(files.isFullscreen);
    }

    void Update(){
        HandleFullScreen();

        if(files.State == AppWindowState.Open){
            if(!isOpen){
                ResetAllHoverState();
                isOpen = true;
            }
        }else isOpen = false;
    }

    #region SETUP
    void BindGestures(){
        pinnedBlock.AddGestureHandler<Gesture.OnPress>(OnPinnedPress);
        pinnedBlock.AddGestureHandler<Gesture.OnHover>(evt => HoverBlock(pinnedBlock, true));
        pinnedBlock.AddGestureHandler<Gesture.OnUnhover>(evt => HoverBlock(pinnedBlock, false));

        tagsBlock.AddGestureHandler<Gesture.OnPress>(OnTagsPress);
        tagsBlock.AddGestureHandler<Gesture.OnHover>(evt => HoverBlock(tagsBlock, true));
        tagsBlock.AddGestureHandler<Gesture.OnUnhover>(evt => HoverBlock(tagsBlock, false));
    }

    void BindChildGestures(){
        BindChildGestures(pinnedChild);
        BindChildGestures(tagsChild);
    }

    void BindChildGestures(RowPair[] rows){
        foreach(var row in rows){
            if(row?.child == null) continue;

            UIBlock2D block = row.child.GetComponent<UIBlock2D>();
            if(block == null) continue;

            Vector3 basePos = block.Position.Value;

            block.AddGestureHandler<Gesture.OnPress>(evt => OnChildPress(row));
            block.AddGestureHandler<Gesture.OnHover>(evt => HoverRow(block, basePos, true));
            block.AddGestureHandler<Gesture.OnUnhover>(evt => HoverRow(block, basePos, false));
        }
    }
    #endregion

    #region PARENT PRESS
    void OnPinnedPress(Gesture.OnPress evt){
        pinnedVisible ^= true;
        RotateArrow(pinnedArrow, pinnedVisible, ref pinnedArrowRoutine);
        SetChildrenActive(pinnedChild, pinnedVisible);
    }

    void OnTagsPress(Gesture.OnPress evt){
        tagsVisible ^= true;
        RotateArrow(tagsArrow, tagsVisible, ref tagsArrowRoutine);
        SetChildrenActive(tagsChild, tagsVisible);
    }
    #endregion

    #region CHILD PRESS (SINGLE ACTIVE PANEL)
    void OnChildPress(RowPair row){
        if(row == null || row.childPanel == null) return;
        StartCoroutine(ChangePanel(row));
        navigator.CloseCurrentPanel(row.childPanel.gameObject);
    }

    IEnumerator ChangePanel(RowPair row){
        yield return new WaitForSeconds(.01f);
        if(activeRow != row){
            Scroller scroller = row.childPanel.GetComponent<Scroller>();
            scroller.ScrollToIndex(0, false);
            //Debug.Log($"{row.childPanel.name} : {scroller.ScrollableChildCount}");

            yield return new WaitForSeconds(.02f);
            if(activeRow != null && activeRow.childPanel != null) activeRow.childPanel.SetActive(false);
            row.childPanel.SetActive(true);
            activeRow = row;
        }
    }
    #endregion

    #region INITIAL ACTIVE
    void InitFirstActiveChild(){
        DisableAllPanels();

        RowPair first = GetFirstValidRow();
        if(first == null) return;

        first.childPanel.SetActive(true);
        activeRow = first;
    }

    RowPair GetFirstValidRow(){
        foreach(var r in pinnedChild) if(r?.childPanel != null) return r;
        foreach(var r in tagsChild) if(r?.childPanel != null) return r;

        return null;
    }

    void DisableAllPanels(){
        DisablePanels(pinnedChild);
        DisablePanels(tagsChild);
    }

    void DisablePanels(RowPair[] rows){
        foreach(var r in rows) if(r?.childPanel != null) r.childPanel.SetActive(false);
    }
    #endregion

    #region HOVER LOGIC
    void HoverBlock(UIBlock2D block, bool hover){
        StartHoverRoutine(
            block,
            hover ? hoverBlockColor : unhoverBlockColor,
            block.Position.Value
        );
    }

    void HoverRow(UIBlock2D block, Vector3 basePos, bool hover){
        Vector3 targetPos = basePos + (hover ? Vector3.right * hoverOffsetX : Vector3.zero);
        StartHoverRoutine(
            block,
            hover ? hoverBlockColor : unhoverBlockColor,
            targetPos
        );
    }

    void StartHoverRoutine(UIBlock2D block, Color targetColor, Vector3 targetPos){
        if(hoverRoutines.TryGetValue(block, out Coroutine c)) StopCoroutine(c);
        hoverRoutines[block] = StartCoroutine(HoverLerp(block, targetColor, targetPos));
    }

    IEnumerator HoverLerp(UIBlock2D block, Color targetColor, Vector3 targetPos){
        Color startColor = block.Color;
        Vector3 startPos = block.Position.Value;
        float t = 0f;

        while(t < 1f){
            t += Time.deltaTime / hoverFadeDuration;
            block.Color = Color.Lerp(startColor, targetColor, t);
            block.Position.Value = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        block.Color = targetColor;
        block.Position.Value = targetPos;
    }
    #endregion

    #region RESET
    void ResetAllHoverState(){
        pinnedBlock.Color = unhoverBlockColor;
        tagsBlock.Color = unhoverBlockColor;

        ResetChildren(pinnedChild);
        ResetChildren(tagsChild);
    }

    void ResetChildren(RowPair[] rows){
        foreach(var row in rows){
            if(row?.child == null) continue;

            UIBlock2D block = row.child.GetComponent<UIBlock2D>();
            if(block == null) continue;

            block.Color = unhoverBlockColor;
            block.Position.Value = Vector3.zero;
        }
    }
    #endregion

    #region VISIBILITY
    void SetChildrenActive(RowPair[] rows, bool state){
        foreach(var row in rows){
            if(row?.child != null) row.child.SetActive(state);
        }
    }
    #endregion

    #region SIZE / ARROW
    void HandleFullScreen(){
        if(files.isFullscreen == cachedIsFullscreen) return;
        cachedIsFullscreen = files.isFullscreen;
        ApplySizeTag(cachedIsFullscreen);
    }

    void ApplySizeTag(bool fullscreen){
        float scale = fullscreen ? 1.4f : 1f;

        foreach(var kvp in cache){
            RowCache data = kvp.Value;
            data.icon.Size.Value = data.baseIconSize * scale;
            data.text.Text = $"<size={data.baseFontSize * scale}>{data.rawText}</size>";
        }

        sideBar.Size.X.Percent = fullscreen ? minSideBar : maxSideBar;

        float titleSize = fullscreen ? 220 : 140;
        float arrowSize = fullscreen ? 20 : 10;

        pinnedBlock.GetChild(0).GetComponent<TextBlock>().Text = $"<size={titleSize}>Pinned</size>";
        tagsBlock.GetChild(0).GetComponent<TextBlock>().Text = $"<size={titleSize}>Tags</size>";

        pinnedBlock.GetChild(1).GetComponent<UIBlock2D>().Size.X = arrowSize;
        tagsBlock.GetChild(1).GetComponent<UIBlock2D>().Size.X = arrowSize;
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

    #region CACHING
    void CacheAllRows(){
        CacheRows(pinnedChild);
        CacheRows(tagsChild);
    }

    void CacheRows(RowPair[] rows){
        foreach(var row in rows){
            if(row?.child == null) continue;
            if(cache.ContainsKey(row.child)) continue;

            Transform t = row.child.transform;
            if(t.childCount < 2) continue;

            UIBlock2D icon = t.GetChild(0).GetComponent<UIBlock2D>();
            TextBlock text = t.GetChild(1).GetComponent<TextBlock>();
            TMP_Text tmp = text?.GetComponent<TMP_Text>();

            if(icon == null || text == null || tmp == null) continue;

            float baseSize = tmp.enableAutoSizing ? tmp.fontSizeMax : tmp.fontSize;

            cache.Add(row.child, new RowCache{
                icon = icon,
                baseIconSize = icon.Size.Value,
                text = text,
                rawText = text.Text,
                baseFontSize = baseSize
            });
        }
    }
    #endregion
}

[System.Serializable]
public class RowPair
{
    public GameObject child;
    public GameObject childPanel;
}

class RowCache
{
    public UIBlock2D icon;
    public Vector2 baseIconSize;
    public TextBlock text;
    public string rawText;
    public float baseFontSize;
}
