using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
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
    public float zoomDuration = 1f;
    public float uiFadeDuration = 0.5f;

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

    [Header("SCREENSHOOT")]
    public int screenshotWidth = 1920;
    public int screenshotHeight = 1080;
    public string screenshotFileName = "LastDesktopView.png";
    [Space(5)]
    public Renderer desktopTargetRenderer;

    Texture2D _lastDesktopScreenshot;

    [Header("REFERENCES")]
    public Transform player;
    public Camera worldCam;
    public CustomCursor cc;
    public CinemachineVirtualCamera vcam;

    [Header("DEBUG")]
    public bool disableDesktopFlicker = false;

    float _targetFOV;
    float _currentXOffset;

    bool _isZooming;
    bool _onMonitor;
    bool _flickerFinished;

    Coroutine _transitionRoutine;
    int _activeFlickers;

    public bool IsDesktopFXDone => _flickerFinished;
    public bool IsDesktopReady => _flickerFinished && !_isZooming && _onMonitor;

    void Start(){
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        _targetFOV = vcam.m_Lens.FieldOfView;

        if(desktopScreen == null) return;

        desktopScreen.SetActive(false);

        if(desktopVisual != null){
            Color c = desktopActiveColor;
            c.a = 0f;
            desktopVisual.Color = c;
        }

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(false);
    }

    void LateUpdate(){
        if(_isZooming || desktopScreen.activeSelf) return;

        HandleMouseX();
        HandleCamPos();
        HandleZoom();
    }

    #region CAMERA FOLLOW
    void HandleMouseX(){
        Vector3 mouseView = worldCam.ScreenToViewportPoint(Input.mousePosition);
        float centeredX = Mathf.Clamp((mouseView.x - 0.5f) * 2f, -1f, 1f);

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
        if(Mathf.Abs(scroll) > 0.0001f)
            _targetFOV = Mathf.Clamp(_targetFOV - scroll * zoomSpeed, minFOV, maxFOV);

        var lens = vcam.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, _targetFOV, fovSmoothSpeed * Time.deltaTime);
        vcam.m_Lens = lens;
    }
    #endregion

    #region DESKTOP TRANSITION
    public void StartTransition(bool toMonitor){
        if(_onMonitor == toMonitor || rezzitScreen.activeSelf) return;

        if(_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _transitionRoutine = StartCoroutine(toMonitor ? ZoomToMonitor() : ReturnFromMonitor());
    }

    // Only used for zoom to monitor
    public void TransistionImmediately()
    {
        if(_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);

        _onMonitor = true;

        Vector3 endPos = monitorTarget.position + Vector3.back;
        float endFOV = 15f;
        transform.position = endPos;
        var lens = vcam.m_Lens;
        lens.FieldOfView = endFOV;
        vcam.m_Lens = lens;

        desktopScreen.SetActive(true);
        UpdatePostProcessingPriority();

        AudioManager.Instance.EnableChannel(AudioChannel.Music, false);
        AudioManager.Instance.EnableChannel(AudioChannel.Ambience, false);
        cc.CancelMonologue();

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(true);

        Color end = desktopActiveColor;
        end.a = 1f;
        desktopVisual.Color = end;
        if(crtOverlay) crtOverlay.alpha = 1f;
        // foreach(Transform child in desktopScreen.transform){
        //     if(crtOverlay && child == crtOverlay.transform) continue;

        //     _activeFlickers++;
        //     StartCoroutine(FadeChildTracked(child));
        // }

        // if(_activeFlickers == 0) _flickerFinished = true;

        _transitionRoutine = null;
        _isZooming = false;
        _flickerFinished = true;
    }

    IEnumerator ZoomToMonitor(){
        _isZooming = true;
        _onMonitor = true;
        _flickerFinished = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = monitorTarget.position + Vector3.back;

        float startFOV = vcam.m_Lens.FieldOfView;
        float endFOV = 15f;

        yield return ZoomRoutine(startPos, endPos, startFOV, endFOV);

        desktopScreen.SetActive(true);
        UpdatePostProcessingPriority();

        audioSource.PlayOneShot(chimeSFX);
        AudioManager.Instance.EnableChannel(AudioChannel.Music, false);
        AudioManager.Instance.EnableChannel(AudioChannel.Ambience, false);
        cc.CancelMonologue();

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(true);
        yield return FadeDesktop(true);

        _transitionRoutine = null;
        _isZooming = false;
    }

    IEnumerator ReturnFromMonitor(){
        _isZooming = true;
        _onMonitor = false;
        _flickerFinished = false;
        CaptureCurrentView();

        yield return FadeDesktop(false);

        desktopScreen.SetActive(false);
        UpdatePostProcessingPriority();

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(player.position.x + _currentXOffset, fixedY, fixedZ);
        float startFOV = vcam.m_Lens.FieldOfView;
        float endFOV = _targetFOV;

        AudioManager.Instance.EnableChannel(AudioChannel.Music, true);
        AudioManager.Instance.EnableChannel(AudioChannel.Ambience, true);

        yield return ZoomRoutine(startPos, endPos, startFOV, endFOV);

        _transitionRoutine = null;
        _isZooming = false;
    }

    IEnumerator ZoomRoutine(Vector3 startPos, Vector3 endPos, float startFOV, float endFOV){
        float t = 0f;
        while(t < 1f){
            t += Time.deltaTime / zoomDuration;
            transform.position = Vector3.Lerp(startPos, endPos, t);

            var lens = vcam.m_Lens;
            lens.FieldOfView = Mathf.Lerp(startFOV, endFOV, t);
            vcam.m_Lens = lens;

            yield return null;
        }
    }
    #endregion

    #region UI FADES
    IEnumerator FadeDesktop(bool fadeIn){
        _activeFlickers = 0;

        Color start = desktopVisual.Color;
        Color end = desktopActiveColor;
        end.a = fadeIn ? 1f : 0f;

        float t = 0f;
        while(t < 1f){
            t += Time.deltaTime / uiFadeDuration;
            desktopVisual.Color = Color.Lerp(start, end, t);
            if(crtOverlay) crtOverlay.alpha = fadeIn ? t : 1f - t;
            yield return null;
        }

        if(!fadeIn){
            _flickerFinished = false;
            yield break;
        }

        foreach(Transform child in desktopScreen.transform){
            if(crtOverlay && child == crtOverlay.transform) continue;

            _activeFlickers++;
            StartCoroutine(FadeChildTracked(child));
        }

        if(_activeFlickers == 0) _flickerFinished = true;
    }

    IEnumerator FadeChildTracked(Transform child){
        yield return FadeChild(child);
        _activeFlickers--;
        if(_activeFlickers <= 0) _flickerFinished = true;
    }

    IEnumerator FadeChild(Transform child){
        if(child == null) yield break;

        UIBlock2D[] blocks = child.GetComponentsInChildren<UIBlock2D>(true);
        TextBlock[] texts = child.GetComponentsInChildren<TextBlock>(true);

        float total = Random.Range(0.8f, 1.4f);
        float elapsed = 0f;

        while(elapsed < total){
            float target = Random.value > 0.5f ? 1f : 0f;
            float speed = Random.Range(0.05f, 0.15f);
            yield return FadeElements(blocks, texts, target, speed);
            elapsed += speed;
        }

        yield return FadeElements(blocks, texts, 1f, 0.2f);
    }

    IEnumerator FadeElements(UIBlock2D[] blocks, TextBlock[] texts, float targetAlpha, float duration){
        float t = 0f;

        while(t < 1f){
            t += Time.deltaTime / duration;

            foreach(var b in blocks){
                Color c = b.Color;
                Color s = b.Shadow.Color;
                c.a = Mathf.Lerp(c.a, targetAlpha, t);
                s.a = c.a;
                b.Color = c;
                b.Shadow.Color = s;
            }

            foreach(var txt in texts){
                Color c = txt.Color;
                c.a = Mathf.Lerp(c.a, targetAlpha, t);
                txt.Color = c;
            }

            yield return null;
        }
    }
    #endregion

    #region POST PROCESSING
    public void UpdatePostProcessingPriority(){
        if(rezzitScreen.activeSelf){
            roomVolume.priority = 0;
            desktopVolume.priority = 0;
            rezzitVolume.priority = 20;
            return;
        }

        rezzitVolume.priority = 0;
        roomVolume.priority = desktopScreen.activeSelf ? 0 : 20;
        desktopVolume.priority = desktopScreen.activeSelf ? 20 : 0;
    }
    #endregion

    void CaptureCurrentView(){
        string folderPath = System.IO.Path.Combine(
            Application.dataPath,
            "Resources/2. Art/Desktop"
        );

        if(!System.IO.Directory.Exists(folderPath))
            System.IO.Directory.CreateDirectory(folderPath);

        string fullPath = System.IO.Path.Combine(folderPath, screenshotFileName);

        RenderTexture rt = new RenderTexture(
            screenshotWidth,
            screenshotHeight,
            24,
            RenderTextureFormat.ARGB32
        );

        Camera cam = vcam.VirtualCameraGameObject.GetComponent<Camera>();
        if(cam == null) cam = worldCam;

        RenderTexture prevRT = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(
            screenshotWidth,
            screenshotHeight,
            TextureFormat.RGB24,
            false
        );

        tex.ReadPixels(
            new Rect(0, 0, screenshotWidth, screenshotHeight),
            0,
            0
        );
        tex.Apply();

        cam.targetTexture = prevRT;
        RenderTexture.active = null;
        Destroy(rt);

        byte[] png = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, png);
        if(desktopTargetRenderer != null) desktopTargetRenderer.material.SetTexture("_MainTex", tex);

    #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
    #endif
    }
}

/*
================= OLD DESKTOP SYSTEM (DISABLED) =================
- computerCanvas fade logic
- non-CRT desktop UI
- isUsingNewDesktop checks
- bonnie blue makan spageti depan embassy indonesia
================================================================
*/
