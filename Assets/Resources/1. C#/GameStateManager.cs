// using System.Collections;
// using UnityEngine;
// using UnityEngine.SceneManagement;

// public class GameStateManager : MonoBehaviour
// {
//     public static GameStateManager Instance { get; private set; }
    
//     [Header("REFERENCES")]
//     public Transform playerTransform;
//     public AudioManager audioManager;
//     public PauseManager pauseManager;
    
//     void Awake(){
//         if(Instance != null && Instance != this){
//             Destroy(this.gameObject);
//             return;
//         }
        
//         Instance = this;
//         DontDestroyOnLoad(this.gameObject);
        
//         //Load saved data
//         SaveSystem.LoadSettings();
//         SaveSystem.LoadGameData();
        
//         //Apply loaded settings
//         ApplySettings();
        
//         SceneManager.sceneLoaded += OnSceneLoaded;
//     }
    
//     void OnDestroy(){
//         SceneManager.sceneLoaded -= OnSceneLoaded;
//     }
    
//     void OnSceneLoaded(Scene scene, LoadSceneMode mode){
//         //Find player in new scene
//         if(playerTransform == null){
//             GameObject player = GameObject.FindGameObjectWithTag("Player");
//             if(player != null) playerTransform = player.transform;
//         }
        
//         //Apply saved position if in gameplay scene
//         if(scene.name == "Main Scene" && playerTransform != null){
//             LoadPlayerPosition();
//         }
        
//         //Update references
//         if(audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();
//         if(pauseManager == null) pauseManager = FindFirstObjectByType<PauseManager>();
//     }
    
//     void ApplySettings(){
//         //Apply audio settings
//         if(audioManager != null){
//             audioManager.SetMusicVolume(SaveSystem.CurrentSettings.musicVolume);
//             audioManager.SetSFXVolume(SaveSystem.CurrentSettings.sfxVolume);
//             audioManager.SetMusicMuted(SaveSystem.CurrentSettings.musicMuted);
//             audioManager.SetSFXMuted(SaveSystem.CurrentSettings.sfxMuted);
//         }
        
//         //Apply graphics settings
//         ApplyGraphicsSettings();
        
//         //Apply game state
//         if(GameManager.Instance != null){
//             GameManager.Instance.currRound = SaveSystem.CurrentGameData.currentRound;
//         }
//     }
    
//     void ApplyGraphicsSettings(){
//         //Apply resolution
//         Resolution[] resolutions = Screen.resolutions;
//         if(SaveSystem.CurrentSettings.resolutionIndex >= 0 && 
//            SaveSystem.CurrentSettings.resolutionIndex < resolutions.Length){
//             Resolution res = resolutions[SaveSystem.CurrentSettings.resolutionIndex];
//             Screen.SetResolution(res.width, res.height, SaveSystem.CurrentSettings.fullscreen);
//         }
        
//         //Apply quality
//         QualitySettings.SetQualityLevel(SaveSystem.CurrentSettings.qualityLevel);
        
//         //Apply fullscreen
//         Screen.fullScreen = SaveSystem.CurrentSettings.fullscreen;
//     }
    
//     void LoadPlayerPosition(){
//         if(playerTransform != null){
//             playerTransform.position = SaveSystem.CurrentGameData.playerPosition;
//             playerTransform.rotation = SaveSystem.CurrentGameData.playerRotation;
//         }
//     }
    
    
//     public void SaveAudioSettings(bool musicMuted, bool ambienceMuted, bool sfxMuted,
//                                   float musicVol, float ambienceVol, float sfxVol){
//         SaveSystem.QuickSaveAudio(musicMuted, ambienceMuted, sfxMuted, 
//                                   musicVol, ambienceVol, sfxVol);
//     }
    
//     public void SaveGraphicsSettings(int resolution, int quality, bool fullscreen,
//                                      float brightness, float contrast){
//         SaveSystem.QuickSaveGraphics(resolution, quality, fullscreen, brightness, contrast);
//     }
    
//     public void SavePlayerPosition(){
//         if(playerTransform != null){
//             SaveSystem.QuickSavePlayerPosition(playerTransform.position, playerTransform.rotation);
//         }
//     }
    
//     public void SaveGameProgress(int round, int health, int score){
//         SaveSystem.QuickSaveProgress(round, health, score);
//     }
    
//     public void AutoSave(){
//         if(playerTransform != null){
//             SaveSystem.CurrentGameData.playerPosition = playerTransform.position;
//             SaveSystem.CurrentGameData.playerRotation = playerTransform.rotation;
//         }
        
//         if(GameManager.Instance != null){
//             SaveSystem.CurrentGameData.currentRound = GameManager.Instance.currRound;
//         }
        
//         SaveSystem.SaveGameData();
//         Debug.Log("Auto-save completed.");
//     }
    
//     void Start(){
//         StartCoroutine(AutoSaveRoutine());
//     }
    
//     IEnumerator AutoSaveRoutine(){
//         while(true){
//             yield return new WaitForMinutes(5);
//             AutoSave();
//         }
//     }
// }