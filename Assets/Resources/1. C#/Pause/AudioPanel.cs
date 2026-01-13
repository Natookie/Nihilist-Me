using UnityEngine;
using Nova;

public class AudioPanel : MonoBehaviour
{
    [Header("UI CONFIG")]
    public UIBlock2D musicMuteButton;
    public UIBlock2D ambienceMuteButton;
    public UIBlock2D sfxMuteButton;
    [Space(10)]
    public UIBlock2D musicSlider;
    public UIBlock2D ambienceSlider;
    public UIBlock2D sfxSlider;
    [Space(10)]
    public TextBlock musicVolumeText;
    public TextBlock ambienceVolumeText;
    public TextBlock sfxVolumeText;
    
    [Header("BUTTON SPRITES")]
    [SerializeField] private Sprite[] buttonSprites; //[0] Mute, [1-3] Volume levels
    
    [Header("BUTTON COLOR SETTINGS")]
    [SerializeField] private Color buttonDefaultColor = Color.white;
    [SerializeField] private Color buttonHoverColor = new Color(.9f, .9f, 1f, 1f);
    [SerializeField] private Color buttonSelectedColor = new Color(.8f, .85f, 1f, 1f);
    [Space(10)]
    [SerializeField] private Color buttonIconDefaultColor = Color.white;
    [SerializeField] private Color buttonIconMutedColor = new Color(.8f, .85f, 1f, 1f);
    [SerializeField] private Color buttonTextDefaultColor = Color.black;
    [SerializeField] private Color buttonTextSelectedColor = Color.white;
    
    [Header("SLIDER COLOR SETTINGS")]
    [SerializeField] private Color sliderDefaultColor = Color.white;
    [SerializeField] private Color sliderHoverColor = new Color(.9f, .9f, 1f, 1f);
    [SerializeField] private Color sliderSelectedColor = new Color(.8f, .85f, 1f, 1f);
    [SerializeField] private Color sliderDisabledColor = new Color(.6f, .6f, .7f, .5f);
    
    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popDuration = .15f;
    [SerializeField] private float stretchFactor = 5f;
    [SerializeField] private float stretchSpeed = 12f;
    [SerializeField] private float smoothSpeed = 15f;
    [SerializeField] private float maxStretchScale = 1.2f;
    
    [Header("VOLUME SETTINGS")]
    [SerializeField] private float defaultVolume = 1f;
    
    private bool musicMuted = false;
    private bool ambienceMuted = false;
    private bool sfxMuted = false;
    
    private float musicVolume = 1f;
    private float ambienceVolume = 1f;
    private float sfxVolume = 1f;
    
    private int hoveredMuteButton = -1;
    private int pressedMuteButton = -1;
    private bool isMusicSliderHovered = false;
    private bool isAmbienceSliderHovered = false;
    private bool isSfxSliderHovered = false;
    
    private bool isDraggingMusicSlider = false;
    private bool isDraggingAmbienceSlider = false;
    private bool isDraggingSfxSlider = false;
    
    private float mutePopTimer = 0f;
    private int mutePopIndex = -1;
    
    private float musicSliderStretch = 1f;
    private float ambienceSliderStretch = 1f;
    private float sfxSliderStretch = 1f;
    
    private UIBlock2D[] muteButtons;
    private UIBlock2D[] sliders;
    private TextBlock[] volumeTexts;
    private Interactable[] sliderInteractables;
    private UIBlock[] sliderParents;
    private float[] sliderBaseWidths;
    
    void Start(){
        CacheReferences();
        InitializeUI();
    }
    
    void CacheReferences(){
        muteButtons = new UIBlock2D[] { musicMuteButton, ambienceMuteButton, sfxMuteButton };
        sliders = new UIBlock2D[] { musicSlider, ambienceSlider, sfxSlider };
        volumeTexts = new TextBlock[] { musicVolumeText, ambienceVolumeText, sfxVolumeText };
        
        sliderInteractables = new Interactable[3];
        sliderParents = new UIBlock[3];
        sliderBaseWidths = new float[3];
        
        for(int i = 0; i < 3; i++){
            if(sliders[i] != null){
                sliderInteractables[i] = sliders[i].GetComponent<Interactable>();
                sliderParents[i] = sliders[i].transform.parent.GetComponent<UIBlock>();
                sliderBaseWidths[i] = sliders[i].Size.X.Value;
            }
        }
    }
    
