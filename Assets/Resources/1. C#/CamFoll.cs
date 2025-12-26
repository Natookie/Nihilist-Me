using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using Cinemachine;
using Nova;

public class CamFoll : MonoBehaviour
{
    [Header("HORIZONTAL PAN")]
    public float maxXOffset = 2f;
    public float followSmooth = 8f;

    [Header("VERTICAL / DEPTH")]
    public float fixedY = 5f;
    public float fixedZ = -10f;

    [Header("ZOOM LOGIC")]
    public float minFOV = 30f;
    public float maxFOV = 60f;
    public float zoomSpeed = 20f;
    public float fovSmoothSpeed = 10f;

    [Header("DESKTOP TRANSITION")]
    public Transform monitorTarget;
    public CanvasGroup computerCanvas;
    public float zoomDuration = 1f;
    public float uiFadeDuration = 0.5f;
    [Space(10)]
    public bool isUsingNewDesktop;

    [Header("DESKTOP REFERENCES")]
    public GameObject desktopScreen;
    public GameObject rezzitScreen;
    public UIBlock2D desktopVisual;
    public Color desktopActiveColor = Color.white;
    public CanvasGroup crtOverlay;

    [Header("POST PROCESSING")]
    public Volume roomVolume;
    public Volume desktopVolume;
    public Volume rezzitVolume;

    [Header("SOUND EFFECT")]
    public AudioSource audioSource;
    public AudioClip chimeSFX;

    [Header("REFERENCES")]
    public Transform player;
    public Camera worldCam;
    public CustomCursor cc;
    public CinemachineVirtualCamera vcam;

    float _targetFOV;
    float _currentXOffset;

    bool _isZooming;
    bool _onMonitor;
    Coroutine _transitionRoutine;

    void Start(){
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        _targetFOV = worldCam.fieldOfView;

        if(computerCanvas == null || desktopScreen == null) return;

        computerCanvas.alpha = 0f;
        desktopScreen.SetActive(false);
        if(desktopVisual != null){
            Color col = desktopActiveColor;
            col.a = 0f;
            desktopVisual.Color = col;
        }

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(false);
    }

    void LateUpdate(){
        if(_isZooming || (computerCanvas && computerCanvas.alpha == 1f)) return;
        if(desktopScreen.activeSelf) return;

        HandleMouseX();
        HandleCamPos();
        HandleZoom();
    }

    void HandleMouseX(){
        Vector3 mouseView = worldCam.ScreenToViewportPoint(Input.mousePosition);
        float centeredX = (mouseView.x - 0.5f) * 2f;
        centeredX = Mathf.Clamp(centeredX, -1f, 1f);

        float desiredOffset = centeredX * maxXOffset;
        float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        _currentXOffset = Mathf.Lerp(_currentXOffset, desiredOffset, t);
    }

    void HandleCamPos(){
        Vector3 target = new Vector3(
            player.position.x + _currentXOffset,
            fixedY,
            fixedZ
        );

        float t = 1f - Mathf.Exp(-followSmooth * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, target, t);
    }

