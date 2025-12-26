using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;

public class PauseSidePanel : MonoBehaviour
{
    // Object references
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private CanvasGroup canvasGroup;
    // Animation variables
    public float moveDurationPerItem = .4f;
    public float staggerDelay = .1f;
    [Range(0f, 1f)] public float bounceIntensity = .1f;
    public float moveDistance = 100f;

    private Vector2 originalPosition;

    private void Start()
    {
        // Assertion check
        Assert.IsNotNull(rectTransform, "rectTransform is missing");
        Assert.IsNotNull(canvasGroup, "canvasGroup is missing");
    }

    public void OpenSidePanel()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateSideObject(true));
    }

    public void CloseSidePanel()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateSideObject(false));
    }

    public void ResetSidePanel()
    {
        rectTransform.anchoredPosition = originalPosition;
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        
    }

    private IEnumerator AnimateSideObject(bool show){
        float duration = show ? 0.35f : 0.25f;
        float elapsed = 0f;
        // Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = originalPosition;

        if(show){
            // gameObject.SetActive(true);
            rectTransform.anchoredPosition = endPos + new Vector2(120f, 0f);
        }

        Vector2 from = show ? rectTransform.anchoredPosition : endPos;
        Vector2 to   = show ? endPos : (endPos + new Vector2(120f, 0f));

        float startAlpha = canvasGroup.alpha;
        float targetAlpha = show ? 1f : 0f;

        while(elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            rectTransform.anchoredPosition = Vector2.Lerp(from, to, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        rectTransform.anchoredPosition = to;
        canvasGroup.alpha = targetAlpha;

        if(!show) gameObject.SetActive(false);
    }
}
