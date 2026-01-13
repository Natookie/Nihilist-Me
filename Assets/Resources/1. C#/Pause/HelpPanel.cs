using UnityEngine;
using Nova;
using System.Collections;

public class HelpPanel : MonoBehaviour
{
    [Header("UI CONFIG")]
    [SerializeField] private UIBlock2D block1;
    [SerializeField] private UIBlock2D block2;
    [SerializeField] private TextBlock lyricBlock;

    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float gradientSpeed = 2f;
    [SerializeField] private float lineRevealInterval = 1f;
    [SerializeField] private float gradientStartPosX;
    [SerializeField] private float gradientEndPosX;
    [SerializeField] private Color activeLineColor = Color.magenta;
    [SerializeField] private Color inactiveLineColor = Color.gray;
    
    string lyrics = 
    "弱い者ばっか見るな\n" +
    "欠点ばかりじゃねんだ\n" +
    "日に日にAttractive\n" +
    "負け犬じゃない we are\n\n" +
    
    "恐れない、土俵が違う、諦めない、step forward\n" +
    "揃えない、ある物でなる可能\n" +
    "覚えない、戦友が損するルール 悲観させたりはしない\n\n" +
    
    "お前はやりてえのか？本気なんか？\n" +
    "困難を超えれるのかが鍵だ\n" +
    "勢いだけを早まったって結果ないとダメだ\n" +
    "つらぬく未来は方向次第で叶うからやりな\n" +
    "張っときゃ行けるバリア\n" +
    "誰でも持っているdrama、偏見から逃げなくてもいい。\n" +
    "負けんな敵とみなすものに\n" +
    "期待に応じ、自分を信じ\n" +
    "羽ばたく人生に間違いなんてない\n\n" +
    
    "(I will be here)\n" +
    "望みを背負い\n" +
    "切り開く新しいステージへと\n" +
    "誰かのために ためらいはしない\n" +
    "平和な世界へ\n\n" +
    
    "弱い者ばっか見るな\n" +
    "欠点ばかりじゃねんだ\n" +
    "日に日にAttractive\n" +
    "負け犬じゃない we are\n\n" +
    
    "恐れない、土俵が違う、諦めないstep forward\n" +
    "揃えない、ある物でなる可能\n" +
    "覚えない、戦友が損するルール 悲観させたりはしない";

    private bool isBlock1Hovered = false;
    private bool isBlock2Hovered = false;
    private bool hasBlock1Animated = false;
    private bool hasBlock2Animated = false;
    
    private Coroutine block1GradientCoroutine;
    private Coroutine block2GradientCoroutine;
    private Coroutine lyricAnimationCoroutine;
    
    private string[] lyricLines;
    private int currentActiveLine = 0;
    
    void Start(){
        SetupGestureHandlers();
        SetupLyricText();
    }
    
    void OnEnable() => StartLyricAnimation();
    void OnDisable() => StopAllAnimations();
    
    void SetupGestureHandlers(){
        if(block1 != null){
            block1.AddGestureHandler<Gesture.OnHover>(evt => OnBlockHover(1));
            block1.AddGestureHandler<Gesture.OnUnhover>(evt => OnBlockUnhover(1));
            SetupGradient(block1);
        }
        
        if(block2 != null){
            block2.AddGestureHandler<Gesture.OnHover>(evt => OnBlockHover(2));
            block2.AddGestureHandler<Gesture.OnUnhover>(evt => OnBlockUnhover(2));
            SetupGradient(block2);
        }
    }
    
    void SetupGradient(UIBlock2D block){
        if(block == null) return;
        
        block.Gradient.Enabled = true;
        
        block.Gradient.Center.Value = new Vector2(gradientStartPosX, 0f);
        block.Gradient.Enabled = false;
    }
    
    void SetupLyricText(){
        if(lyricBlock == null) return;
        
        lyricLines = lyrics.Split('\n');
        UpdateLyricDisplay();
    }
    
    void StartLyricAnimation(){
        if(lyricAnimationCoroutine != null){
            StopCoroutine(lyricAnimationCoroutine);
        }
        lyricAnimationCoroutine = StartCoroutine(LyricAnimationCoroutine());
    }
    
    void OnBlockHover(int blockIndex){
        switch (blockIndex){
            case 1:
                if(!isBlock1Hovered && !hasBlock1Animated){
                    isBlock1Hovered = true;
                    
                    if(block1GradientCoroutine != null) StopCoroutine(block1GradientCoroutine);
                    block1GradientCoroutine = StartCoroutine(GradientAnimationCoroutine(1));
                }
                break;
                
            case 2:
                if(!isBlock2Hovered && !hasBlock2Animated){
                    isBlock2Hovered = true;
                    
                    if(block2GradientCoroutine != null) StopCoroutine(block2GradientCoroutine);
                    block2GradientCoroutine = StartCoroutine(GradientAnimationCoroutine(2));
                }
                break;
        }
    }
    
