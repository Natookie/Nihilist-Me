using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("GAME TITLE")]
    public TextMeshProUGUI gameTitle;
    public float typingSpeed = .05f;
    public float cursorBlinkSpeed = .5f;
    public int cursorRadius = 1;
    public float scrambleSpeed = .03f;
    public float scrambleDuration = .5f;

    [Header("MENU CONFIG")]
    public GameObject rootNode;

    [SerializeField] private List<PauseOptionSelection> pauseOptionSelections;
    // public GameObject[] selectableOptions;
    // public GameObject[] movableObjects;
    // public GameObject[] sideObjects;

    // private Vector2[] sideOriginalPositions;
    // private RectTransform[] sideRects;
    // private Coroutine[] sideRoutines;
    // private CanvasGroup[] sideCanvasGroups;

    [Header("LIST ANIMATION SETTINGS")]
    public float moveDurationPerItem = .4f;
    public float staggerDelay = .1f;
    [Range(0f, 1f)] public float bounceIntensity = .1f;
    public float moveDistance = 100f;
    public float scaleJuice = 1.1f;
    [Space(5)]
    public float selectionRotateAngle = 45f;
    public Color selectedColor = Color.cyan;
    public Color deselectedColor = Color.white;

    // private int currentOptionIndex = 0;
    private GameManager gm;
    private RectTransform textRect;
    private Camera uiCamera;

    private string fullTitle = "Nihilist Me";
    private string displayedTitle = "";
    private bool cursorVisible = false;
    private Coroutine cursorRoutine; 

    private bool isAnimating;
    private bool hasAnimated;
    private bool isScrambling;
    private Coroutine scrambleRoutine;



    private PauseOptionSelection currentPauseOptionSelection;


    void Start(){
        gm = GameManager.Instance;
        textRect = gameTitle.GetComponent<RectTransform>();

        Canvas canvas = gameTitle.canvas;
        uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // if(sideObjects != null && sideObjects.Length > 0){
        //     int len = sideObjects.Length;
        //     sideRects = new RectTransform[len];
        //     sideCanvasGroups = new CanvasGroup[len];
        //     sideOriginalPositions = new Vector2[len];
        //     sideRoutines = new Coroutine[len];

        //     for(int i = 0; i < len; i++){
        //         if(sideObjects[i] == null) continue;

        //         sideRects[i] = sideObjects[i].GetComponent<RectTransform>();
        //         sideCanvasGroups[i] = sideObjects[i].GetComponent<CanvasGroup>() ?? sideObjects[i].AddComponent<CanvasGroup>();
        //         sideOriginalPositions[i] = sideRects[i].anchoredPosition;

        //         sideCanvasGroups[i].alpha = 0f;
        //         sideObjects[i].SetActive(false);
        //     }
        // }

        // Assertion check
        Assert.IsTrue(pauseOptionSelections.Count > 0, "pauseOptionSelections is empty");
        // Connect events
        foreach(PauseOptionSelection pauseOptionSelection in pauseOptionSelections)
        {
            pauseOptionSelection.OnOptionPressed += PauseOptionSelection_OnOptionPressed;
        }
        // Initialize pause menu
        rootNode.SetActive(false);
    }

    void Update(){
        HandleTitle();
        // HandleList();
        // HandleMouseSelection();
    }

    #region MAIN FUNCTIONS
    public void OpenPauseMenu()
    {
        rootNode.SetActive(true);

        StopAllCoroutines();
        ResetPauseMenu();

        StartCoroutine(TypeTitle());
        StartCoroutine(ShowAllOptionSelection());
    }

    public void ClosePauseMenu()
    {
        rootNode.SetActive(false);

        // In case you want to add any extra animation when closing pause menu
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("Main Menu Scene");
    }
    #endregion

    #region TITLE ANIMATION
    void HandleTitle(){
        if(!gm.isPaused && hasAnimated){
            hasAnimated = false;
            if(cursorRoutine != null) StopCoroutine(cursorRoutine);
            if(scrambleRoutine != null) StopCoroutine(scrambleRoutine);
            StopCoroutine(TypeTitle());

            gameTitle.text = "";
        }

        if(gm.isPaused && !isAnimating) DetectHoverScramble();
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

    void UpdateTitleDisplay() => gameTitle.text = displayedTitle + (cursorVisible ? "_" : "");

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
        if(gameTitle == null || isScrambling) return;

        Vector2 mousePos = Input.mousePosition;
        if(!RectTransformUtility.RectangleContainsScreenPoint(textRect, mousePos, uiCamera)) return;

        int charIndex = TMP_TextUtilities.FindIntersectingCharacter(gameTitle, mousePos, uiCamera, true);
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
    #endregion

    // #region LIST HANDLING
    // void HandleList(){
    //     if(Input.GetKeyDown(KeyCode.S)){
    //         currentOptionIndex = (currentOptionIndex + 1) % selectableOptions.Length;
    //         UpdateSelection();
    //     }

    //     if(Input.GetKeyDown(KeyCode.W)){
    //         currentOptionIndex = (currentOptionIndex - 1 + selectableOptions.Length) % selectableOptions.Length;
    //         UpdateSelection();
    //     }
    // }

    // void HandleMouseSelection(){
    //     if(Input.GetMouseButtonDown(0)){
    //         Vector2 mousePos = Input.mousePosition;
    //         for(int i = 0; i < selectableOptions.Length; i++){
    //             if (selectableOptions[i] == null) continue;

    //             RectTransform rect = selectableOptions[i].GetComponent<RectTransform>();
    //             if(RectTransformUtility.RectangleContainsScreenPoint(rect, mousePos, uiCamera)){
    //                 currentOptionIndex = i;
    //                 UpdateSelection();
    //                 break;
    //             }
    //         }
    //     }
    // }

    // void UpdateSelection(){
    //     for(int i = 0; i < selectableOptions.Length; i++){
    //         if(selectableOptions[i] == null) continue;

    //         Transform square = selectableOptions[i].transform.GetChild(0);
    //         Image squareImg = square.GetComponent<Image>();

    //         bool isSelected = (i == currentOptionIndex);
    //         float targetAngle = isSelected ? -selectionRotateAngle : selectionRotateAngle;
    //         Color targetColor = isSelected ? selectedColor : deselectedColor;

    //         StartCoroutine(AnimateSquare(square, targetAngle, targetColor));
    //         StartCoroutine(AnimateScale(selectableOptions[i].transform, isSelected ? scaleJuice : 1f));

    //         if(sideObjects != null && i < sideObjects.Length && sideObjects[i] != null){
    //             if(sideRoutines[i] != null) StopCoroutine(sideRoutines[i]);
    //             sideRoutines[i] = StartCoroutine(AnimateSideObject(i, isSelected));
    //         }
    //     }
    // }

    // IEnumerator AnimateSquare(Transform square, float targetAngle, Color targetColor){
    //     Image img = square.GetComponent<Image>();
    //     float duration = .25f;
    //     float elapsed = 0f;

    //     Quaternion startRot = square.localRotation;
    //     Quaternion endRot = Quaternion.Euler(0f, 0f, targetAngle);
    //     Color startColor = img.color;

    //     while(elapsed < duration){
    //         elapsed += Time.unscaledDeltaTime;
    //         float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
    //         square.localRotation = Quaternion.Slerp(startRot, endRot, t);
    //         img.color = Color.Lerp(startColor, targetColor, t);
    //         yield return null;
    //     }

    //     square.localRotation = endRot;
    //     img.color = targetColor;
    // }

    // IEnumerator AnimateScale(Transform target, float targetScale){
    //     float duration = .25f;
    //     float elapsed = 0f;
    //     Vector3 startScale = target.localScale;
    //     Vector3 endScale = Vector3.one * targetScale;

    //     while(elapsed < duration){
    //         elapsed += Time.unscaledDeltaTime;
    //         float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
    //         target.localScale = Vector3.Lerp(startScale, endScale, t);
    //         yield return null;
    //     }

    //     target.localScale = endScale;
    // }

    // IEnumerator AnimateSideObject(int index, bool show){
    //     if(sideObjects[index] == null) yield break;

    //     RectTransform rt = sideRects[index];
    //     CanvasGroup cg = sideCanvasGroups[index];
    //     float duration = show ? 0.35f : 0.25f;
    //     float elapsed = 0f;

    //     Vector2 startPos = rt.anchoredPosition;
    //     Vector2 endPos = sideOriginalPositions[index];

    //     if(show){
    //         sideObjects[index].SetActive(true);
    //         rt.anchoredPosition = endPos + new Vector2(120f, 0f);
    //     }

    //     Vector2 from = show ? (rt.anchoredPosition) : endPos;
    //     Vector2 to   = show ? endPos : (endPos + new Vector2(120f, 0f));

    //     float startAlpha = cg.alpha;
    //     float targetAlpha = show ? 1f : 0f;

    //     while(elapsed < duration){
    //         elapsed += Time.unscaledDeltaTime;
    //         float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

    //         rt.anchoredPosition = Vector2.Lerp(from, to, t);
    //         cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
    //         yield return null;
    //     }

    //     rt.anchoredPosition = to;
    //     cg.alpha = targetAlpha;

    //     if(!show) sideObjects[index].SetActive(false);
    // }

    // void ResetSideObjects(){
    //     if(sideObjects == null || sideOriginalPositions == null) return;

    //     for(int i = 0; i < sideObjects.Length; i++){
    //         if(sideObjects[i] == null) continue;

    //         RectTransform rt = sideRects[i];
    //         rt.anchoredPosition = sideOriginalPositions[i];
    //         CanvasGroup cg = sideObjects[i].GetComponent<CanvasGroup>() ?? sideObjects[i].AddComponent<CanvasGroup>();
    //         cg.alpha = 0f;
    //         sideObjects[i].SetActive(false);
    //     }
    // }
    // void ResetMovableObjects(){
    //     if(movableObjects == null) return;

    //     for(int i = 0; i < movableObjects.Length; i++){
    //         if(movableObjects[i] == null) continue;

    //         RectTransform rt = movableObjects[i].GetComponent<RectTransform>();
    //         CanvasGroup cg = movableObjects[i].GetComponent<CanvasGroup>() ?? movableObjects[i].AddComponent<CanvasGroup>();

    //         cg.alpha = 1f;
    //         rt.localScale = Vector3.one;
    //     }
    // }

    // IEnumerator MoveSelectionSequential(){
    //     foreach(GameObject obj in movableObjects){
    //         if(obj == null) continue;

    //         CanvasGroup cg = obj.GetComponent<CanvasGroup>() ?? obj.AddComponent<CanvasGroup>();
    //         cg.alpha = 0f;
    //     }

    //     for(int i = 0; i < movableObjects.Length; i++){
    //         if(movableObjects[i] == null) continue;

    //         GameObject obj = movableObjects[i];
    //         RectTransform rt = obj.GetComponent<RectTransform>();
    //         CanvasGroup cg = obj.GetComponent<CanvasGroup>();

    //         Vector3 startPos = rt.anchoredPosition - new Vector2(0f, moveDistance);
    //         Vector3 endPos = rt.anchoredPosition;
    //         rt.anchoredPosition = startPos;
    //         cg.alpha = 1f;

    //         float elapsed = 0f;
    //         while(elapsed < moveDurationPerItem){
    //             elapsed += Time.unscaledDeltaTime;
    //             float t = elapsed / moveDurationPerItem;

    //             float springT = Mathf.Sin(t * Mathf.PI * .5f) * (1f + bounceIntensity * Mathf.Sin(t * Mathf.PI * 2f));

    //             rt.anchoredPosition = Vector3.LerpUnclamped(startPos, endPos, springT);
    //             rt.localScale = Vector3.Lerp(Vector3.one * .95f, Vector3.one, springT);
    //             yield return null;
    //         }

    //         rt.anchoredPosition = endPos;
    //         yield return new WaitForSecondsRealtime(staggerDelay);
    //     }

    //     // UpdateSelection();
    // }
    // #endregion

    private void ResetPauseMenu()
    {
        // Reset game title
        gameTitle.text = "";
        // Reset all pause option selection and pause side panel
        foreach(PauseOptionSelection pauseOptionSelection in pauseOptionSelections)
        {
            pauseOptionSelection.ResetOption();
            pauseOptionSelection.targetSidePanel.ResetSidePanel();
        }
        currentPauseOptionSelection = null;
    }

    private IEnumerator ShowAllOptionSelection(){
        // Make all pause option selection invisible
        foreach(PauseOptionSelection pauseOptionSelection in pauseOptionSelections){
            CanvasGroup cg = pauseOptionSelection.GetComponent<CanvasGroup>() ?? pauseOptionSelection.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }
        // Animate all pause option selection in sequence
        foreach(PauseOptionSelection pauseOptionSelection in pauseOptionSelections){
            // Set all references
            GameObject obj = pauseOptionSelection.gameObject;
            RectTransform rt = obj.GetComponent<RectTransform>();
            CanvasGroup cg = obj.GetComponent<CanvasGroup>();
            // Set all variables
            Vector3 startPos = rt.anchoredPosition - new Vector2(0f, moveDistance);
            Vector3 endPos = rt.anchoredPosition;
            rt.anchoredPosition = startPos;
            cg.alpha = 1f;
            // Animate current pause option selection
            float elapsed = 0f;
            while(elapsed < moveDurationPerItem){
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / moveDurationPerItem;

                float springT = Mathf.Sin(t * Mathf.PI * .5f) * (1f + bounceIntensity * Mathf.Sin(t * Mathf.PI * 2f));

                rt.anchoredPosition = Vector3.LerpUnclamped(startPos, endPos, springT);
                rt.localScale = Vector3.Lerp(Vector3.one * .95f, Vector3.one, springT);
                yield return null;
            }
            // Set final variables and return
            rt.anchoredPosition = endPos;
            yield return new WaitForSecondsRealtime(staggerDelay);
        }
        // Set the first pause option selection as default, if the player have not click anything
        if(currentPauseOptionSelection == null)
        {    
            PauseOptionSelection_OnOptionPressed(this, new PauseOptionSelection.OnOptionPressedEventArgs
            {
                pauseOptionSelection = pauseOptionSelections[0]
            });
        }
    }

    private void PauseOptionSelection_OnOptionPressed(object sender, PauseOptionSelection.OnOptionPressedEventArgs e)
    {
        // Check if the pause option selection is the same
        if(currentPauseOptionSelection == e.pauseOptionSelection) return;
        // Close pause option selection and pause side panel
        if(currentPauseOptionSelection != null)
        {
            currentPauseOptionSelection.DeselectOption();
            currentPauseOptionSelection.targetSidePanel.CloseSidePanel();
        }
        // Switch pause option selection
        currentPauseOptionSelection = e.pauseOptionSelection;
        // Open pause option selection and pause side panel
        currentPauseOptionSelection.SelectOption();
        currentPauseOptionSelection.targetSidePanel.OpenSidePanel();
    }
}
