using UnityEngine;
using Nova;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { private set; get; }

    [Header("CONFIGURATION")]
    public bool isPauseEnabled = true; //Wtf is this for?
    
    [Header("TITLE SETTINGS")]
    public TextBlock[] titleLetters;
    public float typingSpeed = .05f;
    public float cursorBlinkSpeed = .5f;
    public int cursorRadius = 1;
    public float scrambleSpeed = .03f;
    public float scrambleDuration = .5f;
    
    [Header("TITLE COLOR WAVE")]
    public Color waveColor = new Color(1f, 0f, 1f, 1f);
    public float waveDuration = .5f;
    public float waveTrailLength = 3f;
    public float waveFadeSpeed = 2f;
    
    [Header("NAVIGATION SETTINGS")]
    public float moveDurationPerItem = .3f;
    public float staggerDelay = .05f;
    [Range(0f, 1f)] public float bounceIntensity = .2f;
    [Space(5)]
    public float selectionRotateAngle = 45f;
    public float textHoverOffset = 10f;
    
    [Header("COLOR SETTINGS")]
    public Color textHoverColor = Color.cyan;
    public Color textDefaultColor = Color.white;
    
    public Color itemBlockHoverColor = Color.white;
    public Color itemBlockDefaultColor = Color.white;
    
    public Color childHoverColor = Color.white;
    public Color childDefaultColor = Color.white;
    
    public Color iconHoverColor = Color.cyan;
    public Color iconDefaultColor = Color.white;
    
    [Header("OPACITY SETTINGS")]
    [Range(0f, 1f)] public float itemBlockHoverOpacity = 1f;
    [Range(0f, 1f)] public float itemBlockDefaultOpacity = 0f;
    
    [Range(0f, 1f)] public float childHoverOpacity = 1f;
    [Range(0f, 1f)] public float childDefaultOpacity = 0f;
    
    [Header("ANIMATION SPEEDS")]
    public float hoverDuration = .08f;
    public float unhoverDuration = .05f;
    public float selectDuration = .15f;
    public float deselectDuration = .08f;
    
    [Header("REFERENCES")]
    [SerializeField] private UIBlock2D rootNode;
    [SerializeField] private List<PauseSelectionItem> selectionItems;
    private CustomCursor cc;
    
    private bool isScrambling;
    private bool cursorVisible;
    private int currentOptionIndex = 0;
    private int lastHoveredIndex = -1;
    
    private string fullTitle = "PAUSE";
    private char[] originalLetters;
    
    private Coroutine cursorRoutine;
    private Coroutine scrambleRoutine;
    private Coroutine waveRoutine;
    private Coroutine[] sideRoutines;
    private Coroutine currentHoverRoutine;
    private Coroutine currentSelectionRoutine;
    
    private float[] letterColorIntensity;
    
    private Dictionary<PauseSelectionItem, PauseSelectionPanel> itemToPanelMap = new Dictionary<PauseSelectionItem, PauseSelectionPanel>();
    private Dictionary<PauseSelectionItem, Vector3> originalScales = new Dictionary<PauseSelectionItem, Vector3>();
    private Dictionary<PauseSelectionItem, Vector3> originalPositions = new Dictionary<PauseSelectionItem, Vector3>();
    private Dictionary<PauseSelectionItem, float> originalTextOffsets = new Dictionary<PauseSelectionItem, float>();
    
    void Awake(){
        Instance = this;
    }
    
    void Start(){
        if(!isPauseEnabled) return;
        if(cc == null) cc = FindFirstObjectByType<CustomCursor>();
        
        Assert.IsNotNull(rootNode, "rootNode is missing");
        Assert.IsTrue(selectionItems.Count > 0, "selectionItems is empty");
        
        originalLetters = fullTitle.ToCharArray();
        letterColorIntensity = new float[originalLetters.Length];
        
        CacheOriginalValues();
        SetupItemToPanelMapping();
        SetupGestureHandlers();
        SetupTitleLetterHandlers();
        
        ResetMenuState();
        rootNode.gameObject.SetActive(false);
    }
    
    void Update(){
        if(!isPauseEnabled || !rootNode.gameObject.activeSelf) return;
        
        HandleKeyboardNavigation();
    }
    
    #region INIT
    void CacheOriginalValues(){
        foreach(var item in selectionItems){
            if(item == null || item.itemBlock == null) continue;
            
            originalScales[item] = item.itemBlock.transform.localScale;
            originalPositions[item] = item.itemBlock.transform.localPosition;
            
            var firstChild = item.itemBlock.GetChild(0);
            if(firstChild != null) originalTextOffsets[item] = firstChild.Position.X.Value;
        }
        
        sideRoutines = new Coroutine[selectionItems.Count];
    }
    
    void SetupItemToPanelMapping(){
        itemToPanelMap.Clear();
        
        foreach(var item in selectionItems){
            if(item == null || item.targetPanel == null) continue;
            itemToPanelMap[item] = item.targetPanel;
        }
    }
    
    void SetupGestureHandlers(){
        foreach(var item in selectionItems){
            if(item == null || item.itemBlock == null) continue;
            
            item.itemBlock.AddGestureHandler<Gesture.OnPress>(evt => OnItemPressed(item));
            item.itemBlock.AddGestureHandler<Gesture.OnHover>(evt => OnItemHover(item));
            item.itemBlock.AddGestureHandler<Gesture.OnUnhover>(evt => OnItemUnhover(item));
            
            if(item.selectionIndicator != null) item.selectionIndicator.AddGestureHandler<Gesture.OnPress>(evt => OnItemPressed(item));
        }
    }
    
    void SetupTitleLetterHandlers(){
        for(int i = 0; i < titleLetters.Length; i++){
            if(titleLetters[i] != null){
                UIBlock letterBlock = titleLetters[i].GetComponent<UIBlock>();
                if(letterBlock != null){
                    int index = i;
                    letterBlock.AddGestureHandler<Gesture.OnHover>(evt => OnLetterHover(index));
                }
            }
        }
    }
    #endregion
    
    #region PUBLIC INTERFACE
    public void OpenPauseMenu(){
        if(cc != null){
            cc._allowMovement = false;
            cc.gameObject.SetActive(false);
        }
        if(!isPauseEnabled) return;
        
        rootNode.gameObject.SetActive(true);
        StopAllCoroutines();
        ResetMenuVisualState();
        
        currentOptionIndex = 0;
        
        UpdateSelectionImmediate();
        
        StartCoroutine(TypeTitle());
        StartCoroutine(AnimateItemsIn());
    }
    
    public void ClosePauseMenu(){
        if(cc != null){
            cc.gameObject.SetActive(true);
            cc._allowMovement = true;
        }
        if(!isPauseEnabled) return;
        
        if(cursorRoutine != null) StopCoroutine(cursorRoutine);
        if(scrambleRoutine != null) StopCoroutine(scrambleRoutine);
        if(waveRoutine != null) StopCoroutine(waveRoutine);
        
        rootNode.gameObject.SetActive(false);
    }
    
    public void ExitGame(){
        Time.timeScale = 1f;
        ClosePauseMenu();
        StartCoroutine(ExitMenu(.5f));
    }
    IEnumerator ExitMenu(float delay){
        yield return new WaitForSecondsRealtime(delay);
        SceneChangeManager.Instance.ChangeToMenu();
    }
    #endregion
    
    #region NAVIGATION & INPUT
    void HandleKeyboardNavigation(){
        if(Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) NavigateTo((currentOptionIndex + 1) % selectionItems.Count);
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) NavigateTo((currentOptionIndex - 1 + selectionItems.Count) % selectionItems.Count);
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) OnItemPressed(selectionItems[currentOptionIndex]);
    }
    
    void NavigateTo(int newIndex){
        if(newIndex == currentOptionIndex) return;
        
        if(currentHoverRoutine != null){
            StopCoroutine(currentHoverRoutine);
            currentHoverRoutine = null;
        }
        
        if(currentSelectionRoutine != null) StopCoroutine(currentSelectionRoutine);
        
        int oldIndex = currentOptionIndex;
        currentOptionIndex = newIndex;
        
        currentSelectionRoutine = StartCoroutine(AnimateSelectionParallel(oldIndex, newIndex));
    }
    
    void UpdateSelectionImmediate(){
        for(int i = 0; i < selectionItems.Count; i++){
            var item = selectionItems[i];
            if(item == null) continue;
            
            bool isSelected = (i == currentOptionIndex);
            
            if(isSelected) CompleteHover(item);
            else CompleteUnhover(item);
            
            if(item.targetPanel != null){
                if(sideRoutines[i] != null) StopCoroutine(sideRoutines[i]);
                sideRoutines[i] = StartCoroutine(AnimateSidePanel(item.targetPanel, isSelected));
            }
        }
    }
    #endregion
    
    #region GESTURE HANDLERS
    void OnItemPressed(PauseSelectionItem item){
        int index = selectionItems.IndexOf(item);
        if(index >= 0) NavigateTo(index);
        
        switch(index){
            case 0: //Resume
                GameManager.Instance.isPaused = false;
                ClosePauseMenu();
                break;
            case 3: //Go back
                ExitGame();
                break;
        }
    }
    
    void OnItemHover(PauseSelectionItem item){
        int index = selectionItems.IndexOf(item);
        if(index < 0 || index == currentOptionIndex) return;
        
        lastHoveredIndex = index;
        
        if(currentHoverRoutine != null) StopCoroutine(currentHoverRoutine);
        
        currentHoverRoutine = StartCoroutine(AnimateHoverParallel(index, true));
        
        item.OnHover();
    }
    
    void OnItemUnhover(PauseSelectionItem item){
        int index = selectionItems.IndexOf(item);
        if(index < 0 || index == currentOptionIndex) return;
        
        if(lastHoveredIndex == index){
            if(currentHoverRoutine != null) StopCoroutine(currentHoverRoutine);
            currentHoverRoutine = StartCoroutine(AnimateHoverParallel(index, false));
        }
        
        item.OnUnhover();
    }
    
    void OnLetterHover(int letterIndex){
        if(!isScrambling){
            if(scrambleRoutine != null) StopCoroutine(scrambleRoutine);
            scrambleRoutine = StartCoroutine(ScrambleLetter(letterIndex));
        }
        
        if(waveRoutine != null) StopCoroutine(waveRoutine);
        waveRoutine = StartCoroutine(ColorWaveEffect(letterIndex));
    }
    #endregion
    
    #region TITLE ANIMATION
    IEnumerator TypeTitle(){
        foreach(var letter in titleLetters){
            if(letter != null) letter.Text = "";
        }
        
        for(int i = 0; i < originalLetters.Length; i++){
            if(i < titleLetters.Length && titleLetters[i] != null) titleLetters[i].Text = originalLetters[i].ToString();
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
        
        if(cursorRoutine != null) StopCoroutine(cursorRoutine);
        cursorRoutine = StartCoroutine(CursorBlink());
    }
    
    void UpdateTitleDisplay(){
        int lastIndex = originalLetters.Length - 1;
        if(lastIndex >= 0 && lastIndex < titleLetters.Length && titleLetters[lastIndex] != null){
            titleLetters[lastIndex].Text = originalLetters[lastIndex] + (cursorVisible ? "." : "");
        }
    }
    
    IEnumerator CursorBlink(){
        cursorVisible = true;
        
        while(rootNode.gameObject.activeSelf){
            cursorVisible = !cursorVisible;
            UpdateTitleDisplay();
            yield return new WaitForSecondsRealtime(cursorBlinkSpeed);
        }
        
        cursorVisible = false;
        UpdateTitleDisplay();
    }
    
    IEnumerator ScrambleLetter(int centerIndex){
        isScrambling = true;
        float elapsed = 0f;
        
        while(elapsed < scrambleDuration){
            int start = Mathf.Max(0, centerIndex - cursorRadius);
            int end = Mathf.Min(originalLetters.Length - 1, centerIndex + cursorRadius);
            
            for(int i = start; i <= end; i++){
                if(i < titleLetters.Length && titleLetters[i] != null){
                    char randomChar = (char)Random.Range(65, 91);
                    titleLetters[i].Text = randomChar.ToString();
                }
            }
            
            yield return new WaitForSecondsRealtime(scrambleSpeed);
            elapsed += scrambleSpeed;
            
            for(int i = start; i <= end; i++){
                if(i < titleLetters.Length && titleLetters[i] != null) titleLetters[i].Text = originalLetters[i].ToString();
            }
        }
        
        for(int i = 0; i < originalLetters.Length; i++){
            if(i < titleLetters.Length && titleLetters[i] != null) titleLetters[i].Text = originalLetters[i].ToString();
        }
        
        UpdateTitleDisplay();
        isScrambling = false;
    }
    
    IEnumerator ColorWaveEffect(int startIndex){
        for(int i = 0; i < letterColorIntensity.Length; i++){
            letterColorIntensity[i] = 0f;
            UpdateLetterColor(i);
        }
        
        float elapsed = 0f;
        
        while(elapsed < waveDuration){
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / waveDuration;
            
            float wavePosition = Mathf.Lerp(startIndex, originalLetters.Length, progress);
            
            for(int i = 0; i < originalLetters.Length; i++){
                float distance = wavePosition - i;
                
                if(distance >= 0 && distance < waveTrailLength){
                    letterColorIntensity[i] = Mathf.Clamp01(1f - (distance / waveTrailLength));
                }
                else if(distance < 0){
                    letterColorIntensity[i] = 0f;
                }
                else{
                    letterColorIntensity[i] = Mathf.Max(0f, letterColorIntensity[i] - (waveFadeSpeed * Time.unscaledDeltaTime));
                }
                
                UpdateLetterColor(i);
            }
            
            yield return null;
        }
        
        while(HasActiveColors()){
            for(int i = 0; i < letterColorIntensity.Length; i++){
                if(letterColorIntensity[i] > 0){
                    letterColorIntensity[i] = Mathf.Max(0f, letterColorIntensity[i] - (waveFadeSpeed * Time.unscaledDeltaTime));
                    UpdateLetterColor(i);
                }
            }
            yield return null;
        }
    }
    
    bool HasActiveColors(){
        foreach(float intensity in letterColorIntensity){
            if(intensity > .01f) return true;
        }
        return false;
    }
    
    void UpdateLetterColor(int index){
        if(index < titleLetters.Length && titleLetters[index] != null){
            Color currentColor = Color.Lerp(textDefaultColor, waveColor, letterColorIntensity[index]);
            titleLetters[index].Color = currentColor;
        }
    }
    #endregion
    
    #region UI ANIMATIONS
    IEnumerator AnimateItemsIn(){
        float offScreenY = -1000f;
        
        foreach(var item in selectionItems){
            if(item == null || item.itemBlock == null) continue;
            
            Vector3 originalPos = originalPositions[item];
            Vector3 startPos = new Vector3(originalPos.x, offScreenY, originalPos.z);
            item.itemBlock.transform.localPosition = startPos;
            
            item.itemBlock.transform.localScale = Vector3.one * .7f;
        }
        
        for(int i = 0; i < selectionItems.Count; i++){
            var item = selectionItems[i];
            if(item == null || item.itemBlock == null) continue;
            
            Vector3 originalPos = originalPositions[item];
            Vector3 startPos = new Vector3(originalPos.x, offScreenY, originalPos.z);
            Vector3 endPos = originalPos;
            Vector3 endScale = originalScales[item];
            
            StartCoroutine(AnimateSingleItemIn(item, startPos, endPos, endScale));
            
            yield return new WaitForSecondsRealtime(staggerDelay);
        }
        
        yield return new WaitForSecondsRealtime(moveDurationPerItem);
    }
    
    IEnumerator AnimateSingleItemIn(PauseSelectionItem item, Vector3 startPos, Vector3 endPos, Vector3 endScale){
        float elapsed = 0f;
        
        while(elapsed < moveDurationPerItem){
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / moveDurationPerItem;
            
            float springT = FastSpringEaseOut(t);
            Vector3 currentPos = Vector3.LerpUnclamped(startPos, endPos, springT);
            
            if(t < .8f){
                float wobble = Mathf.Sin(t * Mathf.PI * 3f) * 3f * (1f - t);
                currentPos.x += wobble;
            }
            
            float scaleT = FastSpringEaseOut(t);
            Vector3 currentScale = Vector3.LerpUnclamped(Vector3.one * .7f, endScale * 1.1f, scaleT);
            
            item.itemBlock.transform.localPosition = currentPos;
            item.itemBlock.transform.localScale = currentScale;
            
            yield return null;
        }
        
        item.itemBlock.transform.localPosition = endPos;
        item.itemBlock.transform.localScale = endScale;
    }
    
    float FastSpringEaseOut(float t){
        t = Mathf.Clamp01(t);
        
        if(t < .7f) return 1f - Mathf.Pow(1f - (t / .7f), 3f);
        else{
            float remaining = (t - .7f) / .3f;
            return 1f + (Mathf.Sin(remaining * Mathf.PI * 2.5f) * .15f * (1f - remaining));
        }
    }
    
    IEnumerator AnimateSelectionParallel(int deselectIndex, int selectIndex){
        PauseSelectionItem deselectItem = selectionItems[deselectIndex];
        PauseSelectionItem selectItem = selectionItems[selectIndex];
        
        float elapsed = 0f;
        float selectDur = this.selectDuration;
        float deselectDur = this.deselectDuration;
        
        float deselectStartOffset = GetCurrentTextOffset(deselectItem);
        float selectStartOffset = GetCurrentTextOffset(selectItem);
        
        while(elapsed < Mathf.Max(selectDur, deselectDur)){
            elapsed += Time.unscaledDeltaTime;
            
            if(elapsed < deselectDur){
                float deselectT = elapsed / deselectDur;
                
                if(deselectItem.selectionIndicator != null){
                    deselectItem.selectionIndicator.Color = Color.Lerp(iconHoverColor, iconDefaultColor, deselectT);
                    float rotation = Mathf.Lerp(-selectionRotateAngle, selectionRotateAngle, deselectT);
                    deselectItem.selectionIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                }
                
                if(deselectItem.textBlock != null) deselectItem.textBlock.Color = Color.Lerp(textHoverColor, textDefaultColor, deselectT);
                
                Color itemColor = Color.Lerp(itemBlockHoverColor, itemBlockDefaultColor, deselectT);
                float itemAlpha = Mathf.Lerp(itemBlockHoverOpacity, itemBlockDefaultOpacity, deselectT);
                SetItemBlockState(deselectItem, itemColor, itemAlpha);
                
                Color childColor = Color.Lerp(childHoverColor, childDefaultColor, deselectT);
                float childAlpha = Mathf.Lerp(childHoverOpacity, childDefaultOpacity, deselectT);
                SetChildState(deselectItem, childColor, childAlpha);
                
                float targetDeselectOffset = originalTextOffsets.ContainsKey(deselectItem) ? originalTextOffsets[deselectItem] : 0f;
                float currentOffset = Mathf.Lerp(deselectStartOffset, targetDeselectOffset, deselectT);
                UpdateTextOffset(deselectItem, currentOffset);
            }
            
            if(elapsed < selectDur){
                float selectT = elapsed / selectDur;
                
                if(selectItem.selectionIndicator != null){
                    selectItem.selectionIndicator.Color = Color.Lerp(iconDefaultColor, iconHoverColor, selectT);
                    float rotation = Mathf.Lerp(selectionRotateAngle, -selectionRotateAngle, selectT);
                    selectItem.selectionIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                }
                
                if(selectItem.textBlock != null) selectItem.textBlock.Color = Color.Lerp(textDefaultColor, textHoverColor, selectT);
                
                Color itemColor = Color.Lerp(itemBlockDefaultColor, itemBlockHoverColor, selectT);
                float itemAlpha = Mathf.Lerp(itemBlockDefaultOpacity, itemBlockHoverOpacity, selectT);
                SetItemBlockState(selectItem, itemColor, itemAlpha);
                
                Color childColor = Color.Lerp(childDefaultColor, childHoverColor, selectT);
                float childAlpha = Mathf.Lerp(childDefaultOpacity, childHoverOpacity, selectT);
                SetChildState(selectItem, childColor, childAlpha);
                
                float targetSelectOffset = (originalTextOffsets.ContainsKey(selectItem) ? originalTextOffsets[selectItem] : 0f) + textHoverOffset;
                float currentOffset = Mathf.Lerp(selectStartOffset, targetSelectOffset, selectT);
                UpdateTextOffset(selectItem, currentOffset);
            }
            
            yield return null;
        }
        
        CompleteUnhover(deselectItem);
        CompleteHover(selectItem);
        
        if(itemToPanelMap.ContainsKey(deselectItem)) itemToPanelMap[deselectItem].Hide();
        if(itemToPanelMap.ContainsKey(selectItem)) itemToPanelMap[selectItem].Show();
    }
    
    IEnumerator AnimateHoverParallel(int itemIndex, bool hover){
        PauseSelectionItem item = selectionItems[itemIndex];
        if(item == null) yield break;
        
        float duration = (hover) ? hoverDuration : unhoverDuration;
        float elapsed = 0f;
        
        float startOffset = GetCurrentTextOffset(item);
        
        Color targetItemColor = (hover) ? itemBlockHoverColor : itemBlockDefaultColor;
        float targetItemAlpha = (hover) ? itemBlockHoverOpacity : itemBlockDefaultOpacity;
        Color targetChildColor = (hover) ? childHoverColor : childDefaultColor;
        float targetChildAlpha = (hover) ? childHoverOpacity : childDefaultOpacity;
        Color targetTextColor = (hover) ? textHoverColor : textDefaultColor;
        Color targetHoverColor = (hover) ? new Color(1f, 1f, 1f, .3f) : new Color(1f, 1f, 1f, 0f);
        float targetOffset = (hover) ? 
            (originalTextOffsets.ContainsKey(item) ? originalTextOffsets[item] + textHoverOffset : startOffset + textHoverOffset) : 
            (originalTextOffsets.ContainsKey(item) ? originalTextOffsets[item] : startOffset);
        
        while(elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            if(item.hoverIndicator != null) item.hoverIndicator.Color = Color.Lerp(Color.clear, targetHoverColor, t);
            if(item.textBlock != null) item.textBlock.Color = Color.Lerp(textDefaultColor, targetTextColor, t);
            
            Color currentItemColor = Color.Lerp(itemBlockDefaultColor, targetItemColor, t);
            float currentItemAlpha = Mathf.Lerp(itemBlockDefaultOpacity, targetItemAlpha, t);
            SetItemBlockState(item, currentItemColor, currentItemAlpha);
            
            Color currentChildColor = Color.Lerp(childDefaultColor, targetChildColor, t);
            float currentChildAlpha = Mathf.Lerp(childDefaultOpacity, targetChildAlpha, t);
            SetChildState(item, currentChildColor, currentChildAlpha);
            
            float currentOffset = Mathf.Lerp(startOffset, targetOffset, t);
            UpdateTextOffset(item, currentOffset);
            
            yield return null;
        }
        
        if(item.hoverIndicator != null) item.hoverIndicator.Color = targetHoverColor;
        if(item.textBlock != null) item.textBlock.Color = targetTextColor;
        
        SetItemBlockState(item, targetItemColor, targetItemAlpha);
        SetChildState(item, targetChildColor, targetChildAlpha);
        UpdateTextOffset(item, targetOffset);
    }
    
    IEnumerator AnimateSidePanel(PauseSelectionPanel panel, bool show){
        if(panel.panelBlock == null) yield break;
        
        float duration = (show) ? .35f : .25f;
        float elapsed = 0f;
        
        if(show) panel.panelBlock.gameObject.SetActive(true);
        
        Color startColor = panel.panelBlock.Color;
        Color targetColor = (show) ? new Color(startColor.r, startColor.g, startColor.b, 1f) : new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while(elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            panel.panelBlock.Color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        panel.panelBlock.Color = targetColor;
        
        if(!show) panel.panelBlock.gameObject.SetActive(false);
    }
    #endregion
    
    #region STATE MANAGEMENT METHODS
    void SetItemBlockState(PauseSelectionItem item, Color color, float alpha){
        if(item.itemBlock != null){
            color.a = alpha;
            item.itemBlock.Color = color;
        }
    }
    
    void SetChildState(PauseSelectionItem item, Color color, float alpha){
        var firstChild = item.itemBlock?.GetChild(0);
        if(firstChild != null){
            var childUIBlock = firstChild.GetComponent<UIBlock2D>();
            if(childUIBlock != null){
                color.a = alpha;
                childUIBlock.Color = color;
            }
        }
    }
    
    void UpdateTextOffset(PauseSelectionItem item, float offset){
        var firstChild = item.itemBlock?.GetChild(0);
        if(firstChild != null) firstChild.Position.X.Value = offset;
    }
    
    float GetCurrentTextOffset(PauseSelectionItem item){
        var firstChild = item.itemBlock?.GetChild(0);
        if(firstChild != null) return firstChild.Position.X.Value;
        return originalTextOffsets.ContainsKey(item) ? originalTextOffsets[item] : 0f;
    }
    
    void CompleteHover(PauseSelectionItem item){
        if(item.selectionIndicator != null){
            item.selectionIndicator.Color = iconHoverColor;
            item.selectionIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, -selectionRotateAngle);
        }
        
        if(item.textBlock != null) item.textBlock.Color = textHoverColor;
        
        SetItemBlockState(item, itemBlockHoverColor, itemBlockHoverOpacity);
        SetChildState(item, childHoverColor, childHoverOpacity);
        
        float targetOffset = (originalTextOffsets.ContainsKey(item) ? originalTextOffsets[item] : 0f) + textHoverOffset;
        UpdateTextOffset(item, targetOffset);
    }
    
    void CompleteUnhover(PauseSelectionItem item){
        if(item.selectionIndicator != null){
            item.selectionIndicator.Color = iconDefaultColor;
            item.selectionIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, selectionRotateAngle);
        }
        
        if(item.textBlock != null) item.textBlock.Color = textDefaultColor;
        
        SetItemBlockState(item, itemBlockDefaultColor, itemBlockDefaultOpacity);
        SetChildState(item, childDefaultColor, childDefaultOpacity);
        
        float targetOffset = originalTextOffsets.ContainsKey(item) ? originalTextOffsets[item] : 0f;
        UpdateTextOffset(item, targetOffset);
        
        if(item.hoverIndicator != null) item.hoverIndicator.Color = new Color(1f, 1f, 1f, 0f);
    }
    #endregion
    
    #region STATE MANAGEMENT
    void ResetMenuState(){
        cursorVisible = false;
        isScrambling = false;
        
        for(int i = 0; i < originalLetters.Length; i++){
            if(i < titleLetters.Length && titleLetters[i] != null){
                titleLetters[i].Text = originalLetters[i].ToString();
                titleLetters[i].Color = textDefaultColor;
            }
        }
        
        foreach(var item in selectionItems){
            if(item == null || item.itemBlock == null) continue;
            
            item.itemBlock.transform.localPosition = originalPositions[item];
            item.itemBlock.transform.localScale = originalScales[item];
            
            SetItemBlockState(item, itemBlockDefaultColor, itemBlockDefaultOpacity);
            SetChildState(item, childDefaultColor, childDefaultOpacity);
            
            if(item.selectionIndicator != null){
                item.selectionIndicator.Color = iconDefaultColor;
                item.selectionIndicator.transform.localRotation = Quaternion.Euler(0f, 0f, selectionRotateAngle);
            }
            
            if(item.textBlock != null) item.textBlock.Color = textDefaultColor;
            if(item.hoverIndicator != null) item.hoverIndicator.Color = new Color(1f, 1f, 1f, 0f);
            if(item.itemBlock != null){
                var firstChild = item.itemBlock.GetChild(0);
                if(firstChild != null && originalTextOffsets.ContainsKey(item)) firstChild.Position.X.Value = originalTextOffsets[item];
            }
            
            if(itemToPanelMap.ContainsKey(item)) itemToPanelMap[item].Hide();
        }
        
        if(currentHoverRoutine != null){
            StopCoroutine(currentHoverRoutine);
            currentHoverRoutine = null;
        }
        
        if(currentSelectionRoutine != null){
            StopCoroutine(currentSelectionRoutine);
            currentSelectionRoutine = null;
        }
        
        lastHoveredIndex = -1;
    }
    
    void ResetMenuVisualState(){
        cursorVisible = false;
        
        foreach(var letter in titleLetters){
            if(letter != null){
                letter.Text = "";
                letter.Color = textDefaultColor;
            }
        }
        
        foreach(var item in selectionItems){
            if(item == null || item.itemBlock == null) continue;
            
            if(itemToPanelMap.ContainsKey(item)) itemToPanelMap[item].Hide();
        }
        
        if(currentHoverRoutine != null){
            StopCoroutine(currentHoverRoutine);
            currentHoverRoutine = null;
        }
        
        if(currentSelectionRoutine != null){
            StopCoroutine(currentSelectionRoutine);
            currentSelectionRoutine = null;
        }
        
        lastHoveredIndex = -1;
    }
    #endregion
}

[System.Serializable]
public class PauseSelectionItem
{
    [Header("REFERENCES")]
    public UIBlock2D itemBlock;
    public TextBlock textBlock;
    public UIBlock2D selectionIndicator;
    public UIBlock2D hoverIndicator;
    public PauseSelectionPanel targetPanel;
    
    public virtual void OnPressed() {}
    public virtual void OnHover() {}
    public virtual void OnUnhover() {}
}

[System.Serializable]
public class PauseSelectionPanel
{
    public UIBlock2D panelBlock;
    public virtual void Show(){ if(panelBlock != null) panelBlock.gameObject.SetActive(true); }
    public virtual void Hide(){ if(panelBlock != null) panelBlock.gameObject.SetActive(false); }
}