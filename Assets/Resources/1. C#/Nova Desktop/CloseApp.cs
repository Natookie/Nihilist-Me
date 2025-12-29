using UnityEngine;
using UnityEngine.SceneManagement;
using Nova;

[RequireComponent(typeof(Interactable))]
public class CloseApp : MonoBehaviour
{
    [Header("REFERENCES")]
    public Transform visual;

    [Header("ANIMATION")]
    public float hoverLift = 8f;
    public float pressLift = 25f;
    public float hoverScale = 1.05f;
    public float animSpeed = 12f;

    [Header("DIRECTION")]
    public float hoverRight = 6f;
    public float pressRight = 20f;

    [Header("SCENE")]
    public string desktopSceneName = "Main Scene";

    UIBlock2D block;

    Vector3 startLocalPos;
    Vector3 startLocalScale;

    Vector3 targetPos;
    Vector3 targetScale;

    void Awake(){
        block = GetComponent<UIBlock2D>();

        if(visual == null)
            visual = transform.GetChild(0);

        startLocalPos = visual.localPosition;
        startLocalScale = visual.localScale;

        targetPos = startLocalPos;
        targetScale = startLocalScale;
    }

    void Start(){
        block.AddGestureHandler<Gesture.OnHover>(OnHover);
        block.AddGestureHandler<Gesture.OnUnhover>(OnUnhover);
        block.AddGestureHandler<Gesture.OnPress>(OnPress);
    }

    void Update(){
        visual.localPosition = Vector3.Lerp(
            visual.localPosition,
            targetPos,
            Time.deltaTime * animSpeed
        );

        visual.localScale = Vector3.Lerp(
            visual.localScale,
            targetScale,
            Time.deltaTime * animSpeed
        );
    }

    void OnHover(Gesture.OnHover evt){
        targetPos = startLocalPos +
            Vector3.up * hoverLift +
            Vector3.right * hoverRight;

        targetScale = startLocalScale * hoverScale;
    }

    void OnUnhover(Gesture.OnUnhover evt){
        targetPos = startLocalPos;
        targetScale = startLocalScale;
    }

    void OnPress(Gesture.OnPress evt){
        targetPos = startLocalPos +
            Vector3.up * pressLift +
            Vector3.right * pressRight;

        targetScale = startLocalScale * hoverScale;

        Invoke(nameof(LoadDesktop), 0.15f);
    }

    void LoadDesktop(){
        SceneManager.LoadScene(desktopSceneName);
    }
}
