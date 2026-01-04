using UnityEngine;
using System.Collections;
using Nova;
using NovaSamples.UIControls;

public class OnlineForumVisual : MonoBehaviour
{
    [Header("UI BLOCKS")]
    public UIBlock2D randomTopic;
    public UIBlock2D evilTopic;

    [Header("MAC DECORATIONS")]
    public UIBlock2D[] decorativeBlocks = new UIBlock2D[3];
    public float lightCycleSpeed = 1.5f;
    public float litIntensity = 1.4f;
    public float unlitIntensity = 0.6f;
    private Color32[] originalColors;

    [Header("TOPIC TEXT FIELD")]
    public UIBlock2D topicTextField;
    public UIBlock2D SearchIcon;
    public Sprite[] searchState = new Sprite[2];
    public Sprite[] loadingState = new Sprite[4];

    [Header("REPLY TEXT FIELD")]
    public UIBlock2D replyTextField;
    public TextBlock replyPlaceholder;
    public TextBlock replyFill;
    public UIBlock2D replySendButton;

    [Header("HEADER TEXT FIELDS")]
    public TextBlock titleHeader;
    public TextBlock titleOpening;

    [Header("OVERLAYS")]
    public GameObject GridOverlayObject;
    public Color32 gridCenterColor = new Color32(255, 120, 120, 90);
    public Color32 gridEdgeColor = new Color32(120, 10, 10, 180);
    public float pulseSpeed = 2f;
    public byte pulseAmount = 40;
    public byte baseAlpha = 60;

    [Header("GAMEOBJECTS")]
    public GameObject HomeScreen;

    [Header("REFERENCES")]
    public OnlineDebateManager debateUIManager;
    public DictionaryManager dictionaryManager;
    
    private DebateDataManager debateDataManager;
    private UIBlock2D gridOverlay;
    private TextBlock topicTextBlock;

    private bool isTopicFocused = false;
    private bool isReplyTextFieldFocused = false;
    
    private bool isTopicLoading = false;
    private Coroutine loadingCoroutine;

    private string headerFallacy = "";
    private string starterHeader;
    private string starterOpening;

    #region INITIALIZATION
    void Start(){
        debateDataManager = DebateDataManager.Instance;
        gridOverlay = GridOverlayObject.GetComponent<UIBlock2D>();
        topicTextBlock = topicTextField.GetChild(0).GetComponent<TextBlock>();

        originalColors = new Color32[decorativeBlocks.Length];
        for(int i = 0; i < decorativeBlocks.Length; i++) originalColors[i] = decorativeBlocks[i].Color;

        starterHeader = titleHeader.Text;
        starterOpening = titleOpening.Text;

        //Topic section
        RegisterTopicButton(randomTopic);
        RegisterTopicButton(evilTopic);
        RegisterTopicTextField(topicTextField);

        //Reply section
        RegisterReply(replyTextField);
        RegisterSendButton(replySendButton);

        ResetAll();
    }

    void ResetAll(){
        randomTopic.Shadow.Enabled = false;
        evilTopic.Shadow.Enabled = false;
        topicTextField.Border.Enabled = false;

        replySendButton.Color = new Color32(171, 64, 65, 200);
        replySendButton.Gradient.Color = new Color32(202, 78, 64, 191);

        StopAllCoroutines();
        isTopicLoading = false;
        
        EnableTopicTextField();
        DisableReplyTextField();
    }

    void RegisterTopicButton(UIBlock2D block){
        block.AddGestureHandler<Gesture.OnPress>(e => {
            if(block == randomTopic) topicTextBlock.Text = dictionaryManager.GetRandomWord();
            else if(block == evilTopic) topicTextBlock.Text = dictionaryManager.GetEvilWord();
        });
        block.AddGestureHandler<Gesture.OnHover>(e => block.Shadow.Enabled = true);
        block.AddGestureHandler<Gesture.OnUnhover>(e => block.Shadow.Enabled = false);
    }

    void RegisterTopicTextField(UIBlock2D block){
        block.AddGestureHandler<Gesture.OnHover>(e => isTopicFocused = true);
        block.AddGestureHandler<Gesture.OnUnhover>(e => isTopicFocused = false);
    }

