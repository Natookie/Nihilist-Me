using Nova;
using UnityEngine;

[System.Serializable]
public abstract class Setting{
    public string appName;
    public Sprite appIcon;
    public GameObject appPrefab;
} 

[System.Serializable]
public class DesktopIconSetting : Setting{
    public DesktopIconSetting(string name, Sprite icon, GameObject prefab){
        appName = name;
        appIcon = icon;
        appPrefab = prefab;
    }
}
