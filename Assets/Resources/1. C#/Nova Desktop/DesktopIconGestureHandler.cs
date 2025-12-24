using Nova;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DesktopIconGestureHandler : MonoBehaviour
{
    private DesktopManager dm;

    private RectTransform floatingGroup;
    private Image floatingIcon;
    private TextMeshProUGUI floatingLabel;

    private bool isDragging;
    private bool isRestoring;
    private bool isAnimating;

    private Transform pressedIconOriginalParent;
    private Vector3 pressedIconOriginalScreenPos;

    private Vector2 lastMousePos;
    private Vector2 smoothDelta;
    private Vector2 velocity;

    private float nextClickTime;
    private const float clickCooldown = .15f;

    private DesktopIconVisual currentVisual;
    private bool hoverActive;
    private float hoverLerpT;

    private const float hoverSpeed = 10f;
    private const float iconHoverOffset = -4f;
    private const float labelHoverOffset = 4f;

    const float dragThreshold = 6f;
    bool dragStarted;

    [Header("NOVA DATA")]
    public UIBlock Root;
    public DesktopIconSetting IconSetting = new("App Name", null, null);
    public ItemView IconView;

    void Start(){
        dm = DesktopManager.Instance;
        if(dm == null){
            Debug.LogError("DesktopManager not found in scene.");
            enabled = false;
            return;
        }

        Root ??= GetComponentInParent<UIBlock>();
        IconView ??= GetComponent<ItemView>();

        Root.AddGestureHandler<Gesture.OnHover>(OnIconHover);
        Root.AddGestureHandler<Gesture.OnUnhover>(OnIconUnhover);
        Root.AddGestureHandler<Gesture.OnPress>(OnIconPress);
        Root.AddGestureHandler<Gesture.OnDrag>(OnIconDrag);
        Root.AddGestureHandler<Gesture.OnRelease>(OnIconRelease);

        BindIcon(IconSetting, IconView.Visuals as DesktopIconVisual);
    }

    void Update(){
        if(currentVisual == null || currentVisual.iconImage == null || currentVisual.nameText == null) return;

        float target = hoverActive ? 1f : 0f;
        hoverLerpT = Mathf.MoveTowards(hoverLerpT, target, Time.unscaledDeltaTime * hoverSpeed);

        float iconOffset = Mathf.Lerp(0f, iconHoverOffset, hoverLerpT);
        float labelOffset = Mathf.Lerp(0f, labelHoverOffset, hoverLerpT);
        float gradientAlpha = Mathf.Lerp(0f, 1f, hoverLerpT);

        currentVisual.iconImage.Position.Value = new Vector3(0f, iconOffset, 0f);
        currentVisual.nameText.Position.Value = new Vector3(0f, labelOffset, 0f);
        currentVisual.iconImage.Gradient.Enabled = gradientAlpha > 0.05f;
    }

    void BindIcon(DesktopIconSetting setting, DesktopIconVisual visual){
        if(visual == null) return;
        visual.nameText.Text = setting.appName;
        visual.iconImage.SetImage(setting.appIcon);
    }

    #region GESTURES
    void OnIconPress(Gesture.OnPress evt){
        if(isRestoring || isAnimating) return;
        if(Time.unscaledTime < nextClickTime) return;

        nextClickTime = Time.unscaledTime + clickCooldown;

        isDragging = true;
        dragStarted = false;
        smoothDelta = Vector2.zero;
        velocity = Vector2.zero;

        var target = evt.Target as UIBlock2D;
        if(target == null) return;

        pressedIconOriginalParent = target.transform;

        Transform pressedChild = pressedIconOriginalParent.childCount > 0
            ? pressedIconOriginalParent.GetChild(0)
            : pressedIconOriginalParent;

        pressedIconOriginalScreenPos =
            Camera.main.WorldToScreenPoint(pressedChild.position);

        lastMousePos = Input.mousePosition;
    }

    void OnIconDrag(Gesture.OnDrag evt){
        if(!isDragging) return;

        Vector2 currentMousePos = Input.mousePosition;
        Vector2 delta = currentMousePos - lastMousePos;

        if(!dragStarted && delta.magnitude < dragThreshold) return;

        if(!dragStarted){
            dragStarted = true;

            SetChildrenActive(pressedIconOriginalParent, false);
            CreateFloatingGroup(IconSetting.appIcon, IconSetting.appName);
        }

        lastMousePos = currentMousePos;

        smoothDelta = Vector2.SmoothDamp(
            smoothDelta,
            delta,
            ref velocity,
            0.05f,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );

        floatingGroup.position = currentMousePos;

        float tiltZ = Mathf.Clamp(-smoothDelta.x * dm.tiltStrength, -40f, 40f);
        floatingGroup.localRotation = Quaternion.Lerp(
            floatingGroup.localRotation,
            Quaternion.Euler(15f, 0f, tiltZ),
            Time.unscaledDeltaTime * dm.tiltSmooth
        );
    }

    void OnIconRelease(Gesture.OnRelease evt){
        if(!isDragging) return;

        isDragging = false;

        if(!dragStarted){
            string appName = IconSetting.appName;
            NewDesktopManager.Instance.OpenApp(appName.ToLower());
            return;
        }

        isAnimating = true;

        Transform closest = FindClosestSlot();
        if(closest != null && IsWithinSwapRadius(closest)){
            SwapSiblingOrder(pressedIconOriginalParent, closest);
            StartCoroutine(SlideBackAndRestore(
                Camera.main.WorldToScreenPoint(closest.position)
            ));
        }
        else{
            StartCoroutine(SlideBackAndRestore(pressedIconOriginalScreenPos));
        }
    }

    void OnIconHover(Gesture.OnHover evt){
        currentVisual = IconView?.Visuals as DesktopIconVisual;
        if(currentVisual == null) return;
        hoverActive = true;
    }
    void OnIconUnhover(Gesture.OnUnhover evt){
        if(currentVisual == null) return;
        hoverActive = false;
    }
    #endregion

    #region HELPERS LOGIC
    void CreateFloatingGroup(Sprite iconSprite, string appName){
        if(dm.DragCanvas == null) return;

        GameObject groupObj = new("FloatingIconGroup", typeof(RectTransform), typeof(CanvasGroup));
        groupObj.transform.SetParent(dm.DragCanvas.transform, false);

        floatingGroup = groupObj.GetComponent<RectTransform>();
        floatingGroup.sizeDelta = dm.iconSize;
        floatingGroup.position = Input.mousePosition;

        CanvasGroup groupCanvas = groupObj.GetComponent<CanvasGroup>();
        groupCanvas.alpha = dm.dragOpacity;

        GameObject iconObj = new("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(floatingGroup, false);

        floatingIcon = iconObj.GetComponent<Image>();
        floatingIcon.sprite = iconSprite;
        floatingIcon.raycastTarget = false;
        floatingIcon.rectTransform.sizeDelta = dm.iconImageSize;

        GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(floatingGroup, false);

        floatingLabel = labelObj.GetComponent<TextMeshProUGUI>();
        floatingLabel.text = appName;
        floatingLabel.font = dm.labelFont;
        floatingLabel.fontSize = dm.labelFontSize;
        floatingLabel.color = dm.labelColor;
        floatingLabel.alignment = TextAlignmentOptions.Center;
        floatingLabel.raycastTarget = false;
        floatingLabel.rectTransform.sizeDelta = dm.labelSize;
        floatingLabel.rectTransform.anchoredPosition = dm.labelOffset;
    }

    GameObject CreateFloatingClone(DesktopIconSetting setting, Transform source){
        if(setting == null || setting.appIcon == null) return null;

        GameObject clone = new("SwapClone", typeof(RectTransform), typeof(CanvasGroup));
        clone.transform.SetParent(dm.DragCanvas.transform, false);

        RectTransform rect = clone.GetComponent<RectTransform>();
        rect.sizeDelta = dm.iconSize;

        Transform sourceChild = source.childCount > 0 ? source.GetChild(0) : source;
        rect.position = Camera.main.WorldToScreenPoint(sourceChild.position);

        CanvasGroup cg = clone.GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        GameObject iconObj = new("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(rect, false);

        Image img = iconObj.GetComponent<Image>();
        img.sprite = setting.appIcon;
        img.rectTransform.sizeDelta = dm.iconImageSize;
        img.raycastTarget = false;

        GameObject labelObj = new("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(rect, false);

        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.text = setting.appName;
        tmp.font = dm.labelFont;
        tmp.fontSize = dm.labelFontSize;
        tmp.color = dm.labelColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.rectTransform.sizeDelta = dm.labelSize;
        tmp.rectTransform.anchoredPosition = dm.labelOffset;

        return clone;
    }

    void SetChildrenActive(Transform parent, bool active){
        if(parent == null) return;
        foreach(Transform child in parent) child.gameObject.SetActive(active);
    }

    Transform FindClosestSlot(){
        if(dm.iconSlots == null || dm.iconSlots.Count == 0) return null;

        Transform closest = null;
        float bestDist = float.MaxValue;
        Vector2 mouseScreen = Input.mousePosition;

        foreach(Transform slot in dm.iconSlots){
            if(slot == pressedIconOriginalParent) continue;
            Vector3 slotScreen = Camera.main.WorldToScreenPoint(slot.position);
            float d = Vector2.Distance(mouseScreen, slotScreen);
            if(d < bestDist){
                bestDist = d;
                closest = slot;
            }
        }

        return closest;
    }

    bool IsWithinSwapRadius(Transform target){
        Vector2 mouse = Input.mousePosition;
        Vector3 targetScreen = Camera.main.WorldToScreenPoint(target.position);
        return Vector2.Distance(mouse, targetScreen) <= dm.swapRadius;
    }

    void SwapSiblingOrder(Transform a, Transform b){
        if(a == null || b == null) return;

        int indexA = a.GetSiblingIndex();
        int indexB = b.GetSiblingIndex();
        a.SetSiblingIndex(indexB);
        b.SetSiblingIndex(indexA);

        int idxA = dm.iconSlots.IndexOf(a);
        int idxB = dm.iconSlots.IndexOf(b);
        if(idxA >= 0 && idxB >= 0) (dm.iconSlots[idxA], dm.iconSlots[idxB]) = (dm.iconSlots[idxB], dm.iconSlots[idxA]);
    }

    IEnumerator SlideBackAndRestore(Vector3 endScreenPos){
        isRestoring = true;

        Vector3 startPos = floatingGroup.position;
        Quaternion startRot = floatingGroup.localRotation;
        Vector3 startScale = floatingGroup.localScale;

        CanvasGroup groupCanvas = floatingGroup.GetComponent<CanvasGroup>();
        float startAlpha = groupCanvas.alpha;

        float distance = Vector3.Distance(startPos, endScreenPos);
        float duration = Mathf.Clamp(distance / dm.distanceDivisor, dm.minSlideDuration, dm.maxSlideDuration);
        float t = 0f;
        while(t < 1f){
            t += Time.unscaledDeltaTime / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            floatingGroup.position = Vector3.Lerp(startPos, endScreenPos, eased);
            if(dm.animateFullReturn){
                floatingGroup.localRotation = Quaternion.Lerp(startRot, Quaternion.Euler(15f, 0f, 0f), eased);
                floatingGroup.localScale = Vector3.Lerp(startScale, new Vector3(.85f, .85f, .85f), eased);
                groupCanvas.alpha = Mathf.Lerp(startAlpha, 1f, eased);
            }

            yield return null;
        }

        if(floatingGroup != null) Destroy(floatingGroup.gameObject);

        SetChildrenActive(pressedIconOriginalParent, true);
        floatingGroup = null;
        isRestoring = false;
        isAnimating = false;
    }

    IEnumerator SlideBackAndDestroy(GameObject clone, Vector3 endScreenPos, Transform targetSlot){
        if(clone == null){
            SetChildrenActive(targetSlot, true);
            yield break;
        }

        RectTransform rect = clone.GetComponent<RectTransform>();
        CanvasGroup cg = clone.GetComponent<CanvasGroup>();

        Vector3 startPos = rect.position;
        Vector3 startScale = rect.localScale;
        Vector3 targetScale = new(.85f, .85f, .85f);

        float distance = Vector3.Distance(startPos, endScreenPos);
        float duration = Mathf.Clamp(distance / dm.distanceDivisor, dm.minSlideDuration, dm.maxSlideDuration);
        float t = 0f;

        while(t < 1f){
            t += Time.unscaledDeltaTime / duration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            rect.position = Vector3.Lerp(startPos, endScreenPos, eased);
            rect.localScale = Vector3.Lerp(startScale, targetScale, eased);
            cg.alpha = Mathf.Lerp(dm.dragOpacity, 1f, eased);

            yield return null;
        }

        SetChildrenActive(targetSlot, true);
        Destroy(clone);
    }
    #endregion
}
