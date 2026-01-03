using UnityEngine;
using Nova;
using System.Collections;
using System.Collections.Generic;

public class MainMenuGestureHandler : MonoBehaviour
{
    [Header("TITLE")]
    [SerializeField] private TextBlock[] titleChars;
    [SerializeField] private float floatAmplitude = 6f;
    [SerializeField] private float floatSpeed = 1.2f;
    [SerializeField] private float scaleAmplitude = .03f;
    [SerializeField] private float titleLerpSpeed = 8f;

    [Header("MENU OPTIONS")]
    [SerializeField] private UIBlock2D[] menuOptions;
    [SerializeField] private UIBlock2D[] selectors;

    [Header("PANELS")]
    [SerializeField] private GameObject PanelGroup1;
    [SerializeField] private GameObject PanelGroup2;

    [Header("SELECTOR")]
    [SerializeField] private float selectorTargetWidth = 10f;
    [SerializeField] private float selectorExpandSpeed = 28f;
    [SerializeField] private float selectorCollapseSpeed = 14f;
    [SerializeField] private float offsetRight = 12f;

    [Header("PARENT COLORS")]
    [SerializeField] private Color parentHoverColor = Color.white;
    [SerializeField] private Color parentUnhoverColor = Color.black;

    [Header("CHILD COLORS")]
    [SerializeField] private Color childHoverColor = Color.white;
    [SerializeField] private Color childUnhoverColor = Color.gray;

    [Header("SMOOTHING")]
    [SerializeField] private float menuLerpSpeed = 14f;
    [SerializeField] private float colorLerpSpeed = 12f;

    [Header("PANEL DEPTH")]
    [SerializeField] private float activePanelZ = -2f;
    [SerializeField] private float inactivePanelZ = 0f;
    [SerializeField] private float panelZLerpSpeed = 12f;

    private Vector3[] menuStartPos;
    private Vector3[] menuTargetPos;

    private Color[] parentTargetColor;
    private Color[] childTargetColor;

    private UIBlock2D[] menuIcons;
    private TextBlock[] menuTexts;

    private Coroutine[] selectorRoutines;
    private Dictionary<UIBlock2D, int> indexMap;

    private Vector3[] titleStartPos;
    private Vector3[] titleStartScale;

    private Transform[] panels;
    private float[] panelTargetZ;

    private int currentSelection = -1;

    void Start(){
        CacheTitleData();
        CacheMenuData();
        CachePanels();
        BindGestures();
        ResetAllState();
        SetSelection(0);
    }

    void Update(){
        AnimateTitle();
        AnimateMenus();
        AnimatePanels();
    }

    void CacheMenuData(){
        int count = menuOptions.Length;

        menuStartPos = new Vector3[count];
        menuTargetPos = new Vector3[count];
        parentTargetColor = new Color[count];
        childTargetColor = new Color[count];

        menuIcons = new UIBlock2D[count];
        menuTexts = new TextBlock[count];

        for(int i = 0; i < count; i++){
            Transform t = menuOptions[i].transform;

            menuStartPos[i] = t.localPosition;
            menuTargetPos[i] = menuStartPos[i];

            parentTargetColor[i] = parentUnhoverColor;
            childTargetColor[i] = childUnhoverColor;

            if(t.childCount > 0) menuIcons[i] = t.GetChild(0).GetComponent<UIBlock2D>();
            if(t.childCount > 1) menuTexts[i] = t.GetChild(1).GetComponent<TextBlock>();
        }
    }

    void CachePanels(){
        panels = new Transform[2];
        panelTargetZ = new float[2];

        panels[0] = PanelGroup1.transform;
        panels[1] = PanelGroup2.transform;

        for(int i = 0; i < panels.Length; i++) panelTargetZ[i] = inactivePanelZ;
    }

    void CacheTitleData(){
        titleStartPos = new Vector3[titleChars.Length];
        titleStartScale = new Vector3[titleChars.Length];

        for(int i = 0; i < titleChars.Length; i++){
            titleStartPos[i] = titleChars[i].transform.localPosition;
            titleStartScale[i] = titleChars[i].transform.localScale;
        }
    }

    void BindGestures(){
        selectorRoutines = new Coroutine[menuOptions.Length];
        indexMap = new Dictionary<UIBlock2D, int>();

        for(int i = 0; i < menuOptions.Length; i++){
            indexMap.Add(menuOptions[i], i);

            menuOptions[i].AddGestureHandler<Gesture.OnHover>(OnHover);
            menuOptions[i].AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
            menuOptions[i].AddGestureHandler<Gesture.OnPress>(OnClick);
        }
    }

    #region STATE MANAGEMENT
    void ResetAllState(){
        for(int i = 0; i < menuOptions.Length; i++){
            selectors[i].Size.X.Value = 0f;
            menuTargetPos[i] = menuStartPos[i];
            parentTargetColor[i] = parentUnhoverColor;
            childTargetColor[i] = childUnhoverColor;
        }

        currentSelection = -1;
    }

