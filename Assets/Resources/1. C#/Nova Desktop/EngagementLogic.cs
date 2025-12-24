using UnityEngine;
using System.Collections;
using Nova;

public class EngagementLogic : MonoBehaviour
{
    [Header("REFERENCES")]
    public OnlineDebateManager debateManager;
    public OnlineForumVisual onlineForumVisual;
    public UIBlock2D likeBlock;
    public UIBlock2D commentBlock;
    public UIBlock2D hintBlock;

    [Header("ENGAGEMENT ANIMATION")]
    public Color clickedColor = Color.red;
    public Color defaultColor = Color.white;
    public float colorLerpSpeed = 8f;
    public float rotationDuration = .3f;

    [Header("HINT ANIMATION")]
    public float hintRevealSpeed = 15f;
    public Sprite[] hintState = new Sprite[2];

    [Header("NOTIFICATION")]
    public Color32 greenNotif;
    public Color32 redNotif;
    public UIBlock2D topPart;
    public UIBlock2D notificationBlock;
    public GameObject[] notificationPart = new GameObject[3];
    private string topicWord;
    [Space(10)]
    public float notificationSlideDuration = .4f;
    public float notificationBounceHeight = 12f;
    public float notificationBounceDuration = .3f;
    [Space(10)]
    public UIBlock2D retryButton;
    public UIBlock2D statisticButton;
    public Color clickedNotifColor = Color.white;
    public Color defaultNotifColor = new Color32(128, 128, 128, 255);

    private int baseLikes, baseComments;
    private string headerFallacy;
    
    private bool likeClicked = false, commentClicked = false;
    private bool likeHovered = false, commentHovered = false;
    private bool hintRevealing = false;
    
    private Color likeTargetColor, commentTargetColor;
    private Coroutine activeNotificationCoroutine;

    void Start(){
        likeTargetColor = defaultColor;
        commentTargetColor = defaultColor;
    }

    public void Init(string topic){
        RegisterEngagementIcons();
        RegisterHint();
        topicWord = topic;
    }

    void Update(){
        HandleColorAnimations();
    }

    void HandleColorAnimations(){
        UIBlock2D likeIcon = likeBlock.GetChild(0) as UIBlock2D;
        TextBlock likeText = likeBlock.GetChild(1) as TextBlock;
        
        if(likeIcon != null){
            likeIcon.Color = Color.Lerp(likeIcon.Color, likeTargetColor, Time.deltaTime * colorLerpSpeed);
            if(likeText != null) likeText.Color = Color.Lerp(likeText.Color, likeTargetColor, Time.deltaTime * colorLerpSpeed);
        }

        UIBlock2D commentIcon = commentBlock.GetChild(0) as UIBlock2D;
        TextBlock commentText = commentBlock.GetChild(1) as TextBlock;
        
        if(commentIcon != null){
            commentIcon.Color = Color.Lerp(commentIcon.Color, commentTargetColor, Time.deltaTime * colorLerpSpeed);
            if(commentText != null) commentText.Color = Color.Lerp(commentText.Color, commentTargetColor, Time.deltaTime * colorLerpSpeed);
        }
    }

    #region ENGAGEMENT LOGIC
    void RegisterEngagementIcons(){
        likeBlock.AddGestureHandler<Gesture.OnPress>(OnLikeClicked);
        likeBlock.AddGestureHandler<Gesture.OnHover>(e => {
            likeHovered = true;
            UpdateLikeTargetColor();
        });
        likeBlock.AddGestureHandler<Gesture.OnUnhover>(e => {
            likeHovered = false;
            UpdateLikeTargetColor();
        });

        commentBlock.AddGestureHandler<Gesture.OnPress>(OnCommentClicked);
        commentBlock.AddGestureHandler<Gesture.OnHover>(e => {
            commentHovered = true;
            UpdateCommentTargetColor();
        });
        commentBlock.AddGestureHandler<Gesture.OnUnhover>(e => {
            commentHovered = false;
            UpdateCommentTargetColor();
        });
    }

