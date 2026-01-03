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
    public Material onTopMat;
    public Material defaultMat;

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
    bool isOnObj, isTweening, isReturning, pendingSnap;
    bool _allowMovement = true;
    bool isRepositioning = false;
    private float hoverTimer, originalZ;
    private Color currentTargetColor = Color.white;

    private Coroutine currentMonologueRoutine;
    private IEnumerator currentTypewriterRoutine;
    private Coroutine colorRoutine;

    Vector3 _lockedMousePos;
    Vector3[] origCorners;
    Quaternion idleRotation;
    Transform[] corners;
    SpriteRenderer[] cursorRenderers;

    const float CURSOR_Z_OFFSET = .1f;

    Ray ray;
    RaycastHit2D hit;

    void Awake(){
        //Cursor.visible = false;
        //Cursor.lockState = CursorLockMode.None;
    }

    void Start(){
        corners = new Transform[] { TL, TR, BL, BR };
        origCorners = new Vector3[corners.Length];

        for(int i = 0; i < corners.Length; i++) origCorners[i] = corners[i].localPosition;

        idleRotation = transform.rotation;
        originalZ = transform.position.z;

        cursorRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        monologueContainer.alpha = 0f;

        SetCursorMaterial(onTopMat);
    }

    void Update(){
        if(!_allowMovement) return;
        RaycastLogic();
        HandleHoverTimer();
        HandleIdleOrSnap();
        HandleInput();
        UpdateCursorColor();
    }
    void LateUpdate(){
        if(!_allowMovement || isRepositioning) return;
        FollowMouse();
    }

    #region CORE UPDATE SPLITS
    void HandleHoverTimer(){
        if(!pendingSnap || lastTarget == null) return;

        hoverTimer += Time.deltaTime;
        if(hoverTimer >= requiredHoverTime && !isReturning){
            SnapToObject(lastTarget);
            pendingSnap = false;
        }else{
            ResetCorners(returnSpeed, true);
            if(center) center.gameObject.SetActive(true);
        }
    }

    void HandleIdleOrSnap(){
        if(isOnObj && lastTarget != null && !isTweening){
            UpdateObjBounds();
            return;
        }

        if(!isOnObj && !isTweening && !isReturning) RotateIdle();
    }

    void HandleInput(){
        if(desktopScreen.activeSelf || rezzitScreen.activeSelf) return;
        if(!Input.GetMouseButtonDown(0)) return;

        bool isOnRange = (lastTarget != null && Mathf.Abs(playerPos.position.x - lastTarget.position.x) <= 1.5f);

        HandleInteraction(isOnRange);
    }

    void UpdateCursorColor() => SetCursorColor(GetCursorColor());
    #endregion

    #region OTHERS
    void FollowMouse(){
        Vector3 mouse = (_allowMovement) ? Input.mousePosition : _lockedMousePos;

        float z = Mathf.Abs(thisCam.transform.position.z) + CURSOR_Z_OFFSET;
        mouse.z = z;

        Vector3 world = thisCam.ScreenToWorldPoint(mouse);
        world.z = originalZ + CURSOR_Z_OFFSET;

        transform.position = world;
    }

    void HandleInteraction(bool isOnRange){
        if(lastTarget == null) return;

        IMouseInteractable interactObj = lastTarget.GetComponent<IMouseInteractable>();
        if(interactObj == null) return;

        string[] outOfReach = { "Too far.", "Can't reach.", "Need to get closer" };
        string txt = isOnRange
            ? interactObj.GetMonologue()
            : outOfReach[Random.Range(0, outOfReach.Length)];

        if(currentMonologueRoutine != null) StopCoroutine(currentMonologueRoutine);

        currentMonologueRoutine = StartCoroutine(FadeMonologue(txt));

        audioSourceUI.pitch = Random.Range(0.95f, 1.05f);
        audioSourceUI.PlayOneShot(isOnRange ? clickSFX : rejectSFX);
    }

    IEnumerator FadeMonologue(string newText){
        if(currentTypewriterRoutine != null){
            StopCoroutine(currentTypewriterRoutine);
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

    public void CancelMonologue(){
        if(currentMonologueRoutine != null){
            StopCoroutine(currentMonologueRoutine);
            currentMonologueRoutine = null;
        }

        if(currentTypewriterRoutine != null){
            StopCoroutine(currentTypewriterRoutine);
            currentTypewriterRoutine = null;
        }

        if(colorRoutine != null){
            StopCoroutine(colorRoutine);
            colorRoutine = null;
        }

        monologueText.text = "";
        monologueContainer.alpha = 0f;
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
        monologueText.text = "";

        for(int i = 0; i < text.Length; i++){
            monologueText.text += text[i];

            if(i % 2 == 0){
                audioSourceSFX.pitch = Random.Range(0.75f, 1.05f);
                audioSourceSFX.volume = Random.Range(.25f, .45f);
                audioSourceSFX.PlayOneShot(typeShi);
            }

            yield return new WaitForSeconds(.04f);
        }
    }
    #endregion

    #region RAYCAST / SNAP
    void RaycastLogic(){
        ray = thisCam.ScreenPointToRay(Input.mousePosition);
        hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, interactableLayer);

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
        if(isReturning) return;

        isOnObj = false;
        pendingSnap = false;
        hoverTimer = 0f;
        lastTarget = null;

        isReturning = true;
        isTweening = true;

        KillCornerTweens();
        ResetCorners(returnSpeed, true);

        DOVirtual.DelayedCall(returnSpeed, () => {
            transform.rotation = idleRotation;
            isReturning = false;
            isTweening = false;
        });

        SetCursorMaterial(onTopMat);

        if(center) center.gameObject.SetActive(true);
    }
    #endregion

    #region SNAP LOGIC
    void SnapToObject(Transform target){
        if(isReturning) return;

        SetCursorMaterial(defaultMat);
        isOnObj = true;

        transform.rotation = target.rotation;
        if(center) center.gameObject.SetActive(false);

        StartSnapTween(target);
    }

    void StartSnapTween(Transform target){
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if(sr == null || sr.sprite == null) return;

        Bounds b = sr.sprite.bounds;

        Vector3[] worldCorners = {
            sr.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0))
        };

        isTweening = true;
        KillCornerTweens();

        for(int i = 0; i < corners.Length; i++){
            corners[i].DOMove(worldCorners[i], goSpeed)
                .SetUpdate(true)
                .OnComplete(() => isTweening = false);
        }
    }

    void PositionCornersToSprite(Transform target){
        if(target == null) return;

        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if(sr == null || sr.sprite == null) return;

        Bounds b = sr.sprite.bounds;
        Vector3[] worldCorners = {
            sr.transform.TransformPoint(new Vector3(b.min.x, b.max.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.max.x, b.max.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.min.x, b.min.y, 0)),
            sr.transform.TransformPoint(new Vector3(b.max.x, b.min.y, 0))
        };

        for(int i = 0; i < corners.Length; i++) corners[i].position = worldCorners[i];
    }
    #endregion

    #region RESET
    void ResetCorners(float speed, bool tween, Vector3[] positions = null){
        positions ??= origCorners;

        for(int i = 0; i < corners.Length; i++){
            if(tween) corners[i].DOLocalMove(positions[i], speed);
            else corners[i].localPosition = positions[i];
        }
    }
    #endregion

    #region UTILITY
    void RotateIdle(){transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime); idleRotation = transform.rotation;}
    void UpdateObjBounds() => PositionCornersToSprite(lastTarget);
    void KillCornerTweens() => System.Array.ForEach(corners, c => c.DOKill());
    void SetCursorMaterial(Material m){foreach(var sr in cursorRenderers) sr.material = m;}

    public void SetMovementEnabled(bool enabled){
        if(_allowMovement == enabled) return;
        _allowMovement = enabled;

        if(!enabled) _lockedMousePos = Input.mousePosition;
        else{
            Vector3 mouse = _lockedMousePos;
            float z = Mathf.Abs(thisCam.transform.position.z) + CURSOR_Z_OFFSET;
            mouse.z = z;

            Vector3 world = thisCam.ScreenToWorldPoint(mouse);
            world.z = originalZ + CURSOR_Z_OFFSET;

            StartCoroutine(SmoothReposition(world, 0.15f));
        }
    }
    IEnumerator SmoothReposition(Vector3 targetPosition, float duration){
        isRepositioning = true;
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while(elapsed < duration){
            elapsed += Time.deltaTime;
            float t = Mathf.Min(elapsed / duration, 1f);
            transform.position = Vector3.Lerp(startPos, targetPosition, t);
            yield return null;
        }

        transform.position = targetPosition;
        isRepositioning = false;
    }
    #endregion

    #region COLOR
    Color GetCursorColor(){
        if(!isOnObj || lastTarget == null) return Color.white;

        bool inRange = Mathf.Abs(playerPos.position.x - lastTarget.position.x) <= 1.5f;
        return inRange ? Color.cyan : Color.red;
    }

    public void SetCursorColor(Color color, float duration = .45f){
        if(currentTargetColor == color) return;
        currentTargetColor = color;

        if(colorRoutine != null) StopCoroutine(colorRoutine);

        colorRoutine = StartCoroutine(LerpCursorColor(color, duration));
    }

    IEnumerator LerpCursorColor(Color target, float duration){
        Color[] start = new Color[cursorRenderers.Length];
        for(int i = 0; i < start.Length; i++) start[i] = cursorRenderers[i].color;

        float t = 0f;
        while(t < duration){
            t += Time.deltaTime;
            float a = t / duration;

            for(int i = 0; i < cursorRenderers.Length; i++) cursorRenderers[i].color = Color.Lerp(start[i], target, a);

            yield return null;
        }

        foreach(var sr in cursorRenderers) sr.color = target;
        colorRoutine = null;
    }
    #endregion
}
