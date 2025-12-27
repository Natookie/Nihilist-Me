using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DesktopManager : MonoBehaviour
{
    public static DesktopManager Instance { get; private set; }

    [Header("TASKBAR")]
    public Transform taskbarArea;
    public GameObject taskButtonPrefab;

    private List<GameObject> activeApps = new();

    [Header("WIFI")]
    public int signalStrength = 1;
    public Sprite[] wifiIcons;
    private float wifiTimer = 0f;
    private float wifiInterval;

    [Header("AUDIO")]
    public int volumeLevel = 5;
    public Sprite[] speakerIcons;

    [Header("TIME AND DATE")]
    public TextMeshProUGUI clockText;

    [Header("REFERENCES")]
    public Image wifiIconRenderer;
    public Image speakerIconRenderer;

    [Header("DESKTOP ICONS")]
    public Canvas DragCanvas;
    public float tiltSmooth = 25f;
    public float tiltStrength = 1f;
    public float stopThreshold = 1.5f;
    public float dragOpacity = .45f;
    public float swapRadius = 150f;
    [Space(10)]
    public Vector2 iconSize = new Vector2(120, 120);
    public Vector2 iconImageSize = new Vector2(56, 56);
    [Space(10)]
    public TMP_FontAsset labelFont;
    public int labelFontSize = 14;
    public Color labelColor = Color.white;
    public Vector2 labelSize = new Vector2(140, 30);
    public Vector2 labelOffset = new Vector2(0f, -33f);
    [Space(10)]
    public float minSlideDuration = 0.2f;
    public float maxSlideDuration = 0.35f;
    public float distanceDivisor = 1000f;
    public bool animateFullReturn = true;
    [Space(10)]
    public List<Transform> iconSlots = new List<Transform>();

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update(){
        /*
        Not used anymore:
        HandleWifi();
        HandleAudio();
        HandleClock();
        */
    }

    void HandleWifi(){
        wifiTimer += Time.deltaTime;
        if(wifiTimer >= wifiInterval){
            signalStrength += Random.Range(-1, 2);
            signalStrength = Mathf.Clamp(signalStrength, 0, wifiIcons.Length - 1);
            wifiIconRenderer.sprite = wifiIcons[signalStrength];

            wifiTimer = 0f;
            wifiInterval = Random.Range(1f, 15f);
        }
    }

    void HandleAudio(){
        if(volumeLevel <= 0) speakerIconRenderer.sprite = speakerIcons[0];
        else if(volumeLevel <= 25) speakerIconRenderer.sprite = speakerIcons[1];
        else if(volumeLevel <= 75) speakerIconRenderer.sprite = speakerIcons[2];
        else speakerIconRenderer.sprite = speakerIcons[3];
    }

    public void AddTask(DesktopIcon icon, GameObject appInstance){
        GameObject button = Instantiate(taskButtonPrefab, taskbarArea);
        button.GetComponent<Image>().sprite = icon.appIcon;

        button.GetComponent<Button>().onClick.AddListener(() => {
            appInstance.transform.SetAsLastSibling();
        });

        activeApps.Add(appInstance);
    }

    void HandleClock(){
        clockText.text = " > " + System.DateTime.Now.ToString("hh:mm tt");
    }
    
}
