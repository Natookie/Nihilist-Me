using UnityEngine;
using Nova;
using UnityEngine.SceneManagement;

public class PlayButtonGestureHandler : MonoBehaviour
{
    [Header("COLORS")]
    public Color hoverColor = Color.white;
    public Color unhoverColor = Color.gray;

    [Header("Z MOVEMENT")]
    public float zUnhover = -30f;
    public float zHover = -60f;
    public float moveLerpSpeed = 12f;

    [Header("COLOR SMOOTHING")]
    public float colorLerpSpeed = 14f;

    private UIBlock2D block;
    private Color targetColor;
    private float targetZ;

    void Awake(){
        block = GetComponent<UIBlock2D>();

        targetColor = unhoverColor;
        targetZ = zUnhover;

        block.Color = unhoverColor;
    }

    void Start(){
        block.AddGestureHandler<Gesture.OnHover>(OnHover);
        block.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
        block.AddGestureHandler<Gesture.OnPress>(OnClick);
    }

    void Update(){
        Animate();
    }

    void OnHover(Gesture.OnHover evt){
        targetColor = hoverColor;
        targetZ = zHover;
    }

    void OnUnhover(Gesture.OnUnhover evt){
        targetColor = unhoverColor;
        targetZ = zUnhover;
    }

    void OnClick(Gesture.OnPress evt){
        SceneManager.LoadScene("Main Scene");
    }

    void Animate(){
        block.Color = Color.Lerp(
            block.Color,
            targetColor,
            Time.unscaledDeltaTime * colorLerpSpeed
        );

        Vector3 pos = transform.localPosition;
        pos.z = Mathf.Lerp(
            pos.z,
            targetZ,
            Time.unscaledDeltaTime * moveLerpSpeed
        );
        transform.localPosition = pos;
    }
}
