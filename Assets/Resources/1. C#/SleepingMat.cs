using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;

public class SleepingMat : MonoBehaviour, IInteractable
{
    [Header("EYELID ANIMATION")]
    public RectTransform topLid;
    public RectTransform bottomLid;
    public float maxHeight = 1100f;
    public float closeDuration = 1f;
    public float openDuration = 1f;

    [Header("TEXTS")]
    public string[] actNames = {
        "Act 1: Resignation",
        "Act 2: Comeback",
        "Act 3: Determination",
        "Act 4: Hope",
        "Act 5: "
    };
    public string[] endingNames = {
        "Lost cause",
        "Black",
        "\"This is the end.\""
    };

    [Header("UI")]
    public GameObject sleepScreen;
    public TextMeshProUGUI recapText;

    public int dayCount = 1;
    private int winCount;
    private bool isSleeping;
    [HideInInspector] public bool ending3;

    private GameManager gm;

    private Vector2 topStart;
    private Vector2 bottomStart;
    bool isTransitioning;

    void Awake(){
        topStart = topLid.sizeDelta;
        bottomStart = bottomLid.sizeDelta;
    }

    void Start(){
        gm = GameManager.Instance;
        sleepScreen.SetActive(false);
        SetLidHeight(0f);
    }

    public void Interact(){
        if(isTransitioning) return;
        isTransitioning = true;

        if(!isSleeping) StartCoroutine(SleepRoutine());
        else StartCoroutine(WakeUpRoutine());
    }
    IEnumerator SleepRoutine(){
        yield return StartCoroutine(EyeLidClose());
        Sleep();
        isTransitioning = false;
    }
    IEnumerator WakeUpRoutine(){
        WakeUp();
        yield return StartCoroutine(EyeLidOpen());
        isTransitioning = false;
    }

    void Sleep(){
        isSleeping = true;
        sleepScreen.SetActive(true);
        recapText.text = (dayCount < 5) ? actNames[dayCount - 1] : "Act 5: " + endingNames[DecideEnding()];

        gm.isAnyUiActive = true;
    }
    void WakeUp(){
        isSleeping = false;
        sleepScreen.SetActive(false);
        dayCount++;

        gm.isAnyUiActive = false;
    }
    int DecideEnding(){
        if(ending3) return 2;
        return (winCount >= 2) ? 0 : 1;
    }

    IEnumerator EyeLidClose() => LerpLidHeight(0f, maxHeight, closeDuration, false);
    IEnumerator EyeLidOpen()  => LerpLidHeight(maxHeight, 0f, openDuration,  true);
    IEnumerator LerpLidHeight(float start, float end, float duration, bool isOpening){
        float t = 0f;
        float startFocus     = gm._dof.focusDistance.value;
        float startAperture  = gm._dof.aperture.value;
        float targetFocus    = (isOpening) ? gm.normalFocus    : gm.blurFocus;
        float targetAperture = (isOpening) ? gm.normalAperture : gm.blurAperture;
        float blurDuration   = (isOpening) ? duration * 2f : duration / 1.25f;

        while(t < duration){
            t += Time.deltaTime;
            float heightStep = Mathf.SmoothStep(0f, 1f, t / duration);
            float blurStep = Mathf.SmoothStep(0f, 1f, t / blurDuration);
            float h = Mathf.Lerp(start, end, heightStep);

            gm._dof.focusDistance.value = Mathf.Lerp(startFocus, targetFocus, blurStep);
            gm._dof.aperture.value      = Mathf.Lerp(startAperture, targetAperture, blurStep);
            SetLidHeight(h);

            yield return null;
        }
        SetLidHeight(end);
        gm._dof.focusDistance.value = targetFocus;
        gm._dof.aperture.value      = targetAperture;
        gm.disableBlur = !isOpening;
    }
    void SetLidHeight(float height){
        topLid.sizeDelta = new Vector2(topStart.x, height);
        bottomLid.sizeDelta = new Vector2(bottomStart.x, height);
    }

    public string GetPrompt() => "Go to Sleep";
}