    void RegisterReply(UIBlock2D block){
        block.AddGestureHandler<Gesture.OnHover>(e => isReplyTextFieldFocused = true);
        block.AddGestureHandler<Gesture.OnUnhover>(e => isReplyTextFieldFocused = false);
    }

    void RegisterSendButton(UIBlock2D block){
        block.AddGestureHandler<Gesture.OnPress>(e => debateUIManager.SendReply());
        block.AddGestureHandler<Gesture.OnHover>(e => {
            if(IsSendButtonEnabled()){
                block.Color = new Color32(151, 44, 45, 200);
                block.Gradient.Color = new Color32(242, 98, 84, 191);
            }
        });
        block.AddGestureHandler<Gesture.OnUnhover>(e => {
            if(IsSendButtonEnabled()){
                block.Color = new Color32(171, 64, 65, 200);
                block.Gradient.Color = new Color32(202, 78, 64, 191);
            }
        });
    }
    #endregion

    #region UPDATE LOOP
    void Update(){
        HandleGrid();
        HandleDecorativeBlocks();
        HandleManualScroll();

        if(!HomeScreen.activeSelf) return;
        
        UpdateUIState();
        
        if(Input.GetMouseButtonDown(0)){
            topicTextField.Border.Enabled = isTopicFocused && !debateDataManager.isDebateActive;
            replyTextField.Border.Enabled = isReplyTextFieldFocused && debateDataManager.isDebateActive;
        }

        if(!debateDataManager.isDebateActive) FilterTopicTextField();
    }

    void UpdateUIState(){
        if(!debateDataManager.isDebateActive){
            if(isTopicLoading){
                DisableTopicTextField();
                DisableReplyTextField();
            }else{
                EnableTopicTextField();
                DisableReplyTextField();
            }
        }else{
            DisableTopicTextField();
            EnableReplyTextField();
            
            if(isTopicLoading){
                isTopicLoading = false;
                StopLoadingAnimation();
            }
        }
    }
    #endregion

    #region DECORATIONS & OVERLAY
    void HandleGrid(){
        GridOverlayObject.transform.Rotate(Vector3.forward, 3f * Time.deltaTime);

        byte pulse = (byte)(baseAlpha + pulseAmount * Mathf.Sin(Time.time * pulseSpeed));
        Color32 pulsedCenter = new Color32(gridCenterColor.r, gridCenterColor.g, gridCenterColor.b, pulse);

        gridOverlay.Color = gridEdgeColor;
        gridOverlay.Gradient.Color = pulsedCenter;
    }

    void HandleDecorativeBlocks(){
        float totalTime = Time.time * lightCycleSpeed;

        for(int i = 0; i < decorativeBlocks.Length; i++){
            UIBlock2D block = decorativeBlocks[i];
            Color32 baseColor = originalColors[i];

            float phase = totalTime - i * 0.6f;
            float t = (Mathf.Sin(phase) + 1f) * 0.5f;

            float intensity = Mathf.Lerp(unlitIntensity, litIntensity, t);
            Color newColor = new Color(
                baseColor.r / 255f * intensity,
                baseColor.g / 255f * intensity,
                baseColor.b / 255f * intensity,
                baseColor.a / 255f
            );

            block.Color = (Color32)newColor;
        }
    }
    #endregion

    #region TOPIC TEXT FIELD METHODS
    public void EnableTopicTextField(){
        var interactable = topicTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = true;

        var textField = topicTextField.GetComponent<TextField>();
        if(textField != null) textField.enabled = true;

        topicTextField.Color = new Color32(42, 48, 58, 255);
        topicTextField.Border.Enabled = false;

        if(SearchIcon != null){
            SearchIcon.Color = new Color32(148, 148, 148, 200);
            SearchIcon.SetImage(searchState.Length > 0 ? searchState[0] : null);
        }

        StopLoadingAnimation();
    }

    public void DisableTopicTextField(){
        var interactable = topicTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = false;

        var textField = topicTextField.GetComponent<TextField>();
        if(textField != null) textField.enabled = false;

        topicTextField.Color = new Color32(58, 43, 42, 200);
        topicTextField.Border.Enabled = false;
    }

