using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class PauseOptionSelection : MonoBehaviour
{
    public event EventHandler<OnOptionPressedEventArgs> OnOptionPressed;
    public class OnOptionPressedEventArgs : EventArgs{
        public PauseOptionSelection pauseOptionSelection;
    }

    // Object references
    [SerializeField] private GameObject square;
    public PauseSidePanel targetSidePanel;
    // Animation variables
    public float scaleJuice = 1.1f;
    public float selectionRotateAngle = 45f;
    public Color selectedColor = Color.cyan;
    public Color deselectedColor = Color.white;

    public void Start()
    {
        // Assertion check
        Assert.IsTrue(square, "square is missing");
        Assert.IsTrue(targetSidePanel, "targetSidePanel is missing");
    }

    public void SelectOption()
    {
        float targetAngle = -selectionRotateAngle;
        Color targetColor = selectedColor;

        StopAllCoroutines();

        StartCoroutine(AnimateSquare(square.transform, targetAngle, targetColor));
        StartCoroutine(AnimateScale(transform, scaleJuice));
    }

    public void DeselectOption()
    {
        float targetAngle = selectionRotateAngle;
        Color targetColor = deselectedColor;

        StopAllCoroutines();

        StartCoroutine(AnimateSquare(square.transform, targetAngle, targetColor));
        StartCoroutine(AnimateScale(transform, 1f));
    }

    public void ResetOption()
    {
        square.transform.localRotation = Quaternion.Euler(0f, 0f, selectionRotateAngle);
        Image img = square.GetComponent<Image>();
        img.color = deselectedColor;
    }

    public void OptionClicked()
    {
        OnOptionPressed?.Invoke(this, new OnOptionPressedEventArgs
        {
            pauseOptionSelection = this
        });
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
