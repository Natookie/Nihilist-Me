using UnityEngine;
using Nova;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ResumePanel : MonoBehaviour
{
    #region INSTAGRAM_POST
    [Header("INSTAGRAM POST")]
    [SerializeField] private UIBlock layerBack;
    [SerializeField] private UIBlock layerMid;
    [SerializeField] private UIBlock layerFront;
    [Space(10)]
    [SerializeField] private float maxTiltX = 6f;
    [SerializeField] private float maxTiltY = 10f;
    [SerializeField] private float maxOffset = 15f;
    [SerializeField] private float effectSpeed = 12f;
    
    [Header("HIGHLIGHT")]
    [SerializeField] private bool enableHighlight = true;
    [SerializeField] private float highlightIntensity = .2f;
    [SerializeField] private Color highlightLightColor = new Color(1.15f, 1.15f, 1.15f, 1f);
    [SerializeField] private Color highlightDarkColor = new Color(.85f, .85f, .85f, 1f);
    
    private Vector3 backDefaultPos, midDefaultPos, frontDefaultPos;
    private Vector3 backDefaultRot, midDefaultRot, frontDefaultRot;
    private Color frontDefaultColor;
    private bool isHovered;
    private Camera uiCamera;
    private RectTransform frontRectTransform;
    #endregion

    #region MESSAGE_CONTAINER
    [Header("CHAT MESSAGES")]
    [SerializeField] private UIBlock2D[] msgList;
    [SerializeField] private float moveSpeed = 300f;
    [SerializeField] private float fadeEnterSpeed = 2f;
    [SerializeField] private float fadeExitSpeed = 5f;
    [SerializeField] private float messageDelay = 1.5f;
    [SerializeField] private float SCALE_MIN = .95f;
    [SerializeField] private float ALPHA_MIN = .25f;
    [SerializeField] private List<string> dialogue;
    
    private const int MAX_MESSAGES = 5;
    private const int VISIBLE_MESSAGES = 4;
    private const float TOP_CLIP_MASK = -160f;
    private const float EXIT_OFFSET_Y = 250f;
    private const float FINAL_EXIT_Y = 1000f;

    private Queue<string> messageQueue = new Queue<string>();
    private float nextMessageTime;
    private int messageIndex;
    
    private float[] originalYPositions = new float[MAX_MESSAGES];
    private float[] targetYPositions = new float[MAX_MESSAGES];
    private float[] currentYPositions = new float[MAX_MESSAGES];
    private float[] alphas = new float[MAX_MESSAGES];
    private float[] scales = new float[MAX_MESSAGES];
    private int[] positionQueue = new int[MAX_MESSAGES];
    private bool[] isActive = new bool[MAX_MESSAGES];
    
    private bool canShowNextMessage = true;
    
    [Header("MESSAGE STYLING")]
    [SerializeField] private Color leftBubbleColor = new Color(.9f, .9f, 1f, 1f);
    [SerializeField] private Color rightBubbleColor = new Color(.2f, .5f, 1f, 1f);
    [SerializeField] private Color leftTextColor = Color.black;
    [SerializeField] private Color rightTextColor = Color.white;
    #endregion
    
    void Awake(){
        if(layerBack){
            backDefaultPos = layerBack.transform.localPosition;
            backDefaultRot = layerBack.transform.localEulerAngles;
        }
        if(layerMid){
            midDefaultPos = layerMid.transform.localPosition;
            midDefaultRot = layerMid.transform.localEulerAngles;
        }
        if(layerFront){
            frontDefaultPos = layerFront.transform.localPosition;
            frontDefaultRot = layerFront.transform.localEulerAngles;
            frontDefaultColor = layerFront.Color;
            frontRectTransform = layerFront.GetComponent<RectTransform>();
        }
        
        uiCamera = Camera.main;
        UIBlock parentBlock = GetComponent<UIBlock>();
        if(parentBlock){
            parentBlock.AddGestureHandler<Gesture.OnHover>(evt => isHovered = true);
            parentBlock.AddGestureHandler<Gesture.OnUnhover>(evt => isHovered = false);
        }
        
        InitializeChatSystem();
    }
    
    void Start(){
        nextMessageTime = Time.unscaledTime + 1f;
    }
    
    void Update(){
        Handle3DEffect();
        UpdateMessageVisuals();
        
        if(canShowNextMessage) HandleChatMessages();
    }

    #region MESSAGE SYSTEM
    void InitializeChatSystem(){
        if(msgList == null || msgList.Length != MAX_MESSAGES) return;
        
        for(int i = 0; i < MAX_MESSAGES; i++){
            if(msgList[i] != null){
                originalYPositions[i] = msgList[i].Position.Y.Value;
                
                targetYPositions[i] = TOP_CLIP_MASK;
                currentYPositions[i] = TOP_CLIP_MASK;
                alphas[i] = 0f;
                scales[i] = ALPHA_MIN;
                positionQueue[i] = -1;
                isActive[i] = false;
                
                msgList[i].transform.localScale = new Vector3(1f, scales[i], 1f);
                UpdateSingleMessageVisual(i);
            }
        }
        
        messageQueue.Clear();
        foreach(var line in dialogue) messageQueue.Enqueue(line);
    }
    
    void HandleChatMessages(){
        if(messageQueue.Count == 0) return;
        
        if(Time.unscaledTime >= nextMessageTime){
            bool allSettled = true;
            for(int i = 0; i < MAX_MESSAGES; i++){
                if(isActive[i] && Mathf.Abs(currentYPositions[i] - targetYPositions[i]) > 1f){
                    allSettled = false;
                    break;
                }
            }
            
            if(allSettled){
                ShowNextMessage();
                nextMessageTime = Time.unscaledTime + Random.Range(3f, 6f);
                canShowNextMessage = false;
            }
        }
    }
    
    void ShowNextMessage(){
        if(messageQueue.Count == 0) return;
        
        string message = messageQueue.Dequeue();
        messageQueue.Enqueue(message);
        
        int uiIndex = messageIndex % MAX_MESSAGES;
        bool isLeft = (messageIndex % 2 == 0);
        
        if(msgList[uiIndex] != null){
            TextBlock textBlock = null;
            if(msgList[uiIndex].transform.childCount > 0) textBlock = msgList[uiIndex].transform.GetChild(0).GetComponent<TextBlock>();
            
            if(textBlock != null){
                textBlock.Text = message;
                textBlock.Color = isLeft ? leftTextColor : rightTextColor;
            }
            
            msgList[uiIndex].Color = isLeft ? leftBubbleColor : rightBubbleColor;
            msgList[uiIndex].Alignment = isLeft ? Alignment.TopLeft : Alignment.TopRight;
            
            scales[uiIndex] = ALPHA_MIN;
            msgList[uiIndex].transform.localScale = new Vector3(1f, scales[uiIndex], 1f);
        }
        
        if(messageIndex == 0){
            targetYPositions[uiIndex] = originalYPositions[0];
            currentYPositions[uiIndex] = TOP_CLIP_MASK;
            positionQueue[uiIndex] = 0;
            alphas[uiIndex] = 0f;
            scales[uiIndex] = ALPHA_MIN;
            isActive[uiIndex] = true;
        }else{
            bool chatFull = false;
            for(int i = 0; i < MAX_MESSAGES; i++){
                if(positionQueue[i] == MAX_MESSAGES - 1){
                    chatFull = true;
                    break;
                }
            }
            
            if(chatFull){
                for(int i = 0; i < MAX_MESSAGES; i++){
                    if(positionQueue[i] == MAX_MESSAGES - 1){
                        targetYPositions[i] = originalYPositions[MAX_MESSAGES - 1] + EXIT_OFFSET_Y;
                        positionQueue[i] = -2;
                        break;
                    }
                }
            }
            
            for(int i = 0; i < MAX_MESSAGES; i++){
                if(positionQueue[i] >= 0 && positionQueue[i] < MAX_MESSAGES - 1){
                    positionQueue[i]++;
                    targetYPositions[i] = originalYPositions[positionQueue[i]];
                }
            }
            
            targetYPositions[uiIndex] = originalYPositions[0];
            currentYPositions[uiIndex] = TOP_CLIP_MASK;
            positionQueue[uiIndex] = 0;
            alphas[uiIndex] = 0f;
            scales[uiIndex] = ALPHA_MIN;
            isActive[uiIndex] = true;
        }
        
        messageIndex++;
    }
    
    //HARDCODED sequence shit
    void UpdateMessageVisuals()
    {
        bool anyMoving = false;
        
        //fucking piece of shit
        //dont read
        
        for(int i = 0; i < MAX_MESSAGES; i++){
            if(!isActive[i]) continue;
            
            currentYPositions[i] = Mathf.MoveTowards(
                currentYPositions[i], 
                targetYPositions[i], 
                Time.unscaledDeltaTime * moveSpeed
            );
            
            if(Mathf.Abs(currentYPositions[i] - targetYPositions[i]) > 1f) anyMoving = true;
            
            float targetScale = SCALE_MIN;
            float targetAlpha = 0f;
            float fadeSpeedToUse = fadeExitSpeed;
            
            if(positionQueue[i] >= 0 && positionQueue[i] < VISIBLE_MESSAGES){
                //PHASE 1: First message (A)
                if(messageIndex == 1){//Only A exists
                    //A is at position 0, scale 1.0, alpha 1.0
                    if(positionQueue[i] == 0){
                        targetScale = 1f;
                        targetAlpha = 1f;
                    }
                }
                //PHASE 2: B arrives
                else if(messageIndex == 2){ //A and B exist
                    //B is at position 0 (new), A moves to position 1 (old)
                    //Both should be scale 1.0, alpha 1.0
                    if(positionQueue[i] == 0 || positionQueue[i] == 1){
                        targetScale = 1f;
                        targetAlpha = 1f;
                    }
                }
                //PHASE 3: C arrives
                else if(messageIndex == 3){//A, B, C exist
                    //C at position 0 (newest), B at position 1 (middle), A at position 2 (oldest)
                    if(positionQueue[i] == 0 || positionQueue[i] == 1){
                        //C and B: scale 1.0, alpha 1.0
                        targetScale = 1f;
                        targetAlpha = 1f;
                    }else if(positionQueue[i] == 2){
                        //A: scale .7, alpha .5
                        targetScale = SCALE_MIN;
                        targetAlpha = ALPHA_MIN;
                    }
                }
                //PHASE 4: D arrives - Full pattern achieved
                else if(messageIndex == 4){//A, B, C, D exist
                    //D at position 0, C at 1, B at 2, A at 3
                    if(positionQueue[i] == 0 || positionQueue[i] == 3){
                        //Top and bottom: scale .7, alpha .5
                        targetScale = SCALE_MIN;
                        targetAlpha = ALPHA_MIN;
                    }else if(positionQueue[i] == 1 || positionQueue[i] == 2){
                        //Middle two: scale 1.0, alpha 1.0
                        targetScale = 1f;
                        targetAlpha = 1f;
                    }
                }
                //PHASE 5+: cycle
                else{
                    //Repeat
                    //Newest at position 0, oldest at position 3
                    if(positionQueue[i] == 0 || positionQueue[i] == 3){
                        targetScale = SCALE_MIN;
                        targetAlpha = ALPHA_MIN;
                    }else if(positionQueue[i] == 1 || positionQueue[i] == 2){
                        targetScale = 1f;
                        targetAlpha = 1f;
                    }
                }
                
                if(Mathf.Abs(currentYPositions[i] - targetYPositions[i]) > 1f){
                    //disgusting, who cares
                    if(currentYPositions[i] < targetYPositions[i]){
                        int fromPos = positionQueue[i] - 1;
                        if(fromPos >= 0){
                            float startY = originalYPositions[fromPos];
                            float endY = targetYPositions[i];
                            
                            if(Mathf.Abs(endY - startY) > .1f){
                                float progress = Mathf.InverseLerp(startY, endY, currentYPositions[i]);
                                
                                //dont read without parental advisory
                                float startScale = SCALE_MIN;
                                float startAlpha = ALPHA_MIN;
                                
                                //Check the phase for the starting position
                                if(messageIndex == 2){//Phase 2: A moving from pos 0 to 1
                                    if(fromPos == 0) { startScale = 1f; startAlpha = 1f; }
                                }
                                else if(messageIndex == 3){//Phase 3
                                    if(fromPos == 0){//B moving from 0 to 1
                                        startScale = 1f;
                                        startAlpha = 1f;
                                    }else if(fromPos == 1){//A moving from 1 to 2
                                        startScale = 1f;
                                        startAlpha = 1f;
                                    }
                                }else if(messageIndex == 4){//Phase 4
                                    if(fromPos == 0){//C moving from 0 to 1{
                                        startScale = 1f;
                                        startAlpha = 1f;
                                    }
                                    else if(fromPos == 1){//B moving from 1 to 2
                                        startScale = 1f;
                                        startAlpha = 1f;
                                    }
                                    else if(fromPos == 2){//A moving from 2 to 3
                                        startScale = SCALE_MIN;
                                        startAlpha = ALPHA_MIN;
                                    }
                                }else if(messageIndex >= 5){
                                    if(fromPos == 0){//Moving from top to second
                                        startScale = SCALE_MIN;
                                        startAlpha = ALPHA_MIN;
                                    }
                                    else if(fromPos == 1){//Second to third
                                        startScale = 1f;
                                        startAlpha = 1f;
                                    }else if(fromPos == 2){//Third to bottom{
                                        startScale = 1f;
                                        startAlpha = 1f;
                                    }
                                }
                                
                                targetScale = Mathf.Lerp(startScale, targetScale, progress);
                                targetAlpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
                            }
                        }
                    }
                }
                
                if(positionQueue[i] == 0 && alphas[i] < .1f && currentYPositions[i] > TOP_CLIP_MASK){
                    float enterProgress = Mathf.InverseLerp(TOP_CLIP_MASK, originalYPositions[0], currentYPositions[i]);
                    targetAlpha = Mathf.Lerp(0f, targetAlpha, enterProgress);
                    targetScale = Mathf.Lerp(SCALE_MIN, targetScale, enterProgress);
                    fadeSpeedToUse = fadeEnterSpeed;
                }
            }else if(positionQueue[i] == 4){
                targetScale = SCALE_MIN;
                targetAlpha = 0f;
            }else if(positionQueue[i] == -2){
                if(Mathf.Abs(currentYPositions[i] - targetYPositions[i]) < 1f){
                    targetYPositions[i] = FINAL_EXIT_Y;
                    positionQueue[i] = -1;
                }else{
                    targetAlpha = 1f;
                    targetScale = SCALE_MIN;
                }
                fadeSpeedToUse = fadeExitSpeed;
            }else if(positionQueue[i] == -1){
                targetAlpha = 0f;
                targetScale = SCALE_MIN;
                fadeSpeedToUse = fadeExitSpeed;
            }
            
            alphas[i] = Mathf.MoveTowards(alphas[i], targetAlpha, Time.unscaledDeltaTime * fadeSpeedToUse);
            scales[i] = Mathf.MoveTowards(scales[i], targetScale, Time.unscaledDeltaTime * 4f);
            
            UpdateSingleMessageVisual(i);
        }
        
        if(!anyMoving && !canShowNextMessage) canShowNextMessage = true;
    }
    
    void UpdateSingleMessageVisual(int index){
        if(msgList[index] == null) return;
        
        msgList[index].Position.Y.Value = currentYPositions[index];
        msgList[index].transform.localScale = new Vector3(1f, scales[index], 1f);
        
        var color = msgList[index].Color;
        color.a = alphas[index];
        msgList[index].Color = color;
        
        if(msgList[index].Border != null){
            var borderColor = msgList[index].Border.Color;
            borderColor.a = alphas[index];
            msgList[index].Border.Color = borderColor;
        }
        
        if(msgList[index].transform.childCount > 0){
            var textBlock = msgList[index].transform.GetChild(0).GetComponent<TextBlock>();
            if(textBlock != null){
                var textColor = textBlock.Color;
                textColor.a = alphas[index];
                textBlock.Color = textColor;
            }
        }
    }
    
    /*not used
    public void AddMessage(string message){
        dialogue.Add(message);
        messageQueue.Enqueue(message);
    }
    
    public void ClearMessages(){
        dialogue.Clear();
        messageQueue.Clear();
        messageIndex = 0;
        
        for(int i = 0; i < MAX_MESSAGES; i++){
            targetYPositions[i] = TOP_CLIP_MASK;
            alphas[i] = 0f;
            scales[i] = ALPHA_MIN;
            positionQueue[i] = -1;
            isActive[i] = false;
            UpdateSingleMessageVisual(i);
        }
        
        canShowNextMessage = true;
    }
    */
    #endregion

    #region INSTAGRAM_POST_EFFECT
    void Handle3DEffect(){
        float dt = Time.unscaledDeltaTime;
        
        if(!isHovered){
            ReturnToDefault(dt);
            return;
        }
        
        if(layerFront == null || uiCamera == null){
            ReturnToDefault(dt);
            return;
        }
        
        Vector2 mousePos = Input.mousePosition;
        Vector2 normalized = Vector2.zero;
        
        if(frontRectTransform != null){
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(frontRectTransform, mousePos, uiCamera, out Vector2 localPoint)){
                Vector2 rectSize = frontRectTransform.rect.size;
                
                if(rectSize.x > .1f && rectSize.y > .1f){
                    normalized = new Vector2(
                        localPoint.x / (rectSize.x * .5f),
                        localPoint.y / (rectSize.y * .5f)
                    );
                    
                    normalized.x = Mathf.Clamp(normalized.x, -1f, 1f);
                    normalized.y = Mathf.Clamp(normalized.y, -1f, 1f);
                }
            }else{
                ReturnToDefault(dt);
                return;
            }
        }
        
        float tiltX = normalized.y * maxTiltX;
        float tiltY = -normalized.x * maxTiltY;
        
        float baseOffsetX = normalized.x * maxOffset;
        float baseOffsetY = normalized.y * maxOffset * .5f;
        
        float highlightFactor = 0f;
        Color targetFrontColor = frontDefaultColor;
        
        if(enableHighlight && layerFront){
            highlightFactor = Mathf.Clamp(tiltX / maxTiltX, -1f, 1f);
            highlightFactor = -highlightFactor;
            
            if(highlightFactor > 0f) targetFrontColor = Color.Lerp(frontDefaultColor, highlightLightColor, highlightFactor * highlightIntensity);
            else if(highlightFactor < 0f) targetFrontColor = Color.Lerp(frontDefaultColor, highlightDarkColor, -highlightFactor * highlightIntensity);
            else targetFrontColor = frontDefaultColor;
        }
        
        if(layerBack){
            Vector3 backTargetRot = backDefaultRot + new Vector3(tiltX * .3f, tiltY * .3f, 0f);
            layerBack.transform.localRotation = Quaternion.Slerp(
                layerBack.transform.localRotation,
                Quaternion.Euler(backTargetRot),
                effectSpeed * dt
            );
            
            Vector3 backTargetPos = backDefaultPos + new Vector3(baseOffsetX * .2f, baseOffsetY * .2f, 0f);
            layerBack.transform.localPosition = Vector3.Lerp(layerBack.transform.localPosition, backTargetPos, effectSpeed * dt);
        }
        
        if(layerMid){
            Vector3 midTargetRot = midDefaultRot + new Vector3(tiltX * .6f, tiltY * .6f, 0f);
            layerMid.transform.localRotation = Quaternion.Slerp(layerMid.transform.localRotation, Quaternion.Euler(midTargetRot), effectSpeed * dt);
            
            Vector3 midTargetPos = midDefaultPos + new Vector3(baseOffsetX * .5f, baseOffsetY * .5f, 0f);
            layerMid.transform.localPosition = Vector3.Lerp(layerMid.transform.localPosition, midTargetPos, effectSpeed * dt);
        }
        
        if(layerFront){
            Vector3 frontTargetRot = frontDefaultRot + new Vector3(tiltX, tiltY, 0f);
            layerFront.transform.localRotation = Quaternion.Slerp(layerFront.transform.localRotation, Quaternion.Euler(frontTargetRot), effectSpeed * dt);
            
            Vector3 frontTargetPos = frontDefaultPos + new Vector3(baseOffsetX, baseOffsetY, 0f);
            layerFront.transform.localPosition = Vector3.Lerp(layerFront.transform.localPosition, frontTargetPos, effectSpeed * dt);
            
            layerFront.Color = Color.Lerp(layerFront.Color, targetFrontColor, effectSpeed * dt);
        }
    }
    
    void ReturnToDefault(float dt){
        if(layerBack){
            layerBack.transform.localRotation = Quaternion.Slerp(layerBack.transform.localRotation, Quaternion.Euler(backDefaultRot), effectSpeed * dt);
            layerBack.transform.localPosition = Vector3.Lerp(layerBack.transform.localPosition, backDefaultPos, effectSpeed * dt);
        }
        
        if(layerMid){
            layerMid.transform.localRotation = Quaternion.Slerp(layerMid.transform.localRotation, Quaternion.Euler(midDefaultRot), effectSpeed * dt);
            layerMid.transform.localPosition = Vector3.Lerp(layerMid.transform.localPosition, midDefaultPos, effectSpeed * dt);
        }
        
        if(layerFront){
            layerFront.transform.localRotation = Quaternion.Slerp(layerFront.transform.localRotation, Quaternion.Euler(frontDefaultRot), effectSpeed * dt);
            layerFront.transform.localPosition = Vector3.Lerp(layerFront.transform.localPosition, frontDefaultPos, effectSpeed * dt);
            layerFront.Color = Color.Lerp(layerFront.Color, frontDefaultColor, effectSpeed * dt);
        }
    }
    #endregion
}