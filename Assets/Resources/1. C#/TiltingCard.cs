using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TiltingCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tilt")]
    public float maxTiltX = 8f;
    public float maxTiltY = 8f;
    public float stiffness = 60f;
    [Range(0f, 1f)] public float damping = 0.85f;
    public float edgeEase = 0.7f;

    [Header("Scale")]
    public float hoverScale = 1.06f;
    public float scaleStiffness = 35f;
    [Range(0f, 1f)] public float scaleDamping = 0.8f;

    [Header("Layered Depth Illusion")]
    [Tooltip("Optional layers that move at different parallax depths.")]
    public RectTransform layerFront;
    public RectTransform layerMid;
    public RectTransform layerBack;
    [Tooltip("Z offset multiplier for parallax layers.")]
    public float layerDepthStrength = 4f;

    [Header("Lighting")]
    public Graphic highlightGraphic;
    public float highlightIntensity = 0.25f;
    public Color highlightBaseColor = Color.white;

    [Header("Misc")]
    public float deadzone = 0.03f;

    private RectTransform rect;
    private Vector2 currentTilt = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    private float currentScale;
    private float scaleVelocity = 0f;
    private Vector2 targetTilt = Vector2.zero;
    private float targetScale = 1f;
    private bool isHovered;

    private Vector3 frontDefault, midDefault, backDefault;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        currentScale = rect.localScale.x;

        if (layerFront) frontDefault = layerFront.localPosition;
        if (layerMid) midDefault = layerMid.localPosition;
        if (layerBack) backDefault = layerBack.localPosition;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        // --- INPUT TILT CALC ---
        if (isHovered)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, Input.mousePosition, null, out Vector2 local);
            float nx = Mathf.Clamp(local.x / (rect.rect.width * 0.5f), -1f, 1f);
            float ny = Mathf.Clamp(local.y / (rect.rect.height * 0.5f), -1f, 1f);

            nx = Mathf.Sign(nx) * Mathf.Pow(Mathf.Abs(nx), edgeEase);
            ny = Mathf.Sign(ny) * Mathf.Pow(Mathf.Abs(ny), edgeEase);

            float tRotX = ny * maxTiltX;
            float tRotY = -nx * maxTiltY;

            Vector2 candidate = new Vector2(tRotX, tRotY);
            if (candidate.magnitude < deadzone * Mathf.Max(maxTiltX, maxTiltY))
                candidate = Vector2.zero;

            targetTilt = candidate;
            targetScale = hoverScale;
        }
        else
        {
            targetTilt = Vector2.zero;
            targetScale = 1f;
        }

        // --- SPRING DYNAMICS ---
        Vector2 accel = (targetTilt - currentTilt) * stiffness;
        velocity += accel * dt;
        velocity *= Mathf.Pow(damping, dt * 60f);
        currentTilt += velocity * dt;

        float scaleAccel = (targetScale - currentScale) * scaleStiffness;
        scaleVelocity += scaleAccel * dt;
        scaleVelocity *= Mathf.Pow(scaleDamping, dt * 60f);
        currentScale += scaleVelocity * dt;

        // --- APPLY TO CARD ---
        rect.localEulerAngles = new Vector3(currentTilt.x, currentTilt.y, 0f);
        rect.localScale = Vector3.one * currentScale;

        // --- PARALLAX DEPTH EFFECT ---
        float xOff = -currentTilt.y * layerDepthStrength; // invert to feel like depth
        float yOff = currentTilt.x * layerDepthStrength;

        if (layerFront) layerFront.localPosition = frontDefault + new Vector3(xOff * 1.2f, yOff * 1.2f, 0f);
        if (layerMid)   layerMid.localPosition = midDefault + new Vector3(xOff * 0.6f, yOff * 0.6f, 0f);
        if (layerBack)  layerBack.localPosition = backDefault + new Vector3(xOff * 0.2f, yOff * 0.2f, 0f);

        // --- LIGHTING SIM ---
        if (highlightGraphic)
        {
            float intensity = Mathf.InverseLerp(-maxTiltY, maxTiltY, currentTilt.y);
            Color c = highlightBaseColor * (1f + (intensity - 0.5f) * highlightIntensity * 2f);
            c.a = highlightGraphic.color.a;
            highlightGraphic.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => isHovered = true;
    public void OnPointerExit(PointerEventData eventData) => isHovered = false;
}
