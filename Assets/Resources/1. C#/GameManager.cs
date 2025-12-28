using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Assertions;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [Header("DOF")]
    public float normalFocus = 5f;
    public float normalAperture = 8f;
    [Space(5)]
    public float blurFocus = 2f;
    public float blurAperture = 2.8f;
    public float dofSmooth = 12f;
    [HideInInspector] public DepthOfField _dof;
    [HideInInspector] public bool disableBlur;

    [Header("KEYCODE")]
    public KeyCode pauseKey = KeyCode.Escape;
    public KeyCode interactKey = KeyCode.F;

    [Header("REFERENCES")]
    public Transform desktopCanvas;
    public DesktopManager dm;
    public Volume pp;
    private PauseManager pauseManager;

    public int currRound = 1;
    [HideInInspector] public bool isPaused;
    [HideInInspector] public bool isAnyUiActive;

    //Singleton
    public static GameManager Instance { get; private set; }
    void Awake(){
        // if(Instance != null){
        //     Destroy(gameObject);
        //     return;
        // }
        Instance = this;

        if(pp != null) pp.profile.TryGet<DepthOfField>(out _dof);
    }

    void Update(){
        HandlePause();
        UpdateDOF();

        #if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Period)) EditorApplication.isPaused = !EditorApplication.isPaused;
        #endif
    }

    void HandlePause(){
        // Try find the pause manager if empty
        if(pauseManager == null)
        {
            pauseManager = PauseManager.Instance;
            Assert.IsNotNull(pauseManager, "Pause manager in this scene has no local instance");
        }
        // Check if pause enabled
        if(!pauseManager.isPauseEnabled) return;
        // Check for pause input
        if(Input.GetKeyDown(KeyCode.Escape)){
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            if(isPaused) pauseManager.OpenPauseMenu();
            else pauseManager.ClosePauseMenu();
        }
    }

    void UpdateDOF(){
        if(_dof == null || disableBlur) return;

        bool shouldBlur  = (isPaused || disableBlur);
        float targetDist = (shouldBlur) ? blurFocus : normalFocus;
        float targetApt  = (shouldBlur) ? blurAperture : normalAperture;

        _dof.focusDistance.value = Mathf.Lerp(_dof.focusDistance.value, targetDist, Time.unscaledDeltaTime * dofSmooth);
        _dof.aperture.value = Mathf.Lerp(_dof.aperture.value, targetApt, Time.unscaledDeltaTime * dofSmooth);
    }
}