    void UpdateLikeTargetColor(){
        if(likeClicked) likeTargetColor = clickedColor;
        else if(likeHovered) likeTargetColor = clickedColor;
        else likeTargetColor = defaultColor;
    }

    void UpdateCommentTargetColor(){
        if(commentClicked) commentTargetColor = clickedColor;
        else if(commentHovered) commentTargetColor = clickedColor;
        else commentTargetColor = defaultColor;
    }

    public void RecordEngagement(int likes, int comments){
        baseLikes = likes;
        baseComments = comments;
    }
    
    void OnLikeClicked(Gesture.OnPress e){
        likeClicked = !likeClicked;
        
        if(likeClicked) baseLikes++;
        else baseLikes--;
        
        debateManager.likeCount.Text = debateManager.FormatNumber(baseLikes);
        UpdateLikeTargetColor();
        
        StartCoroutine(RotateIcon(likeBlock.GetChild(0) as UIBlock2D));
        StartCoroutine(PulseIcon(likeBlock));
    }
    
    void OnCommentClicked(Gesture.OnPress e){
        commentClicked = !commentClicked;
        
        if(commentClicked) baseComments++;
        else baseComments--;
        
        debateManager.commentCount.Text = debateManager.FormatNumber(baseComments);
        UpdateCommentTargetColor();
        
        StartCoroutine(RotateIcon(commentBlock.GetChild(0) as UIBlock2D));
        StartCoroutine(PulseIcon(commentBlock));
    }
    
    IEnumerator RotateIcon(UIBlock2D icon){
        if(icon == null) yield break;
        
        float timer = 0f;
        
        while(timer < rotationDuration){
            timer += Time.deltaTime;
            float t = timer / rotationDuration;
            float angle = Mathf.Lerp(0f, 360f, t);
            icon.transform.localRotation = Quaternion.Euler(0, 0, angle);
            yield return null;
        }
        
        icon.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
    
    IEnumerator PulseIcon(UIBlock2D block){
        float duration = 0.2f;
        float timer = 0f;
        Vector3 originalScale = block.transform.localScale;
        
        while(timer < duration){
            timer += Time.deltaTime;
            float t = timer / duration;
            float scale = Mathf.Lerp(1f, 1.2f, Mathf.Sin(t * Mathf.PI));
            block.transform.localScale = originalScale * scale;
            yield return null;
        }
        
        block.transform.localScale = originalScale;
    }
    #endregion

    #region HINT LOGIC
    public void RecordFallacy(string fallacy){
        headerFallacy = fallacy;
    }

    void RegisterHint(){
        hintBlock.AddGestureHandler<Gesture.OnPress>(e => { if(!hintRevealing) StartCoroutine(RevealHint()); });
        hintBlock.AddGestureHandler<Gesture.OnHover>(e => hintBlock.Shadow.Enabled = true);
        hintBlock.AddGestureHandler<Gesture.OnUnhover>(e => hintBlock.Shadow.Enabled = false);
    }

    IEnumerator RevealHint(){
        hintRevealing = true;
        hintBlock.GetChild(1).GetComponent<UIBlock2D>().SetImage(hintState[1]);

        TextBlock hintText = hintBlock.GetChild(0).GetComponent<TextBlock>();
        UIBlock2D animateBlock = hintText.GetChild(0).GetComponent<UIBlock2D>();
        UIBlock2D darkAnimateBlock = hintText.GetChild(1).GetComponent<UIBlock2D>();
        
        if(hintText == null || animateBlock == null || darkAnimateBlock == null){
            hintRevealing = false;
            yield break;
        }

        string originalText = hintText.Text;
        Color originalTextColor = hintText.Color;
        
        animateBlock.Size.X.Percent = 0f;
        darkAnimateBlock.Size.X.Percent = 0f;
        
        hintText.Color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0f);
        hintText.Text = headerFallacy;
        
        float coverProgress = 0f;
        while(coverProgress < 1f){
            coverProgress += Time.deltaTime * hintRevealSpeed;
            
            float mainProgress = Mathf.Clamp01(coverProgress * 1.2f);
            animateBlock.Size.X.Percent = Mathf.Lerp(0f, 1f, mainProgress);
            float darkProgress = Mathf.Clamp01(coverProgress * 0.8f);
            darkAnimateBlock.Size.X.Percent = Mathf.Lerp(0f, 1f, darkProgress);
            
            yield return null;
        }
        animateBlock.Size.X.Percent = 1f;
        darkAnimateBlock.Size.X.Percent = 1f;
        
        float revealProgress = 0f;
        while(revealProgress < 1f){
            revealProgress += Time.deltaTime * hintRevealSpeed;
            float alpha = Mathf.Lerp(0f, originalTextColor.a, revealProgress);
            hintText.Color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, alpha);
            yield return null;
        }
        hintText.Color = originalTextColor;
        