    void OnBlockUnhover(int blockIndex){
        switch (blockIndex){
            case 1:
                if(isBlock1Hovered){
                    isBlock1Hovered = false;
                    hasBlock1Animated = false;
                    
                    if(block1GradientCoroutine != null){
                        StopCoroutine(block1GradientCoroutine);
                        block1GradientCoroutine = null;
                    }
                    
                    if(block1 != null) block1.Gradient.Enabled = false;
                }
                break;
                
            case 2:
                if(isBlock2Hovered){
                    isBlock2Hovered = false;
                    hasBlock2Animated = false;
                    
                    if(block2GradientCoroutine != null){
                        StopCoroutine(block2GradientCoroutine);
                        block2GradientCoroutine = null;
                    }
                    
                    if(block2 != null){
                        block2.Gradient.Enabled = false;
                    }
                }
                break;
        }
    }
    
    IEnumerator GradientAnimationCoroutine(int blockIndex){
        float gradientX = gradientStartPosX;
        UIBlock2D block = (blockIndex == 1) ? block1 : block2;
        
        if(block == null) yield break;
        
        block.Gradient.Enabled = true;
        
        while(gradientX < gradientEndPosX){
            gradientX = Mathf.MoveTowards(gradientX, gradientEndPosX, gradientSpeed * Time.unscaledDeltaTime * 240f);
            block.Gradient.Center.Value = new Vector2(gradientX, 0f);
            yield return null;
        }
        
        block.Gradient.Center.Value = new Vector2(gradientEndPosX, 0f);
        
        if(blockIndex == 1) hasBlock1Animated = true;
        else hasBlock2Animated = true;
        
        if(blockIndex == 1) block1GradientCoroutine = null;
        else block2GradientCoroutine = null;
    }
    
    IEnumerator LyricAnimationCoroutine(){
        if(lyricLines == null || lyricLines.Length == 0) yield break;
        
        while(true){
            yield return new WaitForSecondsRealtime(lineRevealInterval);
            
            currentActiveLine = (currentActiveLine + 1) % lyricLines.Length;
            UpdateLyricDisplay();
        }
    }
    
    void UpdateLyricDisplay(){
        if(lyricBlock == null || lyricLines == null) return;
        
        string formattedText = "";
        
        for (int i = 0; i < lyricLines.Length; i++){
            if(string.IsNullOrEmpty(lyricLines[i])){
                formattedText += "\n";
                continue;
            }
            
            if(i == currentActiveLine) formattedText += $"<color=#{ColorUtility.ToHtmlStringRGB(activeLineColor)}>{lyricLines[i]}</color>\n";
            else formattedText += $"<color=#{ColorUtility.ToHtmlStringRGB(inactiveLineColor)}>{lyricLines[i]}</color>\n";
        }

        if(formattedText.EndsWith("\n")) formattedText = formattedText.Substring(0, formattedText.Length - 1);
        lyricBlock.Text = formattedText;
    }
    
    void StopAllAnimations(){
        if(block1GradientCoroutine != null){
            StopCoroutine(block1GradientCoroutine);
            block1GradientCoroutine = null;
        }
        
        if(block2GradientCoroutine != null){
            StopCoroutine(block2GradientCoroutine);
            block2GradientCoroutine = null;
        }
        
        if(lyricAnimationCoroutine != null){
            StopCoroutine(lyricAnimationCoroutine);
            lyricAnimationCoroutine = null;
        }
        
        if(block1 != null) block1.Gradient.Enabled = false;
        if(block2 != null) block2.Gradient.Enabled = false;

        isBlock1Hovered = false;
        isBlock2Hovered = false;
        hasBlock1Animated = false;
        hasBlock2Animated = false;
    }
    
    public void SetLineRevealSpeed(float interval){
        lineRevealInterval = Mathf.Max(0.1f, interval);
        
        if(lyricAnimationCoroutine != null){
            StopCoroutine(lyricAnimationCoroutine);
            lyricAnimationCoroutine = StartCoroutine(LyricAnimationCoroutine());
        }
    }
    
    public void TriggerGradientAnimation(int blockIndex){
        OnBlockUnhover(blockIndex);
        OnBlockHover(blockIndex);
    }
    
    void OnDestroy(){
        StopAllAnimations();
    }
}