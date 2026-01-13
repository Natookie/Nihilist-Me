using UnityEngine;
using Nova;
using System.Collections.Generic;

public class SettingPanel : MonoBehaviour
{
    [Header("UI CONFIG")]
    [SerializeField] private SettingBlock[] settingSelection;
    [SerializeField] private UIBlock2D settingSelector;
    
    [Header("SETTINGS")]
    [SerializeField] private float selectorMoveSpeed = 15f;
    [SerializeField] private float selectorSizeSpeed = 15f;
    [SerializeField] private Color selectorDefaultColor = new Color(.8f, .85f, 1f, 1f);
    [Space(10)]
    [SerializeField] private Color itemDefaultColor = Color.white;
    [SerializeField] private Color itemHoverColor = new Color(.9f, .9f, 1f, 1f);
    [SerializeField] private Color itemSelectedColor = new Color(.8f, .85f, 1f, 1f);
    [SerializeField] private float popScale = 1.15f;
    [SerializeField] private float popDuration = .15f;
    
    private const float PADDING = 17f;
    private int currentSelectedIndex = 0;
    private int previousSelectedIndex = -1;
    private Vector3 selectorTargetPosition;
    private Vector3 selectorTargetSize;
    private bool isInitialized = false;
    private int hoveredIndex = -1;
    private float popTimer = 0f;
    private int popIndex = -1;
    
    void Start(){
        InitializeSettings();
    }
    
    void Update(){
        if(!isInitialized) return;
        
        UpdateSelectorMovement();
        UpdateSelectorSize();
        UpdateItemColors();
        UpdatePopEffect();
    }
    
    void InitializeSettings(){
        if(settingSelection == null || settingSelection.Length == 0) return;
        if(settingSelector == null) return;
        
        for(int i = 0; i < settingSelection.Length; i++){
            int index = i;
            SettingBlock block = settingSelection[i];
            
            if(block.item != null){
                block.item.AddGestureHandler<Gesture.OnHover>(evt => OnSettingHover(index));
                block.item.AddGestureHandler<Gesture.OnUnhover>(evt => OnSettingUnhover(index));
                block.item.AddGestureHandler<Gesture.OnClick>(evt => OnSettingClick(index));
                
                block.item.Color = (i == 0) ? itemSelectedColor : itemDefaultColor;
                block.item.transform.localScale = Vector3.one;
            }
            
            if(block.panel != null) block.panel.gameObject.SetActive(i == 0);
        }
        
        SelectSetting(0);
        isInitialized = true;
    }
    
    void OnSettingHover(int index){
        if(index < 0 || index >= settingSelection.Length) return;
        
        hoveredIndex = index;
        
        if(settingSelection[index].item != null && index != currentSelectedIndex){
            settingSelection[index].item.transform.localScale = Vector3.one * 1.05f;
        }
    }
    
    void OnSettingUnhover(int index){
        if(index < 0 || index >= settingSelection.Length) return;
        
        hoveredIndex = -1;
        
        if(settingSelection[index].item != null && index != currentSelectedIndex){
            settingSelection[index].item.transform.localScale = Vector3.one;
        }
    }
    
    void OnSettingClick(int index){
        if(index < 0 || index >= settingSelection.Length) return;
        
        if(index == currentSelectedIndex) return; // Don't pop if already selected
        
        // Store previous selection
        previousSelectedIndex = currentSelectedIndex;
        
        // Start pop effect first
        popIndex = index;
        popTimer = popDuration;
        
        // Select after pop
        SelectSetting(index);
    }
    
    void SelectSetting(int index){
        if(index < 0 || index >= settingSelection.Length) return;
        
        currentSelectedIndex = index;
        
        if(settingSelection[index].item != null){
            float targetX = PADDING + settingSelection[index].item.Position.X.Value;
            float targetSizeX = settingSelection[index].item.Size.X.Value;
            
            selectorTargetPosition = new Vector3(targetX, settingSelector.Position.Y.Value, 0f);
            selectorTargetSize = new Vector3(targetSizeX, settingSelector.Size.Y.Value, 1f);
        }
        
        for(int i = 0; i < settingSelection.Length; i++){
            if(settingSelection[i].panel != null) settingSelection[i].panel.gameObject.SetActive(i == index);
        }
    }
    
