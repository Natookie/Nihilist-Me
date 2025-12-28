using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class MenuButtonAnimation : MonoBehaviour
{
    // Object references
    [SerializeField] private GameObject square;
    // Animation variables
    public float scaleJuice = 1.1f;
    public float selectionRotateAngle = 45f;
    public Color selectedColor = Color.cyan;
    public Color deselectedColor = Color.white;

    public void Start()
    {
        // Assertion check
        Assert.IsTrue(square, "square is missing");
    }

    #region MAIN FUNCTIONS
    virtual public void AnimateNormal()
    {
        float targetAngle = selectionRotateAngle;
        Color targetColor = deselectedColor;

        StopAllCoroutines();

        StartCoroutine(AnimateSquare(square.transform, targetAngle, targetColor));
        StartCoroutine(AnimateScale(transform, 1f));
    }

    virtual public void AnimateHover()
    {
        float targetAngle = -selectionRotateAngle;
        Color targetColor = selectedColor;

        StopAllCoroutines();

        StartCoroutine(AnimateSquare(square.transform, targetAngle, targetColor));
        StartCoroutine(AnimateScale(transform, scaleJuice));
    }

    virtual public void AnimatePressed()
    {
        
    }

    virtual public void ResetAnimation()
    {
        square.transform.localRotation = Quaternion.Euler(0f, 0f, selectionRotateAngle);
        Image img = square.GetComponent<Image>();
        img.color = deselectedColor;
    }

    private IEnumerator AnimateSquare(Transform square, float targetAngle, Color targetColor){
        Image img = square.GetComponent<Image>();
        float duration = .25f;
        float elapsed = 0f;

        Quaternion startRot = square.localRotation;
        Quaternion endRot = Quaternion.Euler(0f, 0f, targetAngle);
        Color startColor = img.color;

        while(elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            square.localRotation = Quaternion.Slerp(startRot, endRot, t);
            img.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        square.localRotation = endRot;
        img.color = targetColor;
    }
    #endregion

    private IEnumerator AnimateScale(Transform target, float targetScale){
        float duration = .25f;
        float elapsed = 0f;
        Vector3 startScale = target.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        while(elapsed < duration){
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
    }
}