    void HandleZoom(){
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if(Mathf.Abs(scroll) > 0.0001f) _targetFOV = Mathf.Clamp(_targetFOV - scroll * zoomSpeed, minFOV, maxFOV);

        var lens = vcam.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, _targetFOV, fovSmoothSpeed * Time.deltaTime);
        vcam.m_Lens = lens;
    }

    public void StartTransition(bool toMonitor){
        if(_onMonitor == toMonitor) return;
        if(rezzitScreen.activeSelf) return;
        if(_transitionRoutine != null) StopCoroutine(_transitionRoutine);

        _transitionRoutine = toMonitor
            ? StartCoroutine(ZoomToMonitor())
            : StartCoroutine(ReturnFromMonitor());
    }

    IEnumerator ZoomToMonitor(){
        _isZooming = true;
        _onMonitor = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(
            monitorTarget.position.x,
            monitorTarget.position.y,
            monitorTarget.position.z - 1f
        );

        float startFOV = vcam.m_Lens.FieldOfView;
        float endFOV = 15f;

        float t = 0f;
        while(t < 1f){
            t += Time.deltaTime / zoomDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            var lens = vcam.m_Lens;
            lens.FieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            vcam.m_Lens = lens;
            yield return null;
        }

        if(isUsingNewDesktop && desktopVisual != null){
            desktopScreen.SetActive(true);
            UpdatePostProcessingPriority();

            audioSource.PlayOneShot(chimeSFX);
            AudioManager.Instance.EnableChannel(AudioChannel.Music, false);
            AudioManager.Instance.EnableChannel(AudioChannel.Ambience, false);
            cc.CancelMonologue();

            foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(true);
            Color start = desktopVisual.Color;
            Color end = desktopActiveColor;
            end.a = 1f;

            float a = 0f;
            while(a < 1f){
                a += Time.deltaTime / uiFadeDuration;
                desktopVisual.Color = Color.Lerp(start, end, a);
                if(crtOverlay != null) crtOverlay.alpha = a;

                yield return null;
            }

            List<Coroutine> flickers = new();
            foreach(Transform child in desktopScreen.transform){
                if(crtOverlay != null && child == crtOverlay.transform) continue;
                flickers.Add(StartCoroutine(RandomFadeChild(child)));
            }

            yield return new WaitForSeconds(.5f);
        }else if(!isUsingNewDesktop && computerCanvas){
            float f = 0f;
            while(f < 1f){
                f += Time.deltaTime / uiFadeDuration;
                computerCanvas.alpha = f;

                yield return null;
            }
        }

        _transitionRoutine = null;
        _isZooming = false;
    }

    IEnumerator ReturnFromMonitor(){
        _isZooming = true;
        _onMonitor = false;

        if(isUsingNewDesktop && desktopVisual != null){
            foreach(Transform child in desktopScreen.transform){
                if(crtOverlay != null && child == crtOverlay.transform) continue;
                StartCoroutine(FadeChildTo(child, 0f, .3f));
            }

            Color start = desktopVisual.Color;
            Color end = desktopVisual.Color;
            end.a = 0f;

            AudioManager.Instance.EnableChannel(AudioChannel.Music, true);
            AudioManager.Instance.EnableChannel(AudioChannel.Ambience, true);

            float a = 0f;
            while(a < 1f){
                a += Time.deltaTime / uiFadeDuration;
                desktopVisual.Color = Color.Lerp(start, end, a);
                if(crtOverlay != null) crtOverlay.alpha = 1f - a;

                yield return null;
            }

            desktopScreen.SetActive(false);
            UpdatePostProcessingPriority();
        }else if(!isUsingNewDesktop && computerCanvas){
            float f = 1f;
            while(f > 0f){
                f -= Time.deltaTime / uiFadeDuration;
                computerCanvas.alpha = f;
                yield return null;
            }
        }

        Vector3 startPos = transform.position;
        float startFOV = vcam.m_Lens.FieldOfView;

        Vector3 endPos = new Vector3(
            player.position.x + _currentXOffset,
            fixedY,
            fixedZ
        );
        float endFOV = _targetFOV;

        float t = 0f;
        while(t < 1f){
            t += Time.deltaTime / zoomDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            var lens = vcam.m_Lens;
            lens.FieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            vcam.m_Lens = lens;
            yield return null;
        }
        _transitionRoutine = null;
        _isZooming = false;
    }

    IEnumerator RandomFadeChild(Transform child){
        if(child == null) yield break;

        float totalDuration = Random.Range(0.8f, 1.4f);
        float elapsed = 0f;

        var blocks = child.GetComponentsInChildren<UIBlock2D>(true);
        var texts = child.GetComponentsInChildren<TextBlock>(true);

        while(elapsed < totalDuration){
            float targetAlpha = Random.value > 0.5f ? 1f : 0f;
            float flickSpeed = Random.Range(0.05f, 0.15f);

            yield return StartCoroutine(FadeChildTo(child, targetAlpha, flickSpeed));
            elapsed += flickSpeed;
        }

        yield return StartCoroutine(FadeChildTo(child, 1f, 0.2f));
    }

    IEnumerator FadeChildTo(Transform child, float targetAlpha, float duration){
        if(child == null) yield break;

        var blocks = child.GetComponentsInChildren<UIBlock2D>(true);
        var texts = child.GetComponentsInChildren<TextBlock>(true);

        float t = 0f;
        Dictionary<UIBlock2D, (Color c, Color s)> blockStart = new();
        Dictionary<TextBlock, Color> textStart = new();

        foreach(var b in blocks) blockStart[b] = (b.Color, b.Shadow.Color);
        foreach(var txt in texts) textStart[txt] = txt.Color;

        while(t < 1f){
            t += Time.deltaTime / duration;
            float a = Mathf.Lerp(0f, 1f, t);

            foreach(var kvp in blockStart){
                UIBlock2D b = kvp.Key;
                Color c = kvp.Value.c;
                Color s = kvp.Value.s;
                float startA = c.a;

                c.a = Mathf.Lerp(startA, targetAlpha, a);
                s.a = c.a;
                b.Color = c;
                b.Shadow.Color = s;
            }

            foreach (var kvp in textStart){
                TextBlock txt = kvp.Key;
                Color c = kvp.Value;
                float startA = c.a;
                c.a = Mathf.Lerp(startA, targetAlpha, a);
                txt.Color = c;
            }

            yield return null;
        }
    }

    public void UpdatePostProcessingPriority(){
        if(!rezzitScreen || !roomVolume || !desktopVolume){
            return;
        }

        if(rezzitScreen.activeSelf){
            roomVolume.priority = 0;
            desktopVolume.priority = 0;
            rezzitVolume.priority = 20;
            return;
        }

        rezzitVolume.priority = 0;
        if(desktopScreen.activeSelf){
            roomVolume.priority = 0;
            desktopVolume.priority = 20;
        }else{
            roomVolume.priority = 20;
            desktopVolume.priority = 0;
        }
    }
}
