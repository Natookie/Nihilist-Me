using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class DesktopIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("APP DATA")]
    public string appName;
    public Sprite appIcon;
    public GameObject appPrefab;

    [Header("TILT")]
    private float maxTiltAngle = 25f;
    private float tiltSmooth = 10f;
    private float speedToTilt = .1f;

    [Header("SWAP")]
    private float swapMoveDuration = 0.25f;

    private float lastClickTime;
    private float doubleClickDelay = 0.3f;
    private Vector2 lastMousePos;
    private float targetZ;

    private CanvasGroup group;
    private RectTransform canvasRect;
    private DesktopGrid desktopGrid;

    private Slot originalSlot;
    public Slot CurrentSlot { get; set; }

    GameManager gm;

    void Awake(){
        group       = GetComponent<CanvasGroup>();
        canvasRect  = GetComponentInParent<Canvas>().transform as RectTransform;
        desktopGrid = GetComponentInParent<DesktopGrid>();
    }

    void Start(){
        gm = GameManager.Instance;
    }

    #region CLICK LOGIC
    public void OnPointerClick(PointerEventData e){
        if(Time.time - lastClickTime < doubleClickDelay) OpenApplication();
        else Highlight();

        lastClickTime = Time.time;
    }

    void Highlight(){
        transform.localScale = Vector3.one * 1.05f;
        StopCoroutine(nameof(ResetScale));
        StartCoroutine(ResetScale());
    }

    IEnumerator ResetScale(){
        Vector3 target = Vector3.one;
        while((transform.localScale - target).sqrMagnitude > 0.001f){
            transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * 10f);
            yield return null;
        }
        transform.localScale = target;
    }

    void OpenApplication(){
        if(appPrefab == null){
            Debug.LogWarning($"No prefab assigned for {appName}!");
            return;
        }

        GameObject appInstance = Instantiate(appPrefab, gm.desktopCanvas);
        gm.dm.AddTask(this, appInstance);
    }
    #endregion

    #region DRAG LOGIC
    public void OnBeginDrag(PointerEventData e){
        originalSlot = CurrentSlot;

        transform.SetParent(canvasRect, true);

        group.blocksRaycasts = false;
        group.alpha = .35f;

        lastMousePos = e.position;
        targetZ = 0f;
    }

    public void OnDrag(PointerEventData e){
        //Move
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, e.pressEventCamera, out var pos);
        (transform as RectTransform).anchoredPosition = pos;

        //Tilt
        float deltaX = e.position.x - lastMousePos.x;
        float speed  = deltaX / Mathf.Max(Time.unscaledDeltaTime, 1e-6f);

        if(Mathf.Abs(speed) > 5f){
            float desired = Mathf.Clamp(speed * speedToTilt, -maxTiltAngle, maxTiltAngle);
            targetZ = desired;
        }else targetZ = Mathf.Lerp(targetZ, 0f, Time.unscaledDeltaTime * 3f);

        lastMousePos = e.position;

        //Apply tilt
        RectTransform rect = transform as RectTransform;
        Vector3 current = rect.localEulerAngles;
        current.z = Mathf.LerpAngle(current.z, -targetZ, tiltSmooth * Time.unscaledDeltaTime);
        rect.localEulerAngles = current;
    }

    public void OnEndDrag(PointerEventData e){
        group.blocksRaycasts = true;

        StopCoroutine("ResetRotation");
        StartCoroutine(ResetRotation());

        Slot targetSlot = null;
        if(e.pointerEnter) targetSlot = e.pointerEnter.GetComponentInParent<Slot>();
        if(targetSlot == null) targetSlot = FindClosestSlot();

        if(targetSlot == null){
            if(originalSlot != null) originalSlot.SetIcon(this);
            else{ 
                CurrentSlot = null; 
                transform.SetParent(canvasRect, true);
                group.alpha = 1f;
            }
            return;
        }

        if(targetSlot.IsEmpty){
            StartCoroutine(MoveToSlotAndFinalize(this, targetSlot));
            return;
        }

        DesktopIcon other = targetSlot.currentIcon;

        Vector3 thisEndWorld = GetSlotWorldPosition(targetSlot);
        Vector3 otherEndWorld = (originalSlot != null) ? GetSlotWorldPosition(originalSlot) : GetFreeWorldPositionNear(targetSlot);

        other.transform.SetParent(canvasRect, true);
        this.transform.SetParent(canvasRect, true);

        StartCoroutine(MoveWorldAndFinalize(other, otherEndWorld, originalSlot));
        StartCoroutine(MoveWorldAndFinalize(this, thisEndWorld, targetSlot));
    }
    #endregion

    #region PLACING TO GRID
    IEnumerator MoveWorldAndFinalize(DesktopIcon icon, Vector3 worldTarget, Slot finalizeSlot){
        RectTransform rect = icon.transform as RectTransform;

        Vector3 start = rect.position;
        float t = 0f;
        if(swapMoveDuration <= 0f) rect.position = worldTarget;
        else{
            while(t < 1f){
                t += Time.unscaledDeltaTime / swapMoveDuration;
                rect.position = Vector3.Lerp(start, worldTarget, Mathf.SmoothStep(0f,1f,t));
                yield return null;
            }
            rect.position = worldTarget;
        }

        if(finalizeSlot != null) finalizeSlot.SetIcon(icon);
        else{
            icon.CurrentSlot = null;
            icon.transform.SetParent(canvasRect, true);
        }
        icon.group.alpha = 1f;
    }

    private IEnumerator MoveToSlotAndFinalize(DesktopIcon icon, Slot targetSlot){
        Vector3 targetWorld = GetSlotWorldPosition(targetSlot);
        icon.transform.SetParent(canvasRect, true);
        yield return MoveWorldTo(icon, targetWorld, swapMoveDuration);

        targetSlot.SetIcon(icon);
    }

    IEnumerator MoveWorldTo(DesktopIcon icon, Vector3 worldTarget, float duration){
        RectTransform rect = icon.transform as RectTransform;
        Vector3 start = rect.position;
        float t = 0f;
        if(duration <= 0f){
            rect.position = worldTarget;
            yield break;
        }
        while(t < 1f){
            t += Time.unscaledDeltaTime / duration;
            rect.position = Vector3.Lerp(start, worldTarget, Mathf.SmoothStep(0f,1f,t));
            yield return null;
        }
        rect.position = worldTarget;
    }

    Vector3 GetSlotWorldPosition(Slot s){
        RectTransform slotRect = s.transform as RectTransform;
        return slotRect.position;
    }

    Vector3 GetFreeWorldPositionNear(Slot occupiedSlot){
        RectTransform slotRect = occupiedSlot.transform as RectTransform;
        return slotRect.position + new Vector3(40f, -40f, 0f);
    }

    IEnumerator ResetRotation(){
        RectTransform rect = transform as RectTransform;
        while(Mathf.Abs(rect.localEulerAngles.z) > 0.1f){
            float newZ = Mathf.LerpAngle(rect.localEulerAngles.z, 0f, tiltSmooth * Time.unscaledDeltaTime);
            rect.localEulerAngles = new Vector3(0f, 0f, newZ);
            yield return null;
        }
        rect.localEulerAngles = Vector3.zero;
    }

    Slot FindClosestSlot(){
        float best = float.MaxValue;
        Slot closest = null;
        Vector3 pos = transform.position;

        foreach(Slot s in desktopGrid.Slots){
            float d = (s.transform.position - pos).sqrMagnitude;
            if(d < best){
                best = d;
                closest = s;
            }
        }
        return closest;
    }
    #endregion
}