    void Update(){
        float deltaTime = Time.unscaledDeltaTime;
        UpdateColors(deltaTime);
        HandleSliderDrag();
        UpdatePopEffects(deltaTime);
        UpdateSliderStretch(deltaTime);
    }
    
    void InitializeUI(){
        for(int i = 0; i < 3; i++){
            if(muteButtons[i] != null){
                int index = i;
                muteButtons[i].AddGestureHandler<Gesture.OnHover>(evt => OnMuteButtonHover(index));
                muteButtons[i].AddGestureHandler<Gesture.OnUnhover>(evt => OnMuteButtonUnhover(index));
                muteButtons[i].AddGestureHandler<Gesture.OnPress>(evt => OnMuteButtonPress(index));
                muteButtons[i].AddGestureHandler<Gesture.OnRelease>(evt => OnMuteButtonRelease(index));
                muteButtons[i].AddGestureHandler<Gesture.OnClick>(evt => OnMuteButtonClick(index));
                muteButtons[i].Color = buttonDefaultColor;
                UpdateMuteButtonVisual(i);
            }
            
            if(sliders[i] != null){
                int index = i;
                sliders[i].AddGestureHandler<Gesture.OnPress>(evt => OnSliderPress(index));
                sliders[i].AddGestureHandler<Gesture.OnRelease>(evt => OnSliderRelease(index));
                sliders[i].AddGestureHandler<Gesture.OnHover>(evt => OnSliderHover(index));
                sliders[i].AddGestureHandler<Gesture.OnUnhover>(evt => OnSliderUnhover(index));
                UpdateSliderVisual(i);
                UpdateSliderInteractivity(i);
            }
        }
        
        ApplyAudioSettings();
    }
    
    #region MUTE BUTTON
    void OnMuteButtonHover(int index) => hoveredMuteButton = index;
    void OnMuteButtonUnhover(int index){ if(hoveredMuteButton == index) hoveredMuteButton = -1; }
    
    void OnMuteButtonPress(int index) => pressedMuteButton = index;
    void OnMuteButtonRelease(int index){ if(pressedMuteButton == index) pressedMuteButton = -1; }
    
    void OnMuteButtonClick(int index){
        mutePopIndex = index;
        mutePopTimer = popDuration;
        
        switch(index){
            case 0: musicMuted = !musicMuted; break;
            case 1: ambienceMuted = !ambienceMuted; break;
            case 2: sfxMuted = !sfxMuted; break;
        }
        
        UpdateMuteButtonVisual(index);
        UpdateSliderInteractivity(index);
        ApplyAudioSettings();
    }
    #endregion
    
    #region SLIDER LOGIC
    void OnSliderHover(int index){
        switch(index){
            case 0: isMusicSliderHovered = true; break;
            case 1: isAmbienceSliderHovered = true; break;
            case 2: isSfxSliderHovered = true; break;
        }
    }
    
    void OnSliderUnhover(int index){
        switch(index){
            case 0: isMusicSliderHovered = false; break;
            case 1: isAmbienceSliderHovered = false; break;
            case 2: isSfxSliderHovered = false; break;
        }
        
        if(sliders[index] != null && !IsDraggingSlider(index)) sliders[index].Color = GetSliderColor(index);
    }
    
    void OnSliderPress(int index){
        switch(index){
            case 0: isDraggingMusicSlider = true; break;
            case 1: isDraggingAmbienceSlider = true; break;
            case 2: isDraggingSfxSlider = true; break;
        }
        
        if(sliders[index] != null) sliders[index].Color = sliderSelectedColor;
    }
    
    void OnSliderRelease(int index){
        switch(index){
            case 0: isDraggingMusicSlider = false; break;
            case 1: isDraggingAmbienceSlider = false; break;
            case 2: isDraggingSfxSlider = false; break;
        }
        
        UpdateMuteButtonVisual(index);
        ResetSliderStretch(index);
    }
    