    void SetSelection(int index){
        currentSelection = index;
        for(int i = 0; i < menuOptions.Length; i++){
            bool selected = i == index;

            if(selected){
                ApplyHoverVisual(i);
                AnimateSelector(i, selectorTargetWidth, true);
            }else{
                ApplyUnhoverVisual(i);
                AnimateSelector(i, 0f, false);
            }
        }

        if(index == 0) SetPanelActive(0);
        else if(index == 1) SetPanelActive(1);
        else if(index == 2) Application.Quit();
    }

    void SetPanelActive(int activeIndex){
        for(int i = 0; i < panels.Length; i++){
            bool active = i == activeIndex;
            panelTargetZ[i] = active ? activePanelZ : inactivePanelZ;

            if(active) panels[i].SetAsLastSibling();
        }
    }
    #endregion

    #region VISUAL
    void OnHover(Gesture.OnHover evt){
        if(!(evt.Target is UIBlock2D block)) return;
        if(!indexMap.TryGetValue(block, out int index)) return;
        if(index == currentSelection) return;

        ApplyHoverVisual(index);
        AnimateSelector(index, selectorTargetWidth, true);
    }

    void OnUnhover(Gesture.OnUnhover evt){
        if(!(evt.Target is UIBlock2D block)) return;
        if(!indexMap.TryGetValue(block, out int index)) return;
        if(index == currentSelection) return;

        ApplyUnhoverVisual(index);
        AnimateSelector(index, 0f, false);
    }

    void OnClick(Gesture.OnPress evt){
        if(!(evt.Target is UIBlock2D block)) return;
        if(!indexMap.TryGetValue(block, out int index)) return;

        SetSelection(index);
    }

    void ApplyHoverVisual(int index){
        menuTargetPos[index] = menuStartPos[index] + Vector3.right * offsetRight;
        parentTargetColor[index] = parentHoverColor;
        childTargetColor[index] = childHoverColor;
    }

    void ApplyUnhoverVisual(int index){
        menuTargetPos[index] = menuStartPos[index];
        parentTargetColor[index] = parentUnhoverColor;
        childTargetColor[index] = childUnhoverColor;
    }
    #endregion

    #region  ANIMATION
    void AnimateMenus(){
        for(int i = 0; i < menuOptions.Length; i++){
            menuOptions[i].transform.localPosition = Vector3.Lerp(menuOptions[i].transform.localPosition, menuTargetPos[i], Time.unscaledDeltaTime * menuLerpSpeed);
            menuOptions[i].Color = Color.Lerp(menuOptions[i].Color, parentTargetColor[i], Time.unscaledDeltaTime * colorLerpSpeed);

            if(menuIcons[i] != null)
                menuIcons[i].Color = Color.Lerp(menuIcons[i].Color, childTargetColor[i], Time.unscaledDeltaTime * colorLerpSpeed);

            if(menuTexts[i] != null)
                menuTexts[i].Color = Color.Lerp(menuTexts[i].Color, childTargetColor[i], Time.unscaledDeltaTime * colorLerpSpeed);
        }
    }

    void AnimatePanels(){
        for(int i = 0; i < panels.Length; i++){
            Vector3 pos = panels[i].localPosition;
            pos.z = Mathf.Lerp(pos.z, panelTargetZ[i], Time.unscaledDeltaTime * panelZLerpSpeed);
            panels[i].localPosition = pos;
        }
    }

    void AnimateSelector(int index, float target, bool expanding){
        if(selectorRoutines[index] != null) StopCoroutine(selectorRoutines[index]);

        float speed = (expanding) ? selectorExpandSpeed : selectorCollapseSpeed;
        selectorRoutines[index] = StartCoroutine(LerpSelector(selectors[index], target, speed));
    }

    IEnumerator LerpSelector(UIBlock2D selector, float target, float speed){
        float start = selector.Size.X.Value;
        float t = 0f;

        while(t < 1f){
            t += Time.unscaledDeltaTime * speed;
            selector.Size.X.Value = Mathf.Lerp(start, target, t);
            yield return null;
        }

        selector.Size.X.Value = target;
    }

    void AnimateTitle(){
        for(int i = 0; i < titleChars.Length; i++){
            float phase = i * 0.35f;
            float t = Time.unscaledTime * floatSpeed + phase;

            Vector3 targetPos = titleStartPos[i] + Vector3.up * Mathf.Sin(t) * floatAmplitude;
            Vector3 targetScale = titleStartScale[i] * (1f + Mathf.Sin(t) * scaleAmplitude);

            titleChars[i].transform.localPosition = Vector3.Lerp(titleChars[i].transform.localPosition, targetPos, Time.unscaledDeltaTime * titleLerpSpeed);
            titleChars[i].transform.localScale = Vector3.Lerp(titleChars[i].transform.localScale, targetScale, Time.unscaledDeltaTime * titleLerpSpeed);
        }
    }
    #endregion
}
