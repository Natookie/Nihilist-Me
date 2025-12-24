using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class CustomCursor : MonoBehaviour
{
    public Transform TL, TR, BL, BR, center;

    [Header("TWEAKS")]
    public float returnSpeed = .2f;
    public float goSpeed = .2f;
    public float rotateSpeed = 50f;
    public float requiredHoverTime = .3f;
    public LayerMask interactableLayer;

    [Header("UI")]
    public TextMeshProUGUI monologueText;
    public CanvasGroup monologueContainer;

    [Header("MATERIALS")]
    public Material onTopMat, defaultMat;

    [Header("SOUND EFFECT")]
    public AudioSource audioSourceUI;
    public AudioSource audioSourceSFX;
    public AudioClip clickSFX;
    public AudioClip rejectSFX;
    public AudioClip typeShi;

    [Header("REFERENCES")]
    public Camera thisCam;
    public Transform playerPos;
    [Space(10)]
    public GameObject desktopScreen;
    public GameObject rezzitScreen;

    private Transform lastTarget;
    bool isOnObj, isTweening, pendingSnap;
    private float hoverTimer, originalZ;
    private Color currentTargetColor = Color.white;
    private Coroutine fadeRoutine;
    private Coroutine colorRoutine;
    private Coroutine currentMonologueRoutine;
    private IEnumerator currentTypewriterRoutine;

    Vector3[] origCorners;
    Quaternion cursorOriginalRot;
    Transform[] corners;

    void Start(){
        corners = new Transform[] { TL, TR, BL, BR };
        origCorners = new Vector3[corners.Length];
        monologueContainer.alpha = 0f;  

        for(int i = 0; i < corners.Length; i++) origCorners[i] = corners[i].localPosition;

        cursorOriginalRot = transform.rotation;
        originalZ = transform.position.z;

        SetCursorMaterial(onTopMat);
    }

    void Update(){
        FollowMouse();
        RaycastLogic();

        if(pendingSnap && lastTarget != null){
            hoverTimer += Time.deltaTime;
            if(hoverTimer >= requiredHoverTime){
                SnapToObject(lastTarget);
                pendingSnap = false;
            }
        }

        if(!isOnObj) RotateIdle();
        else if(lastTarget != null && !isTweening && lastTarget != null) UpdateObjBounds();

        if(pendingSnap && hoverTimer <= requiredHoverTime){
            ResetCorners(returnSpeed, true);
            if(center) center.gameObject.SetActive(true);
        }

        bool isOnRange = lastTarget != null && isOnObj && Mathf.Abs(playerPos.position.x - lastTarget.position.x) <= 1.5f;
        if(Input.GetMouseButtonDown(0) && !desktopScreen.activeSelf && !rezzitScreen.activeSelf) HandleInteraction(isOnRange);

        Color cursorColor = GetCursorColor();
        SetCursorColor(cursorColor);
    }

    #region OTHERS
    void FollowMouse(){
        Ray ray = thisCam.ScreenPointToRay(Input.mousePosition);
        float targetZ = isOnObj && lastTarget != null ? lastTarget.position.z : originalZ;
        float dist = (targetZ - ray.origin.z) / ray.direction.z;
        Vector3 world = ray.origin + ray.direction * dist;

        world.z = targetZ;
        transform.position = world;
    }

    void HandleInteraction(bool isOnRange){
        if(lastTarget == null) return;
        IMouseInteractable interactObj = lastTarget.gameObject.GetComponent<IMouseInteractable>();
        if(interactObj == null) return;

        string[] outOfReach = {"Too far.", "Can't reach.", "Need to get closer"};
        string txt = (isOnRange) ? interactObj.GetMonologue() : outOfReach[Random.Range(0, outOfReach.Length)];
        
        if(currentMonologueRoutine != null){
            StopCoroutine(currentMonologueRoutine);
            currentMonologueRoutine = null;
        }

        currentMonologueRoutine = StartCoroutine(FadeMonologue(txt));
        //SFX
        audioSourceUI.pitch = Random.Range(0.95f, 1.05f);
        if(isOnRange) audioSourceUI.PlayOneShot(clickSFX);
        else audioSourceUI.PlayOneShot(rejectSFX);
    }

    IEnumerator FadeMonologue(string newText){
        if(currentTypewriterRoutine != null){
            StopCoroutine(currentTypewriterRoutine);
            currentTypewriterRoutine = null;
            monologueText.text = "";
        }
        
        yield return Fade(monologueContainer, 1f, .25f);
        
        monologueText.text = "";
        currentTypewriterRoutine = Typewriter(newText);
        yield return StartCoroutine(currentTypewriterRoutine);
        
        yield return new WaitForSeconds(2.5f);
        
        yield return Fade(monologueContainer, 0f, .2f);
        monologueText.text = "";
        
        currentMonologueRoutine = null;
        currentTypewriterRoutine = null;
    }

    IEnumerator Fade(CanvasGroup group, float target, float duration){
        float start = group.alpha;
        float t = 0f;

        while(t < duration){
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        group.alpha = target;
    }

    IEnumerator Typewriter(string text){
        float characterSpeed = .04f;
        float wordPause = 0f;
        float sentencePause = .15f;
        monologueText.text = "";
        
        for(int i = 0; i < text.Length; i++){
            monologueText.text += text[i];
            
            float delay = characterSpeed;
            
            if(text[i] == ',' || text[i] == ';') delay += sentencePause * 0.5f;
            else if(text[i] == '.' || text[i] == '!' || text[i] == '?') delay += sentencePause;
            else if(text[i] == ' ' && i + 1 < text.Length && char.IsLetter(text[i + 1])) delay += wordPause;
            
            if(i % 2 == 0){
                audioSourceSFX.pitch = Random.Range(0.75f, 1.05f);
                audioSourceSFX.volume = Random.Range(.25f, .45f);
                audioSourceSFX.PlayOneShot(typeShi);
            }
            yield return new WaitForSeconds(delay);
        }
    }

    public void CancelMonologue(){
        if(currentMonologueRoutine != null){
            StopCoroutine(currentMonologueRoutine);
            currentMonologueRoutine = null;
        }

        if(currentTypewriterRoutine != null){
            StopCoroutine(currentTypewriterRoutine);
            currentTypewriterRoutine = null;
        }

        if(fadeRoutine != null){
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        monologueText.text = "";
        monologueContainer.alpha = 0f;
    }
    #endregion

    #region GESTURE LOGIC 
    void RaycastLogic(){
        Ray ray = thisCam.ScreenPointToRay(Input.mousePosition);
        var hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, interactableLayer);

        if(hit.collider && hit.transform != lastTarget) EnterHover(hit.transform);
        else if(!hit.collider && (isOnObj || pendingSnap)) ExitObj();
    }

    void EnterHover(Transform target){
        pendingSnap = true;
        isOnObj = false;
        lastTarget = target;
        hoverTimer = 0f;

        SetCursorMaterial(defaultMat);
        KillCornerTweens();
    }

    void ExitObj(){
        if(isOnObj){
            ResetCornerRotation();
            transform.rotation = cursorOriginalRot;
        }

        isOnObj = false;
        pendingSnap = false;
        hoverTimer = 0f;
        lastTarget = null;

        SetCursorMaterial(onTopMat);
        ResetCorners(returnSpeed, true);

        if(center) center.gameObject.SetActive(true);
    }
    #endregion

    #region SNAP LOGIC
    void SnapToObject(Transform target){
        SetCursorMaterial(defaultMat);
        isOnObj = true;
        transform.rotation = target.rotation;
        if(center) center.gameObject.SetActive(false);

        ResetCornerRotation();
        StartSnapTween(target);
    }

    void StartSnapTween(Transform target){
        var sr = target.GetComponent<SpriteRenderer>();
        if(sr == null || sr.sprite == null) return;

        Bounds b = sr.sprite.bounds;
        Vector3[] worldCorners = new Vector3[]{
            sr.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0)), //TL
            sr.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0)), //TR
            sr.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0)), //BL
            sr.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0))  //BR
        };

        isTweening = true;
        KillCornerTweens();

        for(int i = 0; i < corners.Length; i++){
            if(i == 0) corners[i].DOMove(worldCorners[i], goSpeed).OnComplete(() => isTweening = false);
            else corners[i].DOMove(worldCorners[i], goSpeed);
        }
    }

    void PositionCornersToSprite(Transform target, bool tween){
        var sr = target.GetComponent<SpriteRenderer>();
        if(sr == null || sr.sprite == null) return;

        Bounds b = sr.sprite.bounds;
        Vector3[] worldCorners = new Vector3[]{
            sr.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0))
        };

        ResetCorners(tween ? returnSpeed : 0, tween, worldCorners);
    }
    #endregion  

    #region RESET LOGIC
    void ResetCorners(float speed, bool tween, Vector3[] positions = null){
        if(positions == null) positions = origCorners;

        for(int i = 0; i < corners.Length; i++){
            if(tween) corners[i].DOLocalMove(positions[i], speed);
            else corners[i].position = positions[i];
        }
    }

    void ResetCornerRotation(){
        KillCornerTweens();
        foreach(var c in corners) c.localRotation = Quaternion.identity;
        if(center) center.localRotation = Quaternion.identity;
    }
    #endregion

    #region UTILITY FUNCTION
    void RotateIdle() => transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    void UpdateObjBounds() => PositionCornersToSprite(lastTarget, false);
    void KillCornerTweens() => System.Array.ForEach(corners, c => c.DOKill());
    void SetCursorMaterial(Material m) => System.Array.ForEach(GetComponentsInChildren<SpriteRenderer>(), sr => sr.material = m);
    #endregion

    #region COLOR LOGIC
    Color GetCursorColor(){
        if(!isOnObj) return Color.white;

        bool isOnRange = lastTarget != null && Mathf.Abs(playerPos.position.x - lastTarget.position.x) <= 1.5f;
        return isOnRange ? Color.cyan : Color.red;
    }

    public void SetCursorColor(Color color, float duration = .45f){
        if(currentTargetColor == color) return;
        currentTargetColor = color;

        if(colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(LerpCursorColor(color, duration));
    }

    IEnumerator LerpCursorColor(Color target, float duration){
        List<SpriteRenderer> renderers = new List<SpriteRenderer>();

        void Collect(Transform t){
            if(t == null) return;
            renderers.AddRange(t.GetComponentsInChildren<SpriteRenderer>(true));
        }

        Collect(TL);
        Collect(TR);
        Collect(BL);
        Collect(BR);
        Collect(center);

        List<Color> startColors = new List<Color>();
        foreach(var sr in renderers) startColors.Add(sr.color);

        float tLerp = 0f;

        while(tLerp < duration){
            tLerp += Time.deltaTime;
            float a = Mathf.Clamp01(tLerp / duration);

            for(int i = 0; i < renderers.Count; i++) renderers[i].color = Color.Lerp(startColors[i], target, a);
            yield return null;
        }

        foreach(var sr in renderers) sr.color = target;
        colorRoutine = null;
    }
    #endregion
}
