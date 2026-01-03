using UnityEngine;
using Nova;
using System.Collections.Generic;

public class PlayMenuGestureHandler : MonoBehaviour
{
    [System.Serializable]
    public class MenuRow{
        public GameObject child;
        public GameObject panel;
    }

    [Header("LAYOUT")]
    public MenuRow[] rows;

    [Header("COLORS (ROW)")]
    public Color rowHoverColor = Color.white;
    public Color rowUnhoverColor = Color.gray;

    [Header("COLORS (ICON)")]
    public Color iconHoverColor = Color.white;
    public Color iconUnhoverColor = Color.gray;

    [Header("SMOOTHING")]
    public float colorLerpSpeed = 14f;

    private UIBlock2D[] rowBlocks;
    private UIBlock2D[] iconBlocks;

    private Color[] rowTargetColors;
    private Color[] iconTargetColors;

    private Dictionary<UIBlock2D, int> indexMap;

    private int currentSelection = -1;

    void Start(){
        CacheRows();
        BindGestures();
        ResetState();

        SetSelection(0);
    }

    void Update(){
        AnimateColors();
    }

    void CacheRows(){
        int count = rows.Length;

        rowBlocks = new UIBlock2D[count];
        iconBlocks = new UIBlock2D[count];

        rowTargetColors = new Color[count];
        iconTargetColors = new Color[count];

        indexMap = new Dictionary<UIBlock2D, int>();

        for(int i = 0; i < count; i++){
            UIBlock2D block = rows[i].child.GetComponent<UIBlock2D>();
            rowBlocks[i] = block;
            indexMap.Add(block, i);

            if(rows[i].child.transform.childCount > 0){
                iconBlocks[i] = rows[i].child.transform.GetChild(0).GetComponent<UIBlock2D>();
            }

            rowTargetColors[i] = rowUnhoverColor;
            iconTargetColors[i] = iconUnhoverColor;
        }
    }

    void BindGestures(){
        foreach(var block in rowBlocks){
            block.AddGestureHandler<Gesture.OnHover>(OnHover);
            block.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
            block.AddGestureHandler<Gesture.OnPress>(OnClick);
        }
    }

    void ResetState(){
        for(int i = 0; i < rows.Length; i++){
            rows[i].panel.SetActive(false);

            rowBlocks[i].Color = rowUnhoverColor;
            if(iconBlocks[i] != null) iconBlocks[i].Color = iconUnhoverColor;
        }

        currentSelection = -1;
    }

    #region GESTURE
    void OnHover(Gesture.OnHover evt){
        if(!(evt.Target is UIBlock2D block)) return;
        if(!indexMap.TryGetValue(block, out int index)) return;

        rowTargetColors[index] = rowHoverColor;
        iconTargetColors[index] = iconHoverColor;
    }

    void OnUnhover(Gesture.OnUnhover evt){
        if(!(evt.Target is UIBlock2D block)) return;
        if(!indexMap.TryGetValue(block, out int index)) return;
        if(index == currentSelection) return;

        rowTargetColors[index] = rowUnhoverColor;
        iconTargetColors[index] = iconUnhoverColor;
    }

    void OnClick(Gesture.OnPress evt){
        if(!(evt.Target is UIBlock2D block)) return;
        if(!indexMap.TryGetValue(block, out int index)) return;

        SetSelection(index);
    }
    #endregion

    #region OTHER METHODS
    void SetSelection(int index){
        currentSelection = index;

        for(int i = 0; i < rows.Length; i++){
            bool active = i == index;

            rows[i].panel.SetActive(active);

            rowTargetColors[i] = active ? rowHoverColor : rowUnhoverColor;
            iconTargetColors[i] = active ? iconHoverColor : iconUnhoverColor;
        }
    }

    void AnimateColors(){
        for(int i = 0; i < rowBlocks.Length; i++){
            rowBlocks[i].Color = Color.Lerp(rowBlocks[i].Color, rowTargetColors[i], Time.unscaledDeltaTime * colorLerpSpeed);

            if(iconBlocks[i] != null){
                iconBlocks[i].Color = Color.Lerp(iconBlocks[i].Color, iconTargetColors[i], Time.unscaledDeltaTime * colorLerpSpeed);
            }
        }
    }
    #endregion
}