    void HandleSliderDrag(){
        if(isDraggingMusicSlider) HandleSingleSliderDrag(0);
        if(isDraggingAmbienceSlider) HandleSingleSliderDrag(1);
        if(isDraggingSfxSlider) HandleSingleSliderDrag(2);
    }
    
    void HandleSingleSliderDrag(int index){
        if(sliders[index] == null || sliderParents[index] == null) return;
        
        float parentWidth = sliderParents[index].Size.X.Value;
        float sliderWidth = sliderBaseWidths[index];
        float halfParentWidth = parentWidth * .5f;
        float halfSliderWidth = sliderWidth * .5f;
        
        Vector2 mousePos = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane + 1f));
        Vector3 localPos = sliders[index].transform.parent.InverseTransformPoint(worldPos);
        
        float mousePercent = (localPos.x + halfParentWidth) / parentWidth;
        
        float stretchAmount = 0f;
        if(mousePercent < 0f){
            stretchAmount = Mathf.Abs(mousePercent) * stretchFactor;
            mousePercent = 0f;
        }else if(mousePercent > 1f){
            stretchAmount = (mousePercent - 1f) * stretchFactor;
            mousePercent = 1f;
        }
        
        float volume = Mathf.Clamp01(mousePercent);
        
        switch(index){
            case 0: musicVolume = volume; break;
            case 1: ambienceVolume = volume; break;
            case 2: sfxVolume = volume; break;
        }
        
        switch(index){
            case 0: musicSliderStretch = Mathf.Clamp(1f + stretchAmount, 1f, maxStretchScale); break;
            case 1: ambienceSliderStretch = Mathf.Clamp(1f + stretchAmount, 1f, maxStretchScale); break;
            case 2: sfxSliderStretch = Mathf.Clamp(1f + stretchAmount, 1f, maxStretchScale); break;
        }
        
        float targetX = Mathf.Lerp(-halfParentWidth + halfSliderWidth, halfParentWidth - halfSliderWidth, volume);
        sliders[index].Position.X.Value = targetX;
        
        if(volumeTexts[index] != null){
            int volumePercent = Mathf.RoundToInt(volume * 100f);
            volumeTexts[index].Text = $"{volumePercent}%";
        }
        
        UpdateAudioVolume();
        UpdateMuteButtonVisual(index);
    }
    #endregion
    
    #region VISUAL LOGIC
    void UpdateColors(float deltaTime){
        for(int i = 0; i < 3; i++){
            if(muteButtons[i] == null || (i == mutePopIndex && mutePopTimer > 0f)) continue;
            
            Color targetColor = GetMuteButtonColor(i);
            muteButtons[i].Color = Color.Lerp(muteButtons[i].Color, targetColor, deltaTime * smoothSpeed);
            
            bool isMuted = GetMutedState(i);
            UpdateMuteButtonTextColor(i, isMuted, deltaTime);
            UpdateMuteButtonIconColor(i, isMuted, deltaTime);
        }
        
        for(int i = 0; i < 3; i++){
            if(sliders[i] == null || GetMutedState(i) || IsDraggingSlider(i)) continue;
            sliders[i].Color = Color.Lerp(sliders[i].Color, GetSliderColor(i), deltaTime * smoothSpeed);
        }
    }
    
    void UpdateMuteButtonVisual(int index){
        if(muteButtons[index] == null || buttonSprites == null || buttonSprites.Length < 4) return;
        
        Transform icon = muteButtons[index].transform.childCount > 0 ? muteButtons[index].transform.GetChild(0) : null;
        if(icon == null || icon.childCount == 0) return;
        
        UIBlock2D spriteBlock = icon.GetChild(0).GetComponent<UIBlock2D>();
        if(spriteBlock == null) return;
        
        bool isMuted = GetMutedState(index);
        float volume = GetVolume(index);
        
        if(isMuted || volume <= .01f) spriteBlock.SetImage(buttonSprites[0]);
        else{
            int spriteIndex = 1;
            if(volume > .66f) spriteIndex = 3;else if(volume > .33f) spriteIndex = 2;
            spriteBlock.SetImage(buttonSprites[spriteIndex]);
        }
        
        spriteBlock.Color = isMuted ? buttonIconMutedColor : buttonIconDefaultColor;
    }
    
    void UpdateMuteButtonTextColor(int index, bool isMuted, float deltaTime){
        if(muteButtons[index] == null || muteButtons[index].transform.childCount == 0) return;
        
        TextBlock textBlock = muteButtons[index].transform.GetChild(0).GetComponent<TextBlock>();
        if(textBlock == null || (index == mutePopIndex && mutePopTimer > 0f)) return;
        
        Color targetColor = isMuted ? buttonTextSelectedColor : buttonTextDefaultColor;
        textBlock.Color = Color.Lerp(textBlock.Color, targetColor, deltaTime * smoothSpeed);
    }
    
    void UpdateMuteButtonIconColor(int index, bool isMuted, float deltaTime){
        if(muteButtons[index] == null || muteButtons[index].transform.childCount == 0) return;
        
        Transform icon = muteButtons[index].transform.GetChild(0);
        if(icon == null || icon.childCount == 0) return;
        
        UIBlock2D spriteBlock = icon.GetChild(0).GetComponent<UIBlock2D>();
        if(spriteBlock == null) return;
        
        Color targetIconColor = isMuted ? buttonIconMutedColor : buttonIconDefaultColor;
        spriteBlock.Color = Color.Lerp(spriteBlock.Color, targetIconColor, deltaTime * smoothSpeed);
    }
    
    void UpdateSliderVisual(int index){
        if(sliders[index] == null || sliderParents[index] == null) return;
        
        float parentWidth = sliderParents[index].Size.X.Value;
        float sliderWidth = sliderBaseWidths[index];
        float halfParentWidth = parentWidth * .5f;
        float halfSliderWidth = sliderWidth * .5f;
        
        float volume = GetVolume(index);
        float visualVolume = GetMutedState(index) ? 0f : volume;
        
        float targetX = Mathf.Lerp(-halfParentWidth + halfSliderWidth, halfParentWidth - halfSliderWidth, visualVolume);
        sliders[index].Position.X.Value = targetX;
        
        if(volumeTexts[index] != null){
            int volumePercent = Mathf.RoundToInt(visualVolume * 100f);
            volumeTexts[index].Text = $"{volumePercent}%";
        }
    }
    
    void UpdateSliderStretch(float deltaTime){
        if(musicSlider != null){
            float targetScaleX = Mathf.Lerp(musicSlider.transform.localScale.x, musicSliderStretch, deltaTime * stretchSpeed);
            musicSlider.transform.localScale = new Vector3(targetScaleX, 1f, 1f);
        }
        
        if(ambienceSlider != null){
            float targetScaleX = Mathf.Lerp(ambienceSlider.transform.localScale.x, ambienceSliderStretch, deltaTime * stretchSpeed);
            ambienceSlider.transform.localScale = new Vector3(targetScaleX, 1f, 1f);
        }
        
        if(sfxSlider != null){
            float targetScaleX = Mathf.Lerp(sfxSlider.transform.localScale.x, sfxSliderStretch, deltaTime * stretchSpeed);
            sfxSlider.transform.localScale = new Vector3(targetScaleX, 1f, 1f);
        }
    }
    
    void ResetSliderStretch(int index){
        switch(index){
            case 0: musicSliderStretch = 1f; break;
            case 1: ambienceSliderStretch = 1f; break;
            case 2: sfxSliderStretch = 1f; break;
        }
    }
    
    void UpdatePopEffects(float deltaTime){
        if(mutePopTimer <= 0f || mutePopIndex < 0 || mutePopIndex >= 3) return;
        
        mutePopTimer -= deltaTime;
        float progress = 1f - (mutePopTimer / popDuration);
        
        float scale = 1f;
        if(progress < .5f) scale = Mathf.Lerp(1f, popScale, progress / .5f);else scale = Mathf.Lerp(popScale, 1f, (progress - .5f) / .5f);
        
        Transform textChild = muteButtons[mutePopIndex].transform.GetChild(0);
        textChild.localScale = Vector3.one * scale;
        
        if(mutePopTimer <= 0f){
            mutePopTimer = 0f;
            textChild.localScale = Vector3.one;
            mutePopIndex = -1;
        }
    }
    #endregion
    
    #region UTIL
    bool GetMutedState(int index){
        return index switch{
            0 => musicMuted,
            1 => ambienceMuted,
            2 => sfxMuted,
            _ => false
        };
    }
    
    float GetVolume(int index){
        return index switch{
            0 => musicVolume,
            1 => ambienceVolume,
            2 => sfxVolume,
            _ => 0f
        };
    }
    
    bool IsDraggingSlider(int index){
        return index switch{
            0 => isDraggingMusicSlider,
            1 => isDraggingAmbienceSlider,
            2 => isDraggingSfxSlider,
            _ => false
        };
    }
    
    Color GetSliderColor(int index){
        bool isHovered = index switch{
            0 => isMusicSliderHovered,
            1 => isAmbienceSliderHovered,
            2 => isSfxSliderHovered,
            _ => false
        };
        
        if(GetMutedState(index)) return sliderDisabledColor;
        return isHovered ? sliderHoverColor : sliderDefaultColor;
    }
    
    Color GetMuteButtonColor(int index){
        if(index == pressedMuteButton || index == hoveredMuteButton) return buttonHoverColor;
        if(GetMutedState(index)) return buttonSelectedColor;
        return buttonDefaultColor;
    }
    
    void UpdateSliderInteractivity(int index){
        if(sliders[index] == null) return;
        
        bool isMuted = GetMutedState(index);
        sliders[index].Color = isMuted ? sliderDisabledColor : GetSliderColor(index);
        
        if(sliderInteractables[index] != null)
            sliderInteractables[index].enabled = !isMuted;
    }
    #endregion
    
    void UpdateAudioVolume(){
        float musicVol = musicMuted ? 0f : musicVolume;
        float ambienceVol = ambienceMuted ? 0f : ambienceVolume;
        float sfxVol = sfxMuted ? 0f : sfxVolume;
        
        if(AudioManager.Instance != null){
            AudioManager.Instance.SetVolume(AudioChannel.Music, musicVol);
            AudioManager.Instance.SetVolume(AudioChannel.Ambience, ambienceVol);
            AudioManager.Instance.SetVolume(AudioChannel.SFX, sfxVol);
        }else AudioListener.volume = sfxVol;
    }
    
    void ApplyAudioSettings(){
        UpdateAudioVolume();
        for(int i = 0; i < 3; i++) UpdateSliderVisual(i);
    }
    
    #region STE VOLUME
    public void SetMusicMuted(bool muted){
        if(musicMuted == muted) return;
        musicMuted = muted;
        mutePopIndex = 0;
        mutePopTimer = popDuration;
        UpdateMuteButtonVisual(0);
        UpdateSliderInteractivity(0);
        ApplyAudioSettings();
    }
    
    public void SetAmbienceMuted(bool muted){
        if(ambienceMuted == muted) return;
        ambienceMuted = muted;
        mutePopIndex = 1;
        mutePopTimer = popDuration;
        UpdateMuteButtonVisual(1);
        UpdateSliderInteractivity(1);
        ApplyAudioSettings();
    }
    
    public void SetSfxMuted(bool muted){
        if(sfxMuted == muted) return;
        sfxMuted = muted;
        mutePopIndex = 2;
        mutePopTimer = popDuration;
        UpdateMuteButtonVisual(2);
        UpdateSliderInteractivity(2);
        ApplyAudioSettings();
    }
    
    public void SetMusicVolume(float volume){
        musicVolume = Mathf.Clamp01(volume);
        UpdateMuteButtonVisual(0);
        UpdateSliderVisual(0);
        UpdateAudioVolume();
    }
    
    public void SetAmbienceVolume(float volume){
        ambienceVolume = Mathf.Clamp01(volume);
        UpdateMuteButtonVisual(1);
        UpdateSliderVisual(1);
        UpdateAudioVolume();
    }
    
    public void SetSfxVolume(float volume){
        sfxVolume = Mathf.Clamp01(volume);
        UpdateMuteButtonVisual(2);
        UpdateSliderVisual(2);
        UpdateAudioVolume();
    }
    #endregion
}