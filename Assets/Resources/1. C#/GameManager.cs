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
    public CamFoll cc;
    public Computer computer;
    public Volume pp;

    private PauseManager pauseManager;

    public int currRound = 1;
    public bool isPaused;
    [HideInInspector] public bool isAnyUiActive;

    public static GameManager Instance { get; private set; }
    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

        if(pp != null) pp.profile.TryGet<DepthOfField>(out _dof);
    }

    void Start(){
        ResetReference();
    }

    void Update(){
        HandlePause();

        #if UNITY_EDITOR
        if(Input.GetKeyDown(KeyCode.Period)) EditorApplication.isPaused = !EditorApplication.isPaused;
        #endif
    }

    void HandlePause(){
        if(pauseManager == null || pauseManager.gameObject == null){
            pauseManager = FindFirstObjectByType<PauseManager>();
            if(pauseManager == null) return;
        }

        if(!pauseManager.isPauseEnabled) return;
        
        if(Input.GetKeyDown(pauseKey)){
            if(cc == null) return;
            
            bool isOnDesktop = cc.desktopScreen.activeSelf;
            if(!isOnDesktop){
                isPaused = !isPaused;
                Time.timeScale = isPaused ? 0f : 1f;
                if(isPaused) pauseManager.OpenPauseMenu();
                else pauseManager.ClosePauseMenu();
            }else if(computer != null){
                computer.Interact();
            }
        }
    }

    public void ResetReference(){
        cc = FindFirstObjectByType<CamFoll>();
        computer = FindFirstObjectByType<Computer>();
        pauseManager = null;
        isPaused = false;
    }

    /*Not used anymore
    void UpdateDOF(){
        if(_dof == null || disableBlur) return;

        bool shouldBlur  = (isPaused || disableBlur);
        float targetDist = (shouldBlur) ? blurFocus : normalFocus;
        float targetApt  = (shouldBlur) ? blurAperture : normalAperture;

        _dof.focusDistance.value = Mathf.Lerp(_dof.focusDistance.value, targetDist, Time.unscaledDeltaTime * dofSmooth);
        _dof.aperture.value = Mathf.Lerp(_dof.aperture.value, targetApt, Time.unscaledDeltaTime * dofSmooth);
    }
    */
}