        float retractProgress = 0f;
        while(retractProgress < 1f){
            retractProgress += Time.deltaTime * hintRevealSpeed;
            float darkRetract = Mathf.Clamp01(retractProgress * 1.2f);
            darkAnimateBlock.Size.X.Percent = Mathf.Lerp(1f, 0f, darkRetract);
            float mainRetract = Mathf.Clamp01(retractProgress * 1f);
            animateBlock.Size.X.Percent = Mathf.Lerp(1f, 0f, mainRetract);
            
            yield return null;
        }
        
        animateBlock.Size.X.Percent = 0f;
        darkAnimateBlock.Size.X.Percent = 0f;
        
        hintText.Color = originalTextColor;
    }
    #endregion

    #region NOTIFICATION LOGIC
    public enum NotificationType{ End, Reply, Topic }
    public enum EndReason { PoorResponse, MaxTurn }

    public void PopNotification(NotificationType type, EndReason reason = EndReason.PoorResponse){
        bool isEnd = (type == NotificationType.End);
        bool isTopic = (type == NotificationType.Topic);

        if(isEnd) topPart.Color = redNotif;
        else topPart.Color = greenNotif;

        notificationPart[0].SetActive(isEnd);
        notificationPart[1].SetActive(!isEnd && !isTopic);
        notificationPart[2].SetActive(isTopic);

        if(isEnd){
            string msg = (reason == EndReason.PoorResponse) ? "Poor responses" : "Maximum turns reached";
            notificationPart[0].transform.GetChild(1).GetComponent<TextBlock>().Text = msg;
            RegisterEnd();
        }else if(isTopic){
            notificationPart[2].transform.GetChild(1).GetComponent<TextBlock>().Text = "Debate started";
            notificationPart[2].transform.GetChild(2).GetComponent<TextBlock>().Text = $"Topic: {topicWord}";
        }else{
            string msg = debateManager.currentOpponentName;
            notificationPart[1].transform.GetChild(1).GetComponent<TextBlock>().Text = $"{msg} replied to you";
        }

        if(activeNotificationCoroutine != null) StopCoroutine(activeNotificationCoroutine);
        activeNotificationCoroutine = StartCoroutine(BouncyNotification(type));
    } 

    IEnumerator BouncyNotification(NotificationType type){
        float slideDuration = .4f;
        float startY = -240f;
        float endY = 40f;
        float elapsed = 0f;

        while(elapsed < slideDuration){
            float t = elapsed / slideDuration;
            float smoothT = t * t * (3f - 2f * t);
            float currentY = Mathf.Lerp(startY, endY, smoothT);

            var pos = notificationBlock.Position;
            pos.Y = currentY;
            notificationBlock.Position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        var finalPos = notificationBlock.Position;
        finalPos.Y = endY;
        notificationBlock.Position = finalPos;

        float bounceDuration = .3f;
        float bounceHeight = 12f;
        float bounceElapsed = 0f;

        while(bounceElapsed < bounceDuration){
            float t = bounceElapsed / bounceDuration;
            float amplitude = bounceHeight * (1f - t);
            float offset = amplitude * Mathf.Sin(t * Mathf.PI * 2f);
            float currentY = endY + offset;

            var pos = notificationBlock.Position;
            pos.Y = currentY;
            notificationBlock.Position = pos;

            bounceElapsed += Time.deltaTime;
            yield return null;
        }

        finalPos.Y = endY;
        notificationBlock.Position = finalPos;

        if(type != NotificationType.End){
            yield return new WaitForSeconds(1.5f);
            StartCoroutine(GravityDrop());
        }
    }

    IEnumerator GravityDrop(){
        float duration = .4f;
        float startY = 40f;
        float endY = -240f;
        float elapsed = 0f;

        while(elapsed < duration){
            float t = elapsed / duration;
            float easeT = t * t;
            float currentY = Mathf.Lerp(startY, endY, easeT);

            var pos = notificationBlock.Position;
            pos.Y = currentY;
            notificationBlock.Position = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        var finalPos = notificationBlock.Position;
        finalPos.Y = endY;
        notificationBlock.Position = finalPos;
    }

    void RegisterEnd(){
        Color retryTargetColor = defaultColor;
        Color statTargetColor = defaultColor;

        //Retry
        retryButton.AddGestureHandler<Gesture.OnPress>(e => {
            if(debateManager != null){
                debateManager.ResetDebate();
                onlineForumVisual.EnableTopicTextField();
                StartCoroutine(GravityDrop());
            }
        });
        retryButton.AddGestureHandler<Gesture.OnHover>(e => {
            retryTargetColor = clickedNotifColor;
            retryButton.Gradient.Enabled = true;
        });
        retryButton.AddGestureHandler<Gesture.OnUnhover>(e => {
            retryTargetColor = defaultNotifColor;
            retryButton.Gradient.Enabled = false;
        });

        //Statistic
        statisticButton.AddGestureHandler<Gesture.OnPress>(e => {
            debateManager?.ShowPerformanceSummary();
        });
        statisticButton.AddGestureHandler<Gesture.OnHover>(e => {
            statTargetColor = clickedNotifColor;
            statisticButton.Gradient.Enabled = true;
        });
        statisticButton.AddGestureHandler<Gesture.OnUnhover>(e => {
            statTargetColor = defaultNotifColor;
            statisticButton.Gradient.Enabled = false;
        });

        StartCoroutine(AnimateEndButtons(retryButton, statisticButton, () => retryTargetColor, () => statTargetColor));
    }

    IEnumerator AnimateEndButtons(UIBlock2D retryBtn, UIBlock2D statBtn, System.Func<Color> retryColor, System.Func<Color> statColor){
        Color retryCurrent = defaultColor;
        Color statCurrent = defaultColor;

        while(true){
            if(retryBtn != null){
                var icon = retryBtn.GetChild(0) as UIBlock2D;
                var text = retryBtn.GetChild(1) as TextBlock;

                retryCurrent = Color.Lerp(retryCurrent, retryColor(), Time.deltaTime * colorLerpSpeed);
                if(icon != null) icon.Color = retryCurrent;
                if(text != null) text.Color = retryCurrent;
            }

            if(statBtn != null){
                var icon = statBtn.GetChild(0) as UIBlock2D;
                var text = statBtn.GetChild(1) as TextBlock;

                statCurrent = Color.Lerp(statCurrent, statColor(), Time.deltaTime * colorLerpSpeed);
                if(icon != null) icon.Color = statCurrent;
                if(text != null) text.Color = statCurrent;
            }

            yield return null;
        }
    }
    #endregion
}