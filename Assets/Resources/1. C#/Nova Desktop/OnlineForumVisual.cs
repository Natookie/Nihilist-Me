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
    public TextBlock headerTextField;

    [Header("REPLY TEXT FIELD")]
    public UIBlock2D replyTextField;
    public TextBlock replyPlaceholder;
    public TextBlock replyFill;
    public UIBlock2D replySendButton;

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
    public OnlineDebateManager debateManager;
    public DictionaryManager dictionaryManager;

    private UIBlock2D gridOverlay;
    private TextBlock topicTextBlock;

    private bool isTopicFocused = false;
    private bool isLoading = false;
    private bool isReplyTextFieldFocused = false;
    private bool isDebateInitialize = false;

    private string headerFallacy = "";
    private string starterHeader;
    private string starterOpening;

    #region INITIALIZATION
    void Start(){
        gridOverlay = GridOverlayObject.GetComponent<UIBlock2D>();
        topicTextBlock = topicTextField.GetChild(0).GetComponent<TextBlock>();

        originalColors = new Color32[decorativeBlocks.Length];
        for(int i = 0; i < decorativeBlocks.Length; i++) originalColors[i] = decorativeBlocks[i].Color;

        starterHeader = debateManager.titleHeader.Text;
        starterOpening = debateManager.titleOpening.Text;

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

        if(debateManager.isDebateActive) EnableReplyTextField();
        else DisableReplyTextField();
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
        block.AddGestureHandler<Gesture.OnPress>(e => debateManager.SendReply());
        block.AddGestureHandler<Gesture.OnHover>(e => {
            block.Color = new Color32(151, 44, 45, 200);
            block.Gradient.Color = new Color32(242, 98, 84, 191);
        });
        block.AddGestureHandler<Gesture.OnUnhover>(e => {
            block.Color = new Color32(171, 64, 65, 200);
            block.Gradient.Color = new Color32(202, 78, 64, 191);
        });
        
        if(!debateManager.isDebateActive) block.GetComponent<Interactable>().enabled = false;
    }
    #endregion

    #region UPDATE LOOP
    void Update(){
        HandleGrid();
        HandleDecorativeBlocks();
        HandleManualScroll();

        if(!HomeScreen.activeSelf) return;
        HandleSendButton();

        if(Input.GetMouseButtonDown(0)){
            topicTextField.Border.Enabled = isTopicFocused;
            replyTextField.Border.Enabled = isReplyTextFieldFocused;
        }

        if(debateManager.isDebateActive){
            HandleTopicTextField();
            if(isTopicOnLoad()) DisableReplyTextField();
            else EnableReplyTextField();
        }else{
            DisableReplyTextField();
            FilterTopicTextField();
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

    void HandleSendButton(){
        if(isDebateInitialize) return;

        if(isTopicOnLoad() || !debateManager.isDebateActive){
            replySendButton.Color = new Color32(52, 39, 38, 200);
            replySendButton.Gradient.Color = new Color32(52, 35, 35, 191);

            var it = replySendButton.GetComponent<Interactable>();
            if(it != null) it.enabled = false;

            if(replySendButton.transform.childCount > 0){
                var child = replySendButton.GetChild(0);
                if(child != null) child.Color = new Color32(104, 70, 70, 200);
            }
        }else{
            replySendButton.Color = new Color32(171, 64, 65, 200);
            replySendButton.Gradient.Color = new Color32(242, 98, 84, 191);

            var it = replySendButton.GetComponent<Interactable>();
            if(it != null) it.enabled = true;

            if(replySendButton.transform.childCount > 0){
                var child = replySendButton.GetChild(0);
                if(child != null) child.Color = new Color32(255, 255, 255, 200);
            }

            isDebateInitialize = true;
        }
    }
    #endregion

    #region TOPIC TEXT FIELD
    bool isTopicOnLoad() => string.IsNullOrEmpty(headerTextField.Text) || headerTextField.Text == "";

    void HandleTopicTextField(){
        if(isTopicOnLoad()){
            SearchIcon.Color = Color.white;
            if(!isLoading){
                isLoading = true;
                StartCoroutine(AnimateLoadingIcon());
            }
            return;
        }

        if(isLoading){
            StopAllCoroutines();
            isLoading = false;
        }

        //Disable search
        SearchIcon.Color = new Color32(148, 148, 148, 200);
        SearchIcon.SetImage(searchState[1]);

        topicTextField.Border.Enabled = false;
        topicTextField.Color = new Color32(58, 43, 42, 200);

        var interactable = topicTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = false;

        var textField = topicTextField.GetComponent<TextField>();
        if(textField != null) textField.enabled = false;
    }

    public void EnableTopicTextField(){
        if(topicTextField == null) return;

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

        if(isLoading){
            StopAllCoroutines();
            isLoading = false;
        }

        debateManager.titleHeader.Text = starterHeader;
        debateManager.titleOpening.Text = starterOpening;
        DisableReplyTextField();
        isDebateInitialize = false;
    }

    IEnumerator AnimateLoadingIcon(){
        int frame = 0;

        while(true){
            SearchIcon.SetImage(loadingState[frame]);
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
        foreach(char c in raw)
            if((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                sb.Append(c);

        string clean = sb.ToString();
        if(clean != raw) tf.Text = clean;
    }
    #endregion

    #region REPLY TEXT FIELD
    void EnableReplyTextField(){
        var interactable = replyTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = true;

        replyPlaceholder.Color = new Color32(128, 128, 128, 100);
        replyTextField.Color = new Color32(51, 51, 51, 200);
        replyPlaceholder.Text = "Enter an argument...";
    }

    void DisableReplyTextField(){
        var interactable = replyTextField.GetComponent<Interactable>();
        if(interactable != null) interactable.enabled = false;

        replyPlaceholder.Color = new Color32(150, 100, 100, 100);
        replyTextField.Color = new Color32(58, 43, 42, 200);
        replyPlaceholder.Text = "Create or find a topic first";
    }
    #endregion

    public void SetChosenFallacy(string fallacy) => headerFallacy = fallacy;

    #region REPLY TEXT FIELD
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
