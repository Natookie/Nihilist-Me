using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    //AUDIO SETTINGS
    public bool musicMuted = false;
    public bool ambienceMuted = false;
    public bool sfxMuted = false;

    public float musicVolume = 1f;
    public float ambienceVolume = 1f;
    public float sfxVolume = 1f;
    
    //GRAPHICS SETTINGS
    public int resolutionIndex = 0;
    public int qualityLevel = 2; // Medium
    public bool fullscreen = true;
    
    //GAME STATE
    public Vector3 playerPosition = Vector3.zero;
    public Quaternion playerRotation = Quaternion.identity;
    public int currentRound = 1;
    
    //PROGRESS
    public Dictionary<string, bool> completedEndings = new Dictionary<string, bool>();
    
    public GameData(){
        playerPosition = new Vector3(0, 1, 0);
    }
}