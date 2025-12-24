using UnityEngine;
using Nova;

[System.Serializable]
public abstract class SettingMusic{
    public string musicName;
    public string musicCreator;
    public AudioClip musicClip;
    public Sprite musicIcon;
}

[System.Serializable]
public class MusicSetting : SettingMusic{
    public MusicSetting(string name, string creator, AudioClip clip, Sprite icon){
        musicName = name;
        musicCreator = creator;
        musicClip = clip;
        musicIcon = icon;
    }
}