    public void StartLoadingAnimation(){
        isTopicLoading = true;
        if(loadingCoroutine != null) StopCoroutine(loadingCoroutine);
        loadingCoroutine = StartCoroutine(AnimateLoadingIcon());
    }

    public void StopLoadingAnimation(){
        isTopicLoading = false;
        if(loadingCoroutine != null){
            StopCoroutine(loadingCoroutine);
            loadingCoroutine = null;
        }
        
        if(SearchIcon != null){
            SearchIcon.Color = new Color32(148, 148, 148, 200);
            SearchIcon.SetImage(searchState[1]);
        }
    }

    IEnumerator AnimateLoadingIcon(){
        int frame = 0;

        while(true){
            if(SearchIcon != null && loadingState.Length > 0){
                SearchIcon.Color = Color.white;
                SearchIcon.SetImage(loadingState[frame]);
            }
            yield return new WaitForSeconds(.075f);
            frame = (frame + 1) % loadingState.Length;
        }
    }

    void FilterTopicTextField(){
        TextField tf = topicTextField.GetComponent<TextField>();
        if(tf == null) return;

        string raw = tf.Text;
        if(string.IsNullOrEmpty(raw)) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach(char c in raw){
            if((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z')) sb.Append(c);
        }

        string clean = sb.ToString();
        if(clean != raw) tf.Text = clean;
    }
    #endregion

    #region REPLY TEXT FIELD METHODS
    void EnableReplyTextField(){
        var interactable = replyTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = true;

        replyPlaceholder.Color = new Color32(128, 128, 128, 100);
        replyTextField.Color = new Color32(51, 51, 51, 200);
        replyPlaceholder.Text = "Enter an argument...";

        if(replySendButton != null){
            var sendInteractable = replySendButton.GetComponent<Interactable>();
            if(sendInteractable != null) {
                sendInteractable.enabled = true;
                UpdateSendButtonAppearance(true);
            }
        }
    }

    void DisableReplyTextField(){
        var interactable = replyTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = false;

        replyPlaceholder.Color = new Color32(150, 100, 100, 100);
        replyTextField.Color = new Color32(58, 43, 42, 200);
        replyPlaceholder.Text = "Create or find a topic first";
        
        if(replySendButton != null){
            var sendInteractable = replySendButton.GetComponent<Interactable>();
            if(sendInteractable != null) {
                sendInteractable.enabled = false;
                UpdateSendButtonAppearance(false);
            }
        }
    }

    bool IsSendButtonEnabled() => debateDataManager != null && debateDataManager.isDebateActive;

    void UpdateSendButtonAppearance(bool enabled){
        if(replySendButton == null) return;
        
        if(enabled){
            replySendButton.Color = new Color32(171, 64, 65, 200);
            replySendButton.Gradient.Color = new Color32(242, 98, 84, 191);
            
            if(replySendButton.transform.childCount > 0){
                var child = replySendButton.GetChild(0);
                if(child != null) child.Color = new Color32(255, 255, 255, 200);
            }
        }else{
            replySendButton.Color = new Color32(52, 39, 38, 200);
            replySendButton.Gradient.Color = new Color32(52, 35, 35, 191);
            
            if(replySendButton.transform.childCount > 0){
                var child = replySendButton.GetChild(0);
                if(child != null) child.Color = new Color32(104, 70, 70, 200);
            }
        }
    }
    #endregion

    public void SetChosenFallacy(string fallacy) => headerFallacy = fallacy;

    #region REPLY TEXT FIELD SCROLL
    void HandleManualScroll(){
        float parentWidth = replyTextField.Size.X.Value;
        float childWidth = replyFill.Size.X.Value;
        if(!isReplyTextFieldFocused) return;

        if(childWidth < parentWidth){
            replyFill.Position.X.Value = 0f;
            return;
        }

        float overflow = (childWidth - parentWidth) + 25f;
        if(Input.GetKeyDown(KeyCode.Backspace)) replyFill.Position.X.Value = Mathf.Min(replyFill.Position.X.Value + overflow, 0f);
        else replyFill.Position.X.Value = -overflow;
    }
    #endregion
}