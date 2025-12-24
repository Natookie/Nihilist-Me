using UnityEngine;
using System.Collections.Generic;
using Nova;

public class NewDesktopManager : MonoBehaviour
{
    public static NewDesktopManager Instance;

    [Header("APPLICATIONS (SCENE OBJECTS)")]
    public GameObject files;
    public GameObject corner;
    public GameObject nokion;
    [Space(5)]
    public GameObject rezzit;

    [Header("CLOCK")]
    public TextBlock clockText;

    [Header("REFERENCES")]
    public CamFoll camFoll;

    Dictionary<string, AppGestureHandler> appMap;

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        appMap = new Dictionary<string, AppGestureHandler>{
            { "files", files.GetComponent<AppGestureHandler>() },
            { "corner", corner.GetComponent<AppGestureHandler>() },
            { "nokion", nokion.GetComponent<AppGestureHandler>() }
        };
    }

    void Update(){
        clockText.Text = System.DateTime.Now.ToString("HH:mm");
    }

    public void OpenApp(string appName){
        if(!appMap.TryGetValue(appName, out var app)){
            if(appName == "rezzit") HandleRezzit();
            return;
        }

        app.Open();
    }

    public void CloseApp(string appName){
        if(!appMap.TryGetValue(appName, out var app)) return;
        app.Close();
    }

    void HandleRezzit(){
        camFoll.desktopScreen.SetActive(false);
        rezzit.SetActive(true);
        camFoll.UpdatePostProcessingPriority();
    }
}