    void UpdateSelectorMovement(){
        if(settingSelector == null) return;
        
        Vector3 currentPos = settingSelector.Position.Value;
        Vector3 newPos = Vector3.Lerp(currentPos, selectorTargetPosition, Time.unscaledDeltaTime * selectorMoveSpeed);
        settingSelector.Position.Value = newPos;
    }
    
    void UpdateSelectorSize(){
        if(settingSelector == null) return;
        
        Vector3 currentSize = settingSelector.Size.Value;
        Vector3 newSize = Vector3.Lerp(currentSize, selectorTargetSize, Time.unscaledDeltaTime * selectorSizeSpeed);
        settingSelector.Size.Value = newSize;
    }
    
    void UpdateItemColors(){
        for(int i = 0; i < settingSelection.Length; i++){
            if(settingSelection[i].item != null){
                // Don't change color during pop - wait until pop is done
                if(i == popIndex && popTimer > 0f) continue;
                
                Color targetColor = itemDefaultColor;
                if(i == currentSelectedIndex) targetColor = itemSelectedColor;
                else if(i == hoveredIndex) targetColor = itemHoverColor;
                
                Color currentColor = settingSelection[i].item.Color;
                Color newColor = Color.Lerp(currentColor, targetColor, Time.unscaledDeltaTime * selectorMoveSpeed);
                settingSelection[i].item.Color = newColor;
            }
        }
    }
    
    void UpdatePopEffect(){
        if(popTimer > 0f && popIndex >= 0 && popIndex < settingSelection.Length){
            if(settingSelection[popIndex].item != null){
                popTimer -= Time.unscaledDeltaTime;
                float progress = 1f - (popTimer / popDuration);
                
                // First half: expand to popScale
                // Second half: shrink back to normal
                float scale = 1f;
                if(progress < .5f){
                    float expandProgress = progress / .5f;
                    scale = Mathf.Lerp(1f, popScale, expandProgress);
                }
                else{
                    float shrinkProgress = (progress - .5f) / .5f;
                    scale = Mathf.Lerp(popScale, 1f, shrinkProgress);
                    
                    // Change color at the peak (middle of animation)
                    if(progress >= .5f && progress < .55f){
                        // Update previous item back to default color
                        if(previousSelectedIndex >= 0 && previousSelectedIndex < settingSelection.Length){
                            if(settingSelection[previousSelectedIndex].item != null){
                                settingSelection[previousSelectedIndex].item.Color = itemDefaultColor;
                            }
                        }
                        // Set new item to selected color
                        settingSelection[popIndex].item.Color = itemSelectedColor;
                    }
                }
                
                settingSelection[popIndex].item.transform.localScale = Vector3.one * scale;
            }
            
            if(popTimer <= 0f){
                popTimer = 0f;
                if(settingSelection[popIndex].item != null){
                    settingSelection[popIndex].item.transform.localScale = Vector3.one;
                    settingSelection[popIndex].item.Color = itemSelectedColor;
                }
                popIndex = -1;
                previousSelectedIndex = -1;
            }
        }
    }
    
    public void SelectGraphic(){
        if(0 == currentSelectedIndex) return;
        previousSelectedIndex = currentSelectedIndex;
        popIndex = 0;
        popTimer = popDuration;
        SelectSetting(0);
    }
    
    public void SelectAudio(){
        if(1 == currentSelectedIndex) return;
        previousSelectedIndex = currentSelectedIndex;
        popIndex = 1;
        popTimer = popDuration;
        SelectSetting(1);
    }
    
    public void SelectGeneral(){
        if(2 == currentSelectedIndex) return;
        previousSelectedIndex = currentSelectedIndex;
        popIndex = 2;
        popTimer = popDuration;
        SelectSetting(2);
    }
}

[System.Serializable]
public class SettingBlock
{
    public TextBlock item;
    public UIBlock2D panel;
}