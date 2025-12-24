using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Nova;

[System.Serializable]
public class TabItem
{
    public string name;
    public UIBlock Root;
    public GameObject Screen;

    [HideInInspector] public UIBlock2D Block2D;
    [HideInInspector] public UIBlock2D Icon;
    [HideInInspector] public UIBlock2D Indicator;

    [HideInInspector] public Color32 originalIconColor;
}

public class SelectionList : MonoBehaviour
{
    [Header("TABS")]
    public List<TabItem> Tabs = new List<TabItem>();

    [Header("COLORS")]
    public Color32 normalColor = new Color32(200, 200, 200, 0);
    public Color32 highlightColor = new Color32(150, 30, 30, 255);
    public float lerpDuration = 0.2f;

    [Header("INDICATOR")]
    public float indicatorExpand = 16f;
    public float indicatorShrink = 0f;

    private readonly Hashtable lerpTable = new Hashtable();
    private readonly Hashtable sizeLerpTable = new Hashtable();
    private readonly Hashtable childColorLerpTable = new Hashtable();

    private TabItem currentSelected;

    void Start(){
        CacheComponents();
        InitializeTabs();
        SelectItemImmediate(0);
        RegisterGestures();
        ActivateScreen(0);
    }

    #region INIT
    void CacheComponents(){
        foreach(var tab in Tabs){
            tab.Block2D = tab.Root.GetComponent<UIBlock2D>();
            tab.Icon = tab.Root.GetChild(0).GetComponent<UIBlock2D>();
            tab.Indicator = tab.Root.GetChild(2).GetComponent<UIBlock2D>();
            tab.originalIconColor = tab.Icon.Color;
        }
    }

    void InitializeTabs(){
        foreach(var tab in Tabs){
            SetImmediate(tab.Block2D, normalColor);
            tab.Indicator.Size.X.Value = indicatorShrink;
        }
    }

    void RegisterGestures(){
        foreach(var tab in Tabs){
            tab.Root.AddGestureHandler<Gesture.OnPress>(e => OnClick(tab));
            tab.Root.AddGestureHandler<Gesture.OnHover>(e => OnHover(tab));
            tab.Root.AddGestureHandler<Gesture.OnUnhover>(e => OnUnhover(tab));
        }
    }
    #endregion

    #region TAB LOGIC
    void OnClick(TabItem tab){
        if(currentSelected != null && currentSelected != tab){
            LerpTo(currentSelected.Block2D, normalColor);
            LerpSize(currentSelected, indicatorShrink);
            LerpChildToOriginal(currentSelected);
        }

        currentSelected = tab;

        LerpTo(tab.Block2D, highlightColor);
        LerpSize(tab, indicatorExpand);
        LerpChildToWhite(tab);

        ActivateScreen(Tabs.IndexOf(tab));
    }

    void OnHover(TabItem tab){
        if(tab == currentSelected) return;

        LerpTo(tab.Block2D, highlightColor);
        LerpSize(tab, indicatorExpand);
        LerpChildToWhite(tab);
    }

    void OnUnhover(TabItem tab){
        if(tab == currentSelected) return;

        LerpTo(tab.Block2D, normalColor);
        LerpSize(tab, indicatorShrink);
        LerpChildToOriginal(tab);
    }

    public void SelectItemImmediate(int index){
        if(index < 0 || index >= Tabs.Count) return;

        var tab = Tabs[index];

        if(currentSelected != null)
            SetImmediate(currentSelected.Block2D, normalColor);

        currentSelected = tab;
        SetImmediate(tab.Block2D, highlightColor);

        tab.Indicator.Size.X.Value = indicatorExpand;
        tab.Icon.Color = Color.white;

        ActivateScreen(index);
    }

    void ActivateScreen(int index){
        for(int i=0; i<Tabs.Count; i++){
            if(Tabs[i].Screen != null)
                Tabs[i].Screen.SetActive(i == index);
        }
    }
    #endregion

    #region LERP COLOR
    void SetImmediate(UIBlock2D block, Color32 col){
        block.Color = col;
    }

    void LerpTo(UIBlock2D block, Color32 target){
        if(lerpTable.Contains(block)){
            StopCoroutine((Coroutine)lerpTable[block]);
            lerpTable.Remove(block);
        }

        Coroutine c = StartCoroutine(LerpRoutine(block, target));
        lerpTable.Add(block, c);
    }

    IEnumerator LerpRoutine(UIBlock2D block, Color32 target){
        Color32 start = block.Color;
        float t = 0;

        while(t < lerpDuration){
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lerpDuration);
            p = p * p * (3f - 2f * p);
            block.Color = Color32.Lerp(start, target, p);
            yield return null;
        }

        block.Color = target;

        if(lerpTable.Contains(block)) lerpTable.Remove(block);
    }
    #endregion

    #region LERP SIZE
    void LerpSize(TabItem tab, float targetSize){
        if(sizeLerpTable.Contains(tab)){
            StopCoroutine((Coroutine)sizeLerpTable[tab]);
            sizeLerpTable.Remove(tab);
        }

        Coroutine c = StartCoroutine(LerpSizeRoutine(tab, targetSize));
        sizeLerpTable.Add(tab, c);
    }

    IEnumerator LerpSizeRoutine(TabItem tab, float targetSize){
        float start = tab.Indicator.Size.X.Value;
        float t = 0;

        while(t < lerpDuration){
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lerpDuration);
            p = p * p * (3 - 2*p);

            tab.Indicator.Size.X.Value = Mathf.Lerp(start, targetSize, p);

            yield return null;
        }

        tab.Indicator.Size.X.Value = targetSize;

        if(sizeLerpTable.Contains(tab)) sizeLerpTable.Remove(tab);
    }
    #endregion

    #region LERP CHILD COLORS
    void LerpChildToWhite(TabItem tab){
        LerpChildColor(tab, Color.white);
    }

    void LerpChildToOriginal(TabItem tab){
        LerpChildColor(tab, tab.originalIconColor);
    }

    void LerpChildColor(TabItem tab, Color32 target){
        if(childColorLerpTable.Contains(tab)){
            StopCoroutine((Coroutine)childColorLerpTable[tab]);
            childColorLerpTable.Remove(tab);
        }

        Coroutine c = StartCoroutine(LerpChildRoutine(tab, target));
        childColorLerpTable.Add(tab, c);
    }

    IEnumerator LerpChildRoutine(TabItem tab, Color32 target){
        UIBlock2D icon = tab.Icon;
        Color32 start = icon.Color;
        float t = 0;

        while(t < lerpDuration){
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lerpDuration);
            p = p * p * (3 - 2*p);
            icon.Color = Color32.Lerp(start, target, p);
            yield return null;
        }

        icon.Color = target;

        if(childColorLerpTable.Contains(tab)) childColorLerpTable.Remove(tab);
    }
    #endregion
}
