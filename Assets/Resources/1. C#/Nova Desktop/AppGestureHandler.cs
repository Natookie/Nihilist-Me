using UnityEngine;
using Nova;
using System.Collections;
using System.Collections.Generic;

public enum AppWindowState
{
    Closed,
    Minimized,
    Open
}

public class AppGestureHandler : MonoBehaviour
{
    [Header("NOVA DATA")]
    public UIBlock Root;
    public UIBlock2D minimizeUtil;
    public UIBlock2D fullUtil;
    public UIBlock2D closeUtil;

    [Header("TWEAKS")]
    public bool canSplit;
    public GameObject fullMode;
    public GameObject splitMode;

    const float HIDDEN_Y = -1000f;
    const float DEFAULT_X = 0f;

    bool isDragging;
    bool isAnimating;
    public bool isFullscreen;

    Vector3 preFullscreenPos;
    Length preFullscreenX;
    Length preFullscreenY;

    Vector2 lastMousePos;

    Vector3 originalPos;
    Vector3 closedPos;
    Vector3 lastMinimizedPos;

    public AppWindowState State { get; private set; }

    Dictionary<UIBlock, Coroutine> utilRoutines = new();
    Coroutine moveRoutine;

    void Start(){
        Root ??= GetComponent<UIBlock>();

        originalPos = Root.Position.Value;
        closedPos = new Vector3(DEFAULT_X, HIDDEN_Y, originalPos.z);
        lastMinimizedPos = originalPos;

        preFullscreenX = Root.Size.X;
        preFullscreenY = Root.Size.Y;

        Root.Position.Value = closedPos;
        Root.GetComponent<Interactable>().enabled = false;
        State = AppWindowState.Closed;

        Root.AddGestureHandler<Gesture.OnPress>(OnPress);
        Root.AddGestureHandler<Gesture.OnDrag>(OnDrag);
        Root.AddGestureHandler<Gesture.OnRelease>(OnRelease);

        RegisterUtil(minimizeUtil);
        RegisterUtil(fullUtil);
        RegisterUtil(closeUtil);

        if(canSplit){
            if(fullMode != null) fullMode.SetActive(false);
            if(splitMode != null) splitMode.SetActive(true);
        }
    }

    void RegisterUtil(UIBlock2D block){
        if(block == null) return;

        UIBlock child = block.GetChild(0);

        block.AddGestureHandler<Gesture.OnPress>(e => {
            if(block == minimizeUtil) Minimize();
            else if(block == closeUtil) Close();
            else ToggleFullScreen();
        });

        block.AddGestureHandler<Gesture.OnHover>(e => AnimateUtil(child, 1f));
        block.AddGestureHandler<Gesture.OnUnhover>(e => AnimateUtil(child, 0f));
    }

    void AnimateUtil(UIBlock target, float to){
        if(utilRoutines.TryGetValue(target, out var r))
            StopCoroutine(r);

        utilRoutines[target] = StartCoroutine(UtilRoutine(target, to));
    }

    IEnumerator UtilRoutine(UIBlock target, float to){
        float from = target.Size.Y.Percent;
        float t = 0f;

        while(t < 1f){
            t += Time.unscaledDeltaTime * 10f;
            target.Size.Y.Percent = Mathf.Lerp(from, to, t);
            yield return null;
        }

        target.Size.Y.Percent = to;
    }

    public void Open(){
        if(State == AppWindowState.Open){
            transform.SetAsLastSibling();
            return;
        }

        Root.GetComponent<Interactable>().enabled = true;
        transform.SetAsLastSibling();

        if(State == AppWindowState.Minimized) MoveTo(lastMinimizedPos, true);
        else MoveTo(new Vector3(DEFAULT_X, 0f, originalPos.z), true);

        State = AppWindowState.Open;
    }

    void Minimize(){
        if(State != AppWindowState.Open) return;

        lastMinimizedPos = Root.Position.Value;
        Root.GetComponent<Interactable>().enabled = false;
        State = AppWindowState.Minimized;

        MoveTo(closedPos, true);
    }

    public void Close(){
        if(moveRoutine != null) StopCoroutine(moveRoutine);

        Root.GetComponent<Interactable>().enabled = false;
        State = AppWindowState.Closed;

        Root.Position.Value = closedPos;
        Root.Size.X = preFullscreenX;
        Root.Size.Y = preFullscreenY;
        isFullscreen = false;

        isAnimating = false;
    }

    void MoveTo(Vector3 target, bool smooth){
        if(moveRoutine != null) StopCoroutine(moveRoutine);

        if(smooth) moveRoutine = StartCoroutine(MoveRoutine(target));
        else Root.Position.Value = target;
    }

    IEnumerator MoveRoutine(Vector3 target){
        isAnimating = true;

        Vector3 from = Root.Position.Value;
        float t = 0f;

        while(t < 1f){
            t += Time.unscaledDeltaTime * 6f;

            Root.Position.Value = new Vector3(
                Mathf.Lerp(from.x, target.x, t),
                Mathf.Lerp(from.y, target.y, t),
                originalPos.z
            );

            yield return null;
        }

        Root.Position.Value = target;
        isAnimating = false;
    }

    void ToggleFullScreen(){
        if(State != AppWindowState.Open) return;

        if(!isFullscreen){
            preFullscreenPos = Root.Position.Value;
            preFullscreenX = Root.Size.X;
            preFullscreenY = Root.Size.Y;

            Root.Position.Value = new Vector3(0f, 0f, originalPos.z);
            Root.Size.X.Percent = .9f;
            Root.Size.Y.Percent = .9f;

            if(canSplit){
                if(splitMode != null) splitMode.SetActive(false);
                if(fullMode != null) fullMode.SetActive(true);
            }

            isFullscreen = true;
        }else{
            Root.Position.Value = preFullscreenPos;
            Root.Size.X = preFullscreenX;
            Root.Size.Y = preFullscreenY;

            if(canSplit){
                if(fullMode != null) fullMode.SetActive(false);
                if(splitMode != null) splitMode.SetActive(true);
            }

            isFullscreen = false;
        }
    }

    void OnPress(Gesture.OnPress evt){
        if(isAnimating || State != AppWindowState.Open) return;

        transform.SetAsLastSibling();
        isDragging = true;
        lastMousePos = Input.mousePosition;
    }

    void OnDrag(Gesture.OnDrag evt){
        if(!isDragging || isAnimating) return;

        Vector2 current = Input.mousePosition;
        Vector2 delta = current - lastMousePos;
        lastMousePos = current;

        Root.Position.Value += new Vector3(delta.x, delta.y, 0f);
        lastMinimizedPos = Root.Position.Value;
    }

    void OnRelease(Gesture.OnRelease evt){
        isDragging = false;
    }
}
