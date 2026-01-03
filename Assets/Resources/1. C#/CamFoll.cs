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
    public float uiFadeDuration = .5f;

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
    const float CAM_Z_EPSILON = .02f;

    Vector3 _monitorZoomEndPos;
    bool _lockToMonitorOrigin;

    bool _isZooming;
    bool _onMonitor;
    bool _flickerFinished;

    Coroutine _transitionRoutine;
    int _activeFlickers;

    public bool IsDesktopFXDone => _flickerFinished;
    public bool IsDesktopReady => _flickerFinished && !_isZooming && _onMonitor;
    bool IsCameraAtFollowZ => Mathf.Abs(transform.position.z - fixedZ) <= CAM_Z_EPSILON;

    void Start(){
        fixedY = transform.position.y;
        fixedZ = transform.position.z;
        _targetFOV = vcam.m_Lens.FieldOfView;

        if(desktopScreen == null) return;
        if(desktopTargetRenderer != null){
            Texture2D savedScreenshot = Resources.Load<Texture2D>("2. Art/Desktop/" + System.IO.Path.GetFileNameWithoutExtension(screenshotFileName));
            
            if(savedScreenshot != null) desktopTargetRenderer.material.SetTexture("_MainTex", savedScreenshot);
            else desktopTargetRenderer.material.SetTexture("_MainTex", null);
        }

        desktopScreen.SetActive(false);

        if(desktopVisual != null){
            Color c = desktopActiveColor;
            c.a = 0f;
            desktopVisual.Color = c;
        }

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(false);
    }

    void LateUpdate(){
        if(_lockToMonitorOrigin){
            transform.position = new Vector3(0f, 0f, -.1f);
            return;
        }
        if(_isZooming){
            cc.SetMovementEnabled(false);
            return;
        }
        if(!_onMonitor && IsAtDefaultPosition()) cc.SetMovementEnabled(true);
        if(desktopScreen.activeSelf) return;

        HandleMouseX();
        HandleCamPos();
        HandleZoom();

        if(desktopScreen != null && desktopScreen.activeSelf){
            if(Time.unscaledDeltaTime > 0.02f) Debug.LogWarning($"[Performance Spike] Frame time: {Time.unscaledDeltaTime * 1000:F1}ms while desktop is active");
        }
    }

    #region CAMERA FOLLOW
    void HandleMouseX(){
        Vector3 mouseView = worldCam.ScreenToViewportPoint(Input.mousePosition);
        float centeredX = Mathf.Clamp((mouseView.x - .5f) * 2f, -1f, 1f);

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
        if(Mathf.Abs(scroll) > .0001f)
            _targetFOV = Mathf.Clamp(_targetFOV - scroll * zoomSpeed, minFOV, maxFOV);

        var lens = vcam.m_Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, _targetFOV, fovSmoothSpeed * Time.deltaTime);
        vcam.m_Lens = lens;
    }

    bool IsAtDefaultPosition(){
        Vector3 target = new Vector3(
            player.position.x + _currentXOffset,
            fixedY,
            fixedZ
        );

        return Vector3.SqrMagnitude(transform.position - target) < .0004f;
    }
    #endregion

    #region DESKTOP TRANSITION
    public void StartTransition(bool toMonitor){
        if(rezzitScreen.activeSelf) return;
        if(toMonitor && (_isZooming || !IsCameraAtFollowZ)) return;
        if(!toMonitor && !IsDesktopReady) return;
        if(_onMonitor == toMonitor) return;

        if(_transitionRoutine != null) StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(toMonitor ? ZoomToMonitor() : ReturnFromMonitor());
    }

    IEnumerator ZoomToMonitor(){
        _isZooming = true;
        _onMonitor = true;
        _flickerFinished = false;
        _lockToMonitorOrigin = false;

        cc.SetMovementEnabled(false);

        Vector3 startPos = transform.position;
        Vector3 endPos = monitorTarget.position + Vector3.back;

        float startFOV = vcam.m_Lens.FieldOfView;
        float endFOV = 15f;

        yield return ZoomRoutine(startPos, endPos, startFOV, endFOV);

        _monitorZoomEndPos = endPos;
        desktopScreen.SetActive(true);
        //Debug.Log($"[Desktop] Screen ENABLED at {Time.time:F2}s");
        UpdatePostProcessingPriority();

        audioSource.PlayOneShot(chimeSFX);
        AudioManager.Instance.EnableChannel(AudioChannel.Music, false);
        AudioManager.Instance.EnableChannel(AudioChannel.Ambience, false);
        cc.CancelMonologue();

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(true);
        yield return FadeDesktop(true);

        _lockToMonitorOrigin = true;
        _transitionRoutine = null;
        _isZooming = false;

        //Debug.Log($"[Desktop] Fully READY at {Time.time:F2}s");
    }

    IEnumerator ReturnFromMonitor(){
        _isZooming = true;
        _onMonitor = false;
        _flickerFinished = false;
        _lockToMonitorOrigin = false;
        CaptureCurrentView();

        yield return FadeDesktop(false);

        desktopScreen.SetActive(false);
        //Debug.Log($"[Desktop] Screen DISABLED at {Time.time:F2}s");
        UpdatePostProcessingPriority();

        Vector3 startPos = _monitorZoomEndPos;
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

    public void TransistionImmediately(){
        if(_transitionRoutine != null) StopCoroutine(_transitionRoutine);

        _onMonitor = true;

        Vector3 endPos = monitorTarget.position + Vector3.back;
        float endFOV = 15f;

        transform.position = endPos;
        _monitorZoomEndPos = monitorTarget.position + Vector3.back;
        _lockToMonitorOrigin = true;

        transform.position = new Vector3(0f, 0f, .1f);

        var lens = vcam.m_Lens;
        lens.FieldOfView = endFOV;
        vcam.m_Lens = lens;

        desktopScreen.SetActive(true);
        Debug.Log($"[Desktop] Screen ENABLED (immediate) at {Time.time:F2}s");
        UpdatePostProcessingPriority();

        AudioManager.Instance.EnableChannel(AudioChannel.Music, false);
        AudioManager.Instance.EnableChannel(AudioChannel.Ambience, false);
        cc.CancelMonologue();

        foreach(Transform child in desktopScreen.transform) child.gameObject.SetActive(true);

        Color end = desktopActiveColor;
        end.a = 1f;
        desktopVisual.Color = end;

        if(crtOverlay) crtOverlay.alpha = 1f;

        _transitionRoutine = null;
        _isZooming = false;
        _flickerFinished = true;

        Debug.Log($"[Desktop] Fully READY (immediate) at {Time.time:F2}s");
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
            if(disableDesktopFlicker){
                UIBlock2D[] blocks = child.GetComponentsInChildren<UIBlock2D>(true);
                TextBlock[] texts = child.GetComponentsInChildren<TextBlock>(true);

                foreach(var b in blocks){
                    Color c = b.Color;
                    c.a = desktopActiveColor.a;
                    b.Color = c;

                    Color s = b.Shadow.Color;
                    s.a = c.a;
                    b.Shadow.Color = s;
                }

                foreach(var txt in texts){
                    Color c = txt.Color;
                    c.a = 1f;
                    txt.Color = c;
                }

                continue;
            }

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

        UIBlock2D[] allBlocks = child.GetComponentsInChildren<UIBlock2D>(true);
        TextBlock[] allTexts = child.GetComponentsInChildren<TextBlock>(true);

        List<UIBlock2D> blocks = new List<UIBlock2D>();
        List<TextBlock> texts = new List<TextBlock>();

        List<float> blockBaseAlpha = new List<float>();
        List<float> textBaseAlpha  = new List<float>();

        foreach(var b in allBlocks){
            if(b.Color.a <= 0f) continue;
            blocks.Add(b);
            blockBaseAlpha.Add(b.Color.a);
        }

        foreach(var t in allTexts){
            if(t.Color.a <= 0f) continue;
            texts.Add(t);
            textBaseAlpha.Add(t.Color.a);
        }

        if(blocks.Count == 0 && texts.Count == 0) yield break;

        float total = Random.Range(.8f, 1.4f);
        float elapsed = 0f;

        while(elapsed < total){
            float target = Random.value > .5f ? 1f : 0f;
            float speed = Random.Range(.05f, .15f);
            yield return FadeElements(
                blocks.ToArray(),
                texts.ToArray(),
                target,
                speed
            );
            elapsed += speed;
        }

        yield return FadeElementsRestore(
            blocks.ToArray(),
            texts.ToArray(),
            blockBaseAlpha.ToArray(),
            textBaseAlpha.ToArray(),
            .2f
        );
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

    IEnumerator FadeElementsRestore(UIBlock2D[] blocks, TextBlock[] texts, float[] blockAlpha, float[] textAlpha, float duration){
        float t = 0f;

        while(t < 1f){
            t += Time.deltaTime / duration;

            for(int i = 0; i < blocks.Length; i++){
                Color c = blocks[i].Color;
                Color s = blocks[i].Shadow.Color;
                c.a = Mathf.Lerp(c.a, blockAlpha[i], t);
                s.a = c.a;
                blocks[i].Color = c;
                blocks[i].Shadow.Color = s;
            }

            for(int i = 0; i < texts.Length; i++){
                Color c = texts[i].Color;
                c.a = Mathf.Lerp(c.a, textAlpha[i], t);
                texts[i].Color = c;
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