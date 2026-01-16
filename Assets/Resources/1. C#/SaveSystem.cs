using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class SaveSystem
{
    private static string savePath = Application.persistentDataPath + "/save.dat";
    private static string settingsPath = Application.persistentDataPath + "/settings.dat";
    
    public static GameData CurrentGameData { get; private set; } = new GameData();
    public static GameData CurrentSettings { get; private set; } = new GameData();
    
    //Load all data on game start
    public static void Initialize(){
        LoadSettings();
        LoadGameData();
    }
    
    //Save game progress
    public static void SaveGameData(){
        if(CurrentGameData == null) CurrentGameData = new GameData();
        
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath, FileMode.Create);
        
        formatter.Serialize(stream, CurrentGameData);
        stream.Close();
    }
    
    //Save settings
    public static void SaveSettings(){
        if(CurrentSettings == null) CurrentSettings = new GameData();
        
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(settingsPath, FileMode.Create);
        
        formatter.Serialize(stream, CurrentSettings);
        stream.Close();
    }
    
    //Load game progress
    public static GameData LoadGameData(){
        if(File.Exists(savePath)){
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(savePath, FileMode.Open);
            
            CurrentGameData = formatter.Deserialize(stream) as GameData;
            stream.Close();
            return CurrentGameData;
        }
        else{
            CurrentGameData = new GameData();
            SaveGameData();
            return CurrentGameData;
        }
    }
    
    //Load settings
    public static GameData LoadSettings(){
        if(File.Exists(settingsPath)){
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(settingsPath, FileMode.Open);
            
            CurrentSettings = formatter.Deserialize(stream) as GameData;
            stream.Close();
            return CurrentSettings;
        }
        else{
            CurrentSettings = new GameData();
            SaveSettings();
            return CurrentSettings;
        }
    }
    
    //Quick save audio settings (for PauseManager/AudioPanel)
    public static void SaveAudioSettings(bool musicMuted, bool sfxMuted, bool ambienceMuted,
                                         float musicVol, float sfxVol, float ambienceVol){
        CurrentSettings.musicMuted = musicMuted;
        CurrentSettings.sfxMuted = sfxMuted;
        CurrentSettings.ambienceMuted = ambienceMuted;
        CurrentSettings.musicVolume = musicVol;
        CurrentSettings.sfxVolume = sfxVol;
        CurrentSettings.ambienceVolume = ambienceVol;
        
        SaveSettings();
    }
    
    //Quick save graphics settings (for GraphicPanel)
    public static void SaveGraphicsSettings(int resolution, int quality, bool fullscreen){
        CurrentSettings.resolutionIndex = resolution;
        CurrentSettings.qualityLevel = quality;
        CurrentSettings.fullscreen = fullscreen;
        
        SaveSettings();
    }
    
    //Quick save player position (for GameManager)
    public static void SavePlayerPosition(Vector3 position, Quaternion rotation){
        CurrentGameData.playerPosition = position;
        CurrentGameData.playerRotation = rotation;
        
        SaveGameData();
    }
    
    //Check if save exists
    public static bool SaveExists(){
        return File.Exists(savePath);
    }
    
    //Delete save (for "New Game")
    public static void DeleteSave(){
        if(File.Exists(savePath)){
            File.Delete(savePath);
            CurrentGameData = new GameData();
        }
    }
}