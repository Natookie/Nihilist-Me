using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DesktopDragSettings : MonoBehaviour
{
    public static DesktopDragSettings Instance { get; private set; }

    [Header("DRAG SETTINGS")]
    public Canvas DragCanvas;
    public float tiltSmooth = 25f;
    public float tiltStrength = 1f;
    public float dragOpacity = .45f;
    public float swapRadius = 150f;
    
    [Header("ICON VISUALS")]
    public Vector2 iconSize = new Vector2(120, 120);
    public Vector2 iconImageSize = new Vector2(56, 56);
    
    [Header("LABEL SETTINGS")]
    public TMP_FontAsset labelFont;
    public int labelFontSize = 14;
    public Color labelColor = Color.white;
    public Vector2 labelSize = new Vector2(140, 30);
    public Vector2 labelOffset = new Vector2(0f, -33f);
    
    [Header("ANIMATION SETTINGS")]
    public float minSlideDuration = 0.2f;
    public float maxSlideDuration = 0.35f;
    public float distanceDivisor = 1000f;
    public bool animateFullReturn = true;
    
    [Header("ICON SLOTS")]
    public List<Transform> iconSlots = new List<Transform>();

    void Awake(){
        if(Instance != null && Instance != this){
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}