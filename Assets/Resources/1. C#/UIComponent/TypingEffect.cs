using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class TypingEffect : MonoBehaviour
{
    // Object references
    [SerializeField] private TextMeshProUGUI textBox;
    // Animation variables
    [SerializeField] private string fullTitle = "Nihilist Me";
    
    private GameManager gm;
    private RectTransform textRect;
    private Camera uiCamera;

    public float typingSpeed = .05f;
    public float cursorBlinkSpeed = .5f;
    public int cursorRadius = 1;
    public float scrambleSpeed = .03f;
    public float scrambleDuration = .5f;

    private string displayedTitle = "";
    private bool cursorVisible = false;
    private Coroutine cursorRoutine; 

    private bool isEnabled = false;
    private bool isAnimating = false;
    private bool hasAnimated = false;
    private bool isScrambling = false;
    private Coroutine scrambleRoutine;

    void Start()
    {
        // Assertion check
        Assert.IsNotNull(textBox, "textBox is missing");
        Assert.IsNotNull(fullTitle, "fullTitle is empty");
        // Get initial objects
        gm = GameManager.Instance;
        textRect = textBox.GetComponent<RectTransform>();
        Canvas canvas = textBox.canvas;
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
    }

    void Update()
    {
        // Check if is enabled
        if(!isEnabled) return;
        // Check for any mouse hover
        if(hasAnimated && !isAnimating)
        {
            DetectHoverScramble();
        }
    }

    public void EnableEffect()
    {
        isEnabled = true;
        isAnimating = false;
        hasAnimated = false;
        isScrambling = false;
        StartCoroutine(TypeTitle());
    }

    public void DisableEffect()
    {
        isEnabled = false;
        StopAllCoroutines();
    }

    IEnumerator TypeTitle(){
        isAnimating = true;
        hasAnimated = true;
        displayedTitle = "";

        for(int i = 0; i < fullTitle.Length; i++){
            displayedTitle += fullTitle[i];
            UpdateTitleDisplay();
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isAnimating = false;

        if(cursorRoutine != null) StopCoroutine(cursorRoutine);
        cursorRoutine = StartCoroutine(CursorBlink());
    }

    void UpdateTitleDisplay() => textBox.text = displayedTitle + (cursorVisible ? "_" : "");

    IEnumerator CursorBlink(){
        cursorVisible = true;

        while(gm.isPaused){
            cursorVisible = !cursorVisible;
            UpdateTitleDisplay();
            yield return new WaitForSecondsRealtime(cursorBlinkSpeed);
        }

        cursorVisible = false;
        UpdateTitleDisplay();
    }


    void DetectHoverScramble(){
        if(textBox == null || isScrambling) return;

        Vector2 mousePos = Input.mousePosition;
        if(!RectTransformUtility.RectangleContainsScreenPoint(textRect, mousePos, uiCamera)) return;

        int charIndex = TMP_TextUtilities.FindIntersectingCharacter(textBox, mousePos, uiCamera, true);
        if(charIndex != -1 && charIndex < fullTitle.Length){
            if(scrambleRoutine != null) StopCoroutine(scrambleRoutine);
            scrambleRoutine = StartCoroutine(ScrambleTextAround(charIndex));
        }
    }

    IEnumerator ScrambleTextAround(int centerIndex){
        isScrambling = true;
        float elapsed = 0f;

        while(elapsed < scrambleDuration){
            StringBuilder sb = new StringBuilder(fullTitle);
            int start = Mathf.Max(0, centerIndex - cursorRadius);
            int end = Mathf.Min(fullTitle.Length - 1, centerIndex + cursorRadius);

            for(int i = start; i <= end; i++){
                char c = fullTitle[i];
                if(char.IsLetter(c)) sb[i] = (char)Random.Range(65, 123);
            }

            displayedTitle = sb.ToString();
            UpdateTitleDisplay();

            yield return new WaitForSecondsRealtime(scrambleSpeed);
            elapsed += scrambleSpeed;
        }

        displayedTitle = fullTitle;
        UpdateTitleDisplay();
        isScrambling = false;
    }
}
