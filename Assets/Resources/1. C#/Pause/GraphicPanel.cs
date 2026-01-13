using UnityEngine;
using Nova;

public class GraphicPanel : MonoBehaviour
{
    [Header("UI CONFIG")]
    public UIBlock2D[] fullScreenBlock = new UIBlock2D[2];
    public UIBlock2D[] qualityBlock = new UIBlock2D[3];
    [Space(10)]
    public UIBlock2D resolutionBlock;
    public TextBlock resolutionText;
    
    [Header("TOGGLE COLOR SETTINGS")]
    [SerializeField] private Color toggleDefaultColor = Color.white;
    [SerializeField] private Color toggleHoverColor = new Color(.9f, .9f, 1f, 1f);
    [SerializeField] private Color toggleSelectedColor = new Color(.8f, .85f, 1f, 1f);
    [Space(10)]
    [SerializeField] private Color textDefaultColor = Color.black;
    [SerializeField] private Color textSelectedColor = Color.white;
    
    [Header("SLIDER COLOR SETTINGS")]
    [SerializeField] private Color sliderDefaultColor = Color.white;
    [SerializeField] private Color sliderHoverColor = new Color(.9f, .9f, 1f, 1f);
    [SerializeField] private Color sliderSelectedColor = new Color(.8f, .85f, 1f, 1f);
    
    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popDuration = .15f;
    
    private int fullScreenSelected = 0;
    private int qualitySelected = 1;
    private int resolutionSelected = 1;
    private int hoveredFullScreen = -1;
    private int hoveredQuality = -1;
    private bool isResolutionHovered = false;
    private bool isDraggingResolution = false;
    
    private float fullScreenPopTimer = 0f;
    private int fullScreenPopIndex = -1;
    private float qualityPopTimer = 0f;
    private int qualityPopIndex = -1;
    
    private readonly string[] resolutionLabels = new string[3]{
        "1366×768",
        "1600×900", 
        "1920×1080"
    };
    
    void Start(){
        InitializeUI();
    }
    
    void Update(){
        UpdateColors();
        HandleResolutionDrag();
        UpdatePopEffects();
    }
    
    void InitializeUI(){
        if(fullScreenBlock.Length >= 2){
            for(int i = 0; i < 2; i++){
                int index = i;
                fullScreenBlock[i].AddGestureHandler<Gesture.OnHover>(evt => OnFullScreenHover(evt, index));
                fullScreenBlock[i].AddGestureHandler<Gesture.OnUnhover>(evt => OnFullScreenUnhover(evt, index));
                fullScreenBlock[i].AddGestureHandler<Gesture.OnClick>(evt => OnFullScreenClick(evt, index));
                
                fullScreenBlock[i].Color = (i == fullScreenSelected) ? toggleSelectedColor : toggleDefaultColor;
                UpdateToggleTextColor(fullScreenBlock[i], i == fullScreenSelected);
            }
        }
        
        if(qualityBlock.Length >= 3){
            for(int i = 0; i < 3; i++){
                int index = i;
                qualityBlock[i].AddGestureHandler<Gesture.OnHover>(evt => OnQualityHover(evt, index));
                qualityBlock[i].AddGestureHandler<Gesture.OnUnhover>(evt => OnQualityUnhover(evt, index));
                qualityBlock[i].AddGestureHandler<Gesture.OnClick>(evt => OnQualityClick(evt, index));
                
                qualityBlock[i].Color = (i == qualitySelected) ? toggleSelectedColor : toggleDefaultColor;
                UpdateToggleTextColor(qualityBlock[i], i == qualitySelected);
            }
        }
        
        if(resolutionBlock != null){
            resolutionBlock.AddGestureHandler<Gesture.OnPress>(OnResolutionPress);
            resolutionBlock.AddGestureHandler<Gesture.OnRelease>(OnResolutionRelease);
            resolutionBlock.AddGestureHandler<Gesture.OnHover>(OnResolutionHover);
            resolutionBlock.AddGestureHandler<Gesture.OnUnhover>(OnResolutionUnhover);
            UpdateResolutionVisual();
        }
        
        if(resolutionText != null){
            resolutionText.Text = resolutionLabels[resolutionSelected];
        }
        
        ApplySettings();
    }
    
    void OnFullScreenHover(Gesture.OnHover evt, int index){
        hoveredFullScreen = index;
    }
    
    void OnFullScreenUnhover(Gesture.OnUnhover evt, int index){
        hoveredFullScreen = -1;
    }
    
    void OnFullScreenClick(Gesture.OnClick evt, int index){
        if(index == fullScreenSelected) return;
        
        fullScreenSelected = index;
        fullScreenPopIndex = index;
        fullScreenPopTimer = popDuration;
        ApplyFullScreenSetting();
    }
    
    void OnQualityHover(Gesture.OnHover evt, int index){
        hoveredQuality = index;
    }
    
    void OnQualityUnhover(Gesture.OnUnhover evt, int index){
        hoveredQuality = -1;
    }
    
    void OnQualityClick(Gesture.OnClick evt, int index){
        if(index == qualitySelected) return;
        
        qualitySelected = index;
        qualityPopIndex = index;
        qualityPopTimer = popDuration;
        ApplyQualitySetting();
    }
    
    void OnResolutionHover(Gesture.OnHover evt){
        isResolutionHovered = true;
    }
    
    void OnResolutionUnhover(Gesture.OnUnhover evt){
        isResolutionHovered = false;
        if(!isDraggingResolution && resolutionBlock != null){
            resolutionBlock.Color = sliderDefaultColor;
        }
    }
    
    void OnResolutionPress(Gesture.OnPress evt){
        isDraggingResolution = true;
        if(resolutionBlock != null) resolutionBlock.Color = sliderSelectedColor;
    }
    
    void OnResolutionRelease(Gesture.OnRelease evt){
        isDraggingResolution = false;
        UpdateResolutionVisual();
        ApplyResolutionSetting();
    }
    
    void HandleResolutionDrag(){
        if(!isDraggingResolution || resolutionBlock == null) return;
        
        UIBlock parentUIBlock = resolutionBlock.transform.parent.GetComponent<UIBlock>();
        if(parentUIBlock == null) return;
        
        float parentWidth = parentUIBlock.Size.X.Value;
        float blockWidth = resolutionBlock.Size.X.Value;
        float halfParentWidth = parentWidth * .5f;
        float halfBlockWidth = blockWidth * .5f;
        
        Vector2 mousePos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane + 1f));
        Vector3 localPos = resolutionBlock.transform.parent.InverseTransformPoint(worldPos);
        
        float mousePercent = (localPos.x + halfParentWidth) / parentWidth;
        mousePercent = Mathf.Clamp01(mousePercent);
        
        int segmentIndex = 1;
        float targetX = 0f;
        
        if(mousePercent < 1f/3f){
            segmentIndex = 0;
            targetX = -halfParentWidth + halfBlockWidth;
        }
        else if(mousePercent < 2f/3f){
            segmentIndex = 1;
            targetX = 0f;
        }
        else{
            segmentIndex = 2;
            targetX = halfParentWidth - halfBlockWidth;
        }
        
        resolutionBlock.Position.X.Value = targetX;
        
        if(segmentIndex != resolutionSelected){
            resolutionSelected = segmentIndex;
            if(resolutionText != null) resolutionText.Text = resolutionLabels[resolutionSelected];
        }
    }
    
    void UpdateResolutionVisual(){
        if(resolutionBlock == null) return;
        
        UIBlock parentUIBlock = resolutionBlock.transform.parent.GetComponent<UIBlock>();
        if(parentUIBlock == null) return;
        
        float parentWidth = parentUIBlock.Size.X.Value;
        float blockWidth = resolutionBlock.Size.X.Value;
        float halfParentWidth = parentWidth * .5f;
        float halfBlockWidth = blockWidth * .5f;
        
        float targetX = resolutionSelected switch{
            0 => -halfParentWidth + halfBlockWidth,
            1 => 0f,
            2 => halfParentWidth - halfBlockWidth,
            _ => 0f
        };
        
        resolutionBlock.Position.X.Value = targetX;
        
        if(resolutionText != null) resolutionText.Text = resolutionLabels[resolutionSelected];
    }
    
    void UpdateColors(){
        if(fullScreenBlock.Length >= 2){
            for(int i = 0; i < 2; i++){
                if(i == fullScreenPopIndex && fullScreenPopTimer > 0f) continue;
                
                Color targetColor = toggleDefaultColor;
                if(i == fullScreenSelected) targetColor = toggleSelectedColor;
                else if(i == hoveredFullScreen) targetColor = toggleHoverColor;
                
                fullScreenBlock[i].Color = Color.Lerp(fullScreenBlock[i].Color, targetColor, Time.unscaledDeltaTime * 15f);
                UpdateToggleTextColor(fullScreenBlock[i], i == fullScreenSelected);
            }
        }
        
        if(qualityBlock.Length >= 3){
            for(int i = 0; i < 3; i++){
                if(i == qualityPopIndex && qualityPopTimer > 0f) continue;
                
                Color targetColor = toggleDefaultColor;
                if(i == qualitySelected) targetColor = toggleSelectedColor;
                else if(i == hoveredQuality) targetColor = toggleHoverColor;
                
                qualityBlock[i].Color = Color.Lerp(qualityBlock[i].Color, targetColor, Time.unscaledDeltaTime * 15f);
                UpdateToggleTextColor(qualityBlock[i], i == qualitySelected);
            }
        }
        
        if(!isDraggingResolution && resolutionBlock != null){
            Color targetColor = isResolutionHovered ? sliderHoverColor : sliderDefaultColor;
            resolutionBlock.Color = Color.Lerp(resolutionBlock.Color, targetColor, Time.unscaledDeltaTime * 15f);
        }
    }
    
    void UpdateToggleTextColor(UIBlock2D toggleBlock, bool isSelected){
        if(toggleBlock.transform.childCount > 0){
            TextBlock textBlock = toggleBlock.transform.GetChild(0).GetComponent<TextBlock>();
            if(textBlock != null){
                int index = GetToggleIndex(toggleBlock);
                bool isPopping = (IsFullScreenToggle(index) && index == fullScreenPopIndex && fullScreenPopTimer > 0f) ||
                                (IsQualityToggle(index) && index == qualityPopIndex && qualityPopTimer > 0f);
                
                if(!isPopping){
                    Color targetColor = isSelected ? textSelectedColor : textDefaultColor;
                    textBlock.Color = Color.Lerp(textBlock.Color, targetColor, Time.unscaledDeltaTime * 15f);
                }
            }
        }
    }
    
    void UpdatePopEffects(){
        if(fullScreenPopTimer > 0f && fullScreenPopIndex >= 0 && fullScreenPopIndex < 2){
            UpdateSingleTextPopEffect(fullScreenBlock[fullScreenPopIndex], ref fullScreenPopTimer);
        }
        
        if(qualityPopTimer > 0f && qualityPopIndex >= 0 && qualityPopIndex < 3){
            UpdateSingleTextPopEffect(qualityBlock[qualityPopIndex], ref qualityPopTimer);
        }
    }
    
    void UpdateSingleTextPopEffect(UIBlock2D block, ref float timer){
        if(block == null || block.transform.childCount == 0) return;
        
        timer -= Time.unscaledDeltaTime;
        float progress = 1f - (timer / popDuration);
        
        float scale = 1f;
        if(progress < .5f){
            float expandProgress = progress / .5f;
            scale = Mathf.Lerp(1f, popScale, expandProgress);
        }else{
            float shrinkProgress = (progress - .5f) / .5f;
            scale = Mathf.Lerp(popScale, 1f, shrinkProgress);
            
            if(progress >= .5f && progress < .55f){
                block.Color = toggleSelectedColor;
                
                TextBlock textBlock = block.transform.GetChild(0).GetComponent<TextBlock>();
                if(textBlock != null) textBlock.Color = textSelectedColor;
            }
        }
        
        Transform textChild = block.transform.GetChild(0);
        textChild.localScale = Vector3.one * scale;
        
        if(timer <= 0f){
            timer = 0f;
            Transform textChildFinal = block.transform.GetChild(0);
            textChildFinal.localScale = Vector3.one;
        }
    }
    
    int GetToggleIndex(UIBlock2D toggleBlock){
        for(int i = 0; i < 2; i++) if(fullScreenBlock[i] == toggleBlock) return i;
        for(int i = 0; i < 3; i++) if(qualityBlock[i] == toggleBlock) return i;
        
        return -1;
    }
    
    bool IsFullScreenToggle(int index) => index >= 0 && index < 2;
    bool IsQualityToggle(int index) => index >= 0 && index < 3;
    
    void ApplyFullScreenSetting(){
        bool isFullScreen = fullScreenSelected == 0;
        Screen.fullScreen = isFullScreen;
    }
    
    void ApplyQualitySetting(){
        QualitySettings.SetQualityLevel(qualitySelected, true);
    }
    
    void ApplyResolutionSetting(){
        Vector2Int resolution = resolutionSelected switch{
            0 => new Vector2Int(1366, 768),
            1 => new Vector2Int(1600, 900),
            2 => new Vector2Int(1920, 1080),
            _ => new Vector2Int(1600, 900)
        };
        
        Screen.SetResolution(resolution.x, resolution.y, Screen.fullScreen);
    }
    
    void ApplySettings(){
        ApplyFullScreenSetting();
        ApplyQualitySetting();
        ApplyResolutionSetting();
    }
    
    public void SetFullScreen(bool isOn){
        int targetIndex = isOn ? 0 : 1;
        if(targetIndex == fullScreenSelected) return;
        
        fullScreenSelected = targetIndex;
        fullScreenPopIndex = fullScreenSelected;
        fullScreenPopTimer = popDuration;
        ApplyFullScreenSetting();
    }
    
    public void SetQuality(int level){
        if(level < 0 || level >= 3) return;
        if(level == qualitySelected) return;
        
        qualitySelected = level;
        qualityPopIndex = level;
        qualityPopTimer = popDuration;
        ApplyQualitySetting();
    }
    
    public void SetResolution(int index){
        if(index < 0 || index >= 3) return;
        
        resolutionSelected = index;
        UpdateResolutionVisual();
        ApplyResolutionSetting();
    }
}