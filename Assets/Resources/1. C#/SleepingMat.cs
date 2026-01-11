using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Nova;
using Cinemachine;

public class SleepingMat : MonoBehaviour, IInteractable
{
    [Header("EYELID ANIMATIONS")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;
    [SerializeField] private float maxHeight = 1100f;
    [SerializeField] private float closeDur = 1f;
    [SerializeField] private float openDur = 1f;

    [Header("ENDINGS")]
    [SerializeField] private string[] actNames = {
        "Act 1: Resignation",
        "Act 2: Comeback",
        "Act 3: Determination",
        "Act 4: Hope",
        "Act 5: "
    };
    [SerializeField] private string[] endingNames = {
        "Lost cause",
        "Black",
        "\"This is the end.\""
    };

    [Header("UI ELEMENTS")]
    [SerializeField] private GameObject sleepPanel;
    [SerializeField] private UIBlock2D overlay1;
    [SerializeField] private UIBlock2D overlay2;
    [Space(5)]
    [SerializeField] private UIBlock2D padding;
    [SerializeField] private UIBlock2D[] containers;
    [Space(5)]
    [SerializeField] private TextBlock titleText;
    [SerializeField] private TextBlock[] titleChars;
    [SerializeField] private UIBlock2D separator;
    [SerializeField] private TextBlock recapText;
    [Space(5)]
    [SerializeField] private UIBlock2D playerIcon;
    [SerializeField] private UIBlock2D instruction;
    [SerializeField] private Sprite[] playerIconState;

    [Header("UI ANIMATION TWEAK")]
    [SerializeField] private float overlayFadeSpeed = 1f;
    [SerializeField] private float titleSpeed = .1f;
    [SerializeField] private float titlePopScale = 1.5f;
    [SerializeField] private float titlePopDur = .4f;
    [SerializeField] private float separatorSpeed = .5f;
    [SerializeField] private float recapSpeed = .03f;
    [SerializeField] private float recapFade = .8f;
    [SerializeField] private float containerFade = .5f;
    [SerializeField] private float containerDelay = .3f;
    [SerializeField] private float fillSpeed = .8f;
    [SerializeField] private float playerIconFade = .5f;
    [SerializeField] private float instructionFade = .3f;

    [Header("OVERLAP TIMING")]
    [SerializeField] private float titleToSeparatorOverlap = .7f;
    [SerializeField] private float separatorToRecapOverlap = .6f;
    [SerializeField] private float recapToContainersOverlap = .5f;
    [SerializeField] private float containerToPlayerIconOverlap = .8f;
    [SerializeField] private float playerIconToInstructionOverlap = .6f;

    [Header("STATE")]
    public int dayCount = 1;
    public bool ending3;
    private bool isSleeping;
    private bool isTransitioning;
    private bool forceDebug;

    [Header("REFERENCES")]
    public SpriteRenderer playerSprite;

    private GameManager gm;
    private Vector2 topStart;
    private Vector2 bottomStart;
    private Coroutine waveCoroutine;
    private Coroutine paddingCoroutine;
    private float overlay1Y;
    private float overlay2Y;
    private float paddingY;
    
    private List<Transform> panelChildren = new List<Transform>();
    private Color overlay1Color;
    private Color overlay2Color;
    private UIBlock2D instructionIcon;
    private TextBlock instructionTxt;
    private Vector3 instructionStart;
    private Vector3 iconStart;
    private Vector3 textStart;
    private Color playerIconColor;
    private Vector3 playerIconStart;
    
    private Dictionary<TextBlock, Color> titleCharColors = new Dictionary<TextBlock, Color>();
    private Dictionary<TextBlock, Vector3> titleCharScales = new Dictionary<TextBlock, Vector3>();
    
    private Dictionary<UIBlock2D, UIBlock2D> fillMap = new Dictionary<UIBlock2D, UIBlock2D>();
    private Dictionary<UIBlock2D, List<UIBlock2D>> uiChildren = new Dictionary<UIBlock2D, List<UIBlock2D>>();
    private Dictionary<UIBlock2D, List<TextBlock>> textChildren = new Dictionary<UIBlock2D, List<TextBlock>>();
    private Dictionary<TextBlock, Color> textColors = new Dictionary<TextBlock, Color>();
    private Dictionary<UIBlock2D, Color> uiColors = new Dictionary<UIBlock2D, Color>();
    private Dictionary<UIBlock2D, Color> borderColors = new Dictionary<UIBlock2D, Color>();

    void Awake(){
        topStart = topLid.sizeDelta;
        bottomStart = bottomLid.sizeDelta;
        
        if(overlay1 != null){
            overlay1Y = overlay1.Position.Y.Value;
            overlay1Color = overlay1.Color;
        }
        if(overlay2 != null){
            overlay2Y = overlay2.Position.Y.Value;
            overlay2Color = overlay2.Color;
        }
        if(padding != null){
            paddingY = padding.Position.Y.Value;
        }
    }

    void Start(){
        gm = GameManager.Instance;
        CacheAll();
        ResetUI();
        
        if(instruction != null){
            instructionStart = instruction.transform.localPosition;
        }
    }

    void Update(){
        #if UNITY_EDITOR
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.W)){
            forceDebug = !forceDebug;
        }
        #endif
    }

    #region INIT
    void CacheAll(){
        if(sleepPanel != null){
            panelChildren.Clear();
            foreach(Transform child in sleepPanel.transform){
                if(child != padding && child != overlay1?.transform && child != overlay2?.transform){
                    panelChildren.Add(child);
                }
            }
        }
        
        if(playerIcon != null){
            playerIconColor = playerIcon.Color;
            playerIconStart = playerIcon.transform.localPosition;
        }
        
        titleCharColors.Clear();
        titleCharScales.Clear();
        if(titleChars != null && titleChars.Length > 0){
            foreach(var charText in titleChars){
                if(charText != null){
                    titleCharColors[charText] = charText.Color;
                    titleCharScales[charText] = charText.transform.localScale;
                }
            }
        }
        
        if(instruction != null){
            foreach(Transform child in instruction.transform){
                if(child.name.ToLower().Contains("icon")){
                    instructionIcon = child.GetComponent<UIBlock2D>();
                    if(instructionIcon != null) iconStart = instructionIcon.transform.localPosition;
                }
                else if(child.name.ToLower().Contains("text")){
                    instructionTxt = child.GetComponent<TextBlock>();
                    if(instructionTxt != null) textStart = instructionTxt.transform.localPosition;
                }
            }
        }
        
        foreach(var container in containers){
            if(container == null) continue;
            
            List<UIBlock2D> uiList = new List<UIBlock2D>();
            List<TextBlock> txtList = new List<TextBlock>();
            
            SearchChildren(container.transform, uiList, txtList, container);
            
            uiChildren[container] = uiList;
            textChildren[container] = txtList;
            
            if(fillMap.ContainsKey(container)){
                UIBlock2D fill = fillMap[container];
                if(fill != null) fill.Size.X.Percent = 0f;
            }
        }
    }
    
    void SearchChildren(Transform parent, List<UIBlock2D> uiList, List<TextBlock> txtList, UIBlock2D container){
        foreach(Transform child in parent){
            UIBlock2D ui = child.GetComponent<UIBlock2D>();
            if(ui != null){
                uiList.Add(ui);
                
                if(!uiColors.ContainsKey(ui)) uiColors[ui] = ui.Color;
                if(ui.Border.Enabled && !borderColors.ContainsKey(ui)) borderColors[ui] = ui.Border.Color;
                
                if(child.name.ToLower().Contains("fill")) fillMap[container] = ui;
            }
            
            TextBlock txt = child.GetComponent<TextBlock>();
            if(txt != null){
                txtList.Add(txt);
                if(!textColors.ContainsKey(txt)) textColors[txt] = txt.Color;
            }
            
            if(child.childCount > 0) SearchChildren(child, uiList, txtList, container);
        }
    }
    
    void ResetUI(){
        if(sleepPanel != null) sleepPanel.SetActive(false);
        
        SetOverlayAlpha(0f);
        SetOverlayActive(false);
        SetPlayerIconAlpha(0f);
        SetPlayerIconImage(0);
        SetInstructionAlpha(0f);
        SetContainerAlpha(0f);
        
        if(titleChars != null){
            foreach(var charText in titleChars){
                if(charText != null){
                    charText.gameObject.SetActive(false);
                    if(titleCharColors.ContainsKey(charText)){
                        charText.Color = new Color(titleCharColors[charText].r, titleCharColors[charText].g, 
                                                  titleCharColors[charText].b, 0f);
                        charText.transform.localScale = titleCharScales[charText] * .5f;
                    }
                }
            }
        }
        
        if(titleText != null) titleText.gameObject.SetActive(false);
        if(separator != null) separator.gameObject.SetActive(false);
        if(recapText != null) recapText.gameObject.SetActive(false);
        if(padding != null) padding.gameObject.SetActive(false);
        if(playerIcon != null) playerIcon.gameObject.SetActive(false);
        
        foreach(var container in containers){
            if(container != null){
                container.gameObject.SetActive(false);
                if(fillMap.ContainsKey(container)){
                    UIBlock2D fill = fillMap[container];
                    if(fill != null) fill.Size.X.Percent = 0f;
                }
            }
        }
        
        if(instruction != null){
            instruction.gameObject.SetActive(false);
            instruction.transform.localPosition = instructionStart + new Vector3(0f, -100f, 0f);
        }
    }
    
    void SetPanelChildren(bool active){
        foreach(var child in panelChildren){
            if(child != null) child.gameObject.SetActive(active);
        }
    }
    
    void SetOverlayActive(bool active){
        if(overlay1 != null) overlay1.gameObject.SetActive(active);
        if(overlay2 != null) overlay2.gameObject.SetActive(active);
    }
    #endregion

    #region INTERACTION LOGIC
    public void Interact(){
        if(isTransitioning) return;
        isTransitioning = true;

        if(!isSleeping) StartCoroutine(Sleep());
        else StartCoroutine(Wake());
    }
    
    IEnumerator Sleep(){
        SetPlayerIconImage(0);

        Coroutine closeLidsCoroutine = StartCoroutine(CloseLids());
        
        yield return new WaitForSeconds(closeDur * .8f);
        
        Coroutine showUICoroutine = StartCoroutine(ShowUI());
        
        yield return closeLidsCoroutine;
        DisableLids();
        if(playerSprite != null) playerSprite.enabled = false;
        
        yield return showUICoroutine;
        isTransitioning = false;
    }
    
    IEnumerator Wake(){
        SetPlayerIconImage(1);

        yield return StartCoroutine(HideUI());
        if(playerSprite != null) playerSprite.enabled = true;
        EnableLids();
        yield return StartCoroutine(OpenLids());
        isTransitioning = false;
    }
    #endregion

    #region UI SHOW/HIDE
    IEnumerator ShowUI(){
        isSleeping = true;
        ResetUI();
        
        if(sleepPanel != null){
            sleepPanel.SetActive(true);
            SetOverlayActive(true);
            SetPanelChildren(false);
        }
        
        gm.isAnyUiActive = true;
        
        if(gm._dof != null){
            gm.disableBlur = true;
            gm._dof.focusDistance.value = gm.normalFocus;
            gm._dof.aperture.value = gm.normalAperture;
        }
        
        if(waveCoroutine != null) StopCoroutine(waveCoroutine);
        waveCoroutine = StartCoroutine(WaveOverlays());
        
        if(paddingCoroutine != null) StopCoroutine(paddingCoroutine);
        paddingCoroutine = StartCoroutine(WavePadding());
        
        yield return StartCoroutine(FadeOverlaysIn(overlayFadeSpeed));
        
        if(padding != null) padding.gameObject.SetActive(true);
        
        yield return StartCoroutine(AnimateUI());
    }
    
    IEnumerator HideUI(){
        if(waveCoroutine != null){
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }
        
        if(paddingCoroutine != null){
            StopCoroutine(paddingCoroutine);
            paddingCoroutine = null;
        }
        
        yield return StartCoroutine(FadeUIOut(overlayFadeSpeed * .5f));
        
        SetOverlayActive(false);
        
        isSleeping = false;
        if(sleepPanel != null) sleepPanel.SetActive(false);
        dayCount++;
        gm.isAnyUiActive = false;
    }

    IEnumerator FadeUIOut(float dur){
        float t = 0f;
        
        List<UIBlock2D> allUIElements = new List<UIBlock2D>();
        List<TextBlock> allTextElements = new List<TextBlock>();
        
        foreach(var container in containers){
            if(container == null) continue;
            
            allUIElements.Add(container);
            
            if(uiChildren.ContainsKey(container)) allUIElements.AddRange(uiChildren[container]);
            if(textChildren.ContainsKey(container)) allTextElements.AddRange(textChildren[container]);
        }
        
        if(padding != null) allUIElements.Add(padding);
        if(overlay1 != null) allUIElements.Add(overlay1);
        if(overlay2 != null) allUIElements.Add(overlay2);
        if(separator != null) allUIElements.Add(separator);
        if(playerIcon != null) allUIElements.Add(playerIcon);
        if(instruction != null) allUIElements.Add(instruction);
        if(instructionIcon != null) allUIElements.Add(instructionIcon);
        
        if(recapText != null) allTextElements.Add(recapText);
        if(instructionTxt != null) allTextElements.Add(instructionTxt);
        
        if(titleText != null) allTextElements.Add(titleText);
        if(titleChars != null && titleChars.Length > 0) allTextElements.AddRange(titleChars);
        
        allUIElements.RemoveAll(x => x == null);
        allTextElements.RemoveAll(x => x == null);
        
        Dictionary<UIBlock2D, float> uiStartAlphas = new Dictionary<UIBlock2D, float>();
        Dictionary<TextBlock, float> textStartAlphas = new Dictionary<TextBlock, float>();
        
        foreach(var ui in allUIElements) uiStartAlphas[ui] = ui.Color.a;
        foreach(var txt in allTextElements) textStartAlphas[txt] = txt.Color.a;
        
        while(t < dur){
            t += Time.deltaTime;
            float progress = t / dur;
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            foreach(var ui in allUIElements){
                if(ui == null) continue;
                
                Color c = ui.Color;
                float startAlpha = uiStartAlphas.ContainsKey(ui) ? uiStartAlphas[ui] : c.a;
                c.a = startAlpha * alpha;
                ui.Color = c;
                
                if(ui.Border.Enabled){
                    Color borderColor = ui.Border.Color;
                    borderColor.a = borderColor.a * alpha;
                    ui.Border.Color = borderColor;
                }
            }
            
            foreach(var txt in allTextElements){
                if(txt == null) continue;
                
                Color c = txt.Color;
                float startAlpha = textStartAlphas.ContainsKey(txt) ? textStartAlphas[txt] : c.a;
                c.a = startAlpha * alpha;
                txt.Color = c;
            }
            
            yield return null;
        }
        
        foreach(var ui in allUIElements){
            if(ui == null) continue;
            
            Color c = ui.Color;
            c.a = 0f;
            ui.Color = c;
            
            if(ui.Border.Enabled){
                Color borderColor = ui.Border.Color;
                borderColor.a = 0f;
                ui.Border.Color = borderColor;
            }
        }
        
        foreach(var txt in allTextElements){
            if(txt == null) continue;
            
            Color c = txt.Color;
            c.a = 0f;
            txt.Color = c;
        }
    }
    #endregion

    #region ANIMATION SEQUENCE
    IEnumerator AnimateUI(){
        List<Coroutine> activeAnimations = new List<Coroutine>();
        
        Coroutine titleAnim = StartCoroutine(ShowTitle());
        activeAnimations.Add(titleAnim);
        
        float titleTotalTime = (titleChars?.Length ?? 0) > 0 ? 
            ((titleChars.Length * titleSpeed) + titlePopDur) : 
            (titleText != null ? (titleText.Text.Length * titleSpeed) : 0);
        
        if(titleTotalTime > 0){
            float separatorStartTime = titleTotalTime * titleToSeparatorOverlap;
            yield return new WaitForSeconds(separatorStartTime);
            
            Coroutine separatorAnim = StartCoroutine(ShowSeparator());
            activeAnimations.Add(separatorAnim);
            
            float recapStartTime = separatorSpeed * separatorToRecapOverlap;
            yield return new WaitForSeconds(recapStartTime);
            
            Coroutine recapAnim = StartCoroutine(ShowRecap());
            activeAnimations.Add(recapAnim);
            
            string recapTextStr = (dayCount < 5) ? actNames[dayCount - 1] : "Act 5: " + endingNames[GetEnding()];
            float recapTypeTime = recapTextStr.Length * recapSpeed;
            float recapTotalTime = recapTypeTime + recapFade;
            
            float containersStartTime = recapTotalTime * recapToContainersOverlap;
            yield return new WaitForSeconds(containersStartTime);
            
            Coroutine containersAnim = StartCoroutine(ShowContainers());
            activeAnimations.Add(containersAnim);
            
            float containerAnimTime = CalcContainerTime();
            
            float playerIconStartTime = containerAnimTime * containerToPlayerIconOverlap;
            yield return new WaitForSeconds(playerIconStartTime);
            
            Coroutine playerIconAnim = StartCoroutine(ShowPlayerIcon());
            activeAnimations.Add(playerIconAnim);
            
            float instructionStartTime = playerIconFade * playerIconToInstructionOverlap;
            yield return new WaitForSeconds(instructionStartTime);
            
            Coroutine instructionAnim = StartCoroutine(ShowInstruction());
            activeAnimations.Add(instructionAnim);
        }else{
            yield return StartCoroutine(ShowTitle());
            yield return StartCoroutine(ShowSeparator());
            yield return StartCoroutine(ShowRecap());
            yield return StartCoroutine(ShowContainers());
            yield return StartCoroutine(ShowPlayerIcon());
            yield return StartCoroutine(ShowInstruction());
        }
        
        foreach(var anim in activeAnimations) yield return anim;
    }
    
    float CalcContainerTime(){
        if(containers == null || containers.Length == 0) return 0f;
        
        float totalTime = 0f;
        
        for(int i = 0; i < containers.Length; i++){
            float containerTime = containerFade + fillSpeed;
            
            if(i < containers.Length - 1) containerTime += containerDelay * .8f;
            totalTime += containerTime;
        }
        
        return totalTime;
    }
    
    int GetEnding(){
        int win = (DebateDataManager.Instance != null) ? DebateDataManager.Instance.winCount : 0;
        int lose = (DebateDataManager.Instance != null) ? DebateDataManager.Instance.loseCount : 0;

        if(win >= 2) return 0;
        if(lose >= 2) return 1;
        if(ending3) return 2;
        return 0;
    }
    #endregion

    #region LID ANIMATIONS
    IEnumerator CloseLids() => AnimateLids(0f, maxHeight, closeDur, false);
    IEnumerator OpenLids()  => AnimateLids(maxHeight, 0f, openDur, true);
    
    IEnumerator AnimateLids(float start, float end, float dur, bool opening){
        float t = 0f;
        float startFocus = gm._dof.focusDistance.value;
        float startAperture = gm._dof.aperture.value;
        float targetFocus = (opening) ? gm.normalFocus : gm.blurFocus;
        float targetAperture = (opening) ? gm.normalAperture : gm.blurAperture;
        float blurDur = (opening) ? dur * 2f : dur / 1.25f;

        while(t < dur){
            t += Time.deltaTime;
            float heightStep = Mathf.SmoothStep(0f, 1f, t / dur);
            float blurStep = Mathf.SmoothStep(0f, 1f, t / blurDur);
            float h = Mathf.Lerp(start, end, heightStep);

            if(!isSleeping){
                gm._dof.focusDistance.value = Mathf.Lerp(startFocus, targetFocus, blurStep);
                gm._dof.aperture.value = Mathf.Lerp(startAperture, targetAperture, blurStep);
            }
            
            SetLidHeight(h);
            yield return null;
        }
        
        SetLidHeight(end);
        
        if(!isSleeping){
            gm._dof.focusDistance.value = targetFocus;
            gm._dof.aperture.value = targetAperture;
        }
        
        gm.disableBlur = isSleeping;
    }
    
    void SetLidHeight(float h){
        if(topLid.gameObject.activeSelf) topLid.sizeDelta = new Vector2(topStart.x, h);
        if(bottomLid.gameObject.activeSelf) bottomLid.sizeDelta = new Vector2(bottomStart.x, h);
    }
    
    void DisableLids(){
        if(topLid != null) topLid.gameObject.SetActive(false);
        if(bottomLid != null) bottomLid.gameObject.SetActive(false);
    }
    
    void EnableLids(){
        if(topLid != null) topLid.gameObject.SetActive(true);
        if(bottomLid != null) bottomLid.gameObject.SetActive(true);
    }
    #endregion

    #region BACKGROUND EFFECTS
    IEnumerator WaveOverlays(){
        float speed1 = Random.Range(.8f, 1.2f);
        float speed2 = Random.Range(1.5f, 2.1f);
        float amp1 = Random.Range(15f, 25f);
        float amp2 = Random.Range(30f, 40f);
        float offset1 = Random.Range(0f, Mathf.PI * 2f);
        float offset2 = Random.Range(0f, Mathf.PI * 2f);
        
        float time1 = 0f;
        float time2 = 0f;
        
        while(true){
            time1 += Time.deltaTime * speed1;
            time2 += Time.deltaTime * speed2;
            
            float y1 = Mathf.PerlinNoise(time1 * .1f, offset1) * 2f - 1f;
            float y2 = Mathf.PerlinNoise(offset2, time2 * .1f) * 2f - 1f;
            
            y1 *= amp1;
            y2 *= amp2;
            
            y1 = Mathf.Clamp(y1 + overlay1Y, 0f, 200f) - overlay1Y;
            y2 = Mathf.Clamp(y2 + overlay2Y, 0f, 200f) - overlay2Y;
            
            if(overlay1 != null) overlay1.Position.Y.Value = overlay1Y + y1;
            if(overlay2 != null) overlay2.Position.Y.Value = overlay2Y + y2;
            
            yield return null;
        }
    }
    
    IEnumerator WavePadding(){
        if(padding == null) yield break;
        
        float speed = Random.Range(.5f, .8f);
        float amp = Random.Range(40f, 60f);
        float offset = Random.Range(0f, Mathf.PI * 2f);
        
        float time = 0f;
        
        while(true){
            time += Time.deltaTime * speed;
            
            float y = Mathf.PerlinNoise(time * .2f, offset) * 2f - 1f;
            y *= amp;
            
            y = Mathf.Clamp(y + paddingY, -80f, 80f) - paddingY;
            
            padding.Position.Y.Value = paddingY + y;
            
            yield return null;
        }
    }
    
    IEnumerator FadeOverlaysIn(float dur){
        float t = 0f;
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / dur);
            
            if(overlay1 != null){
                Color c = overlay1Color;
                c.a = overlay1Color.a * a;
                overlay1.Color = c;
            }
            
            if(overlay2 != null){
                Color c = overlay2Color;
                c.a = overlay2Color.a * a;
                overlay2.Color = c;
            }
            
            yield return null;
        }
        
        if(overlay1 != null) overlay1.Color = overlay1Color;
        if(overlay2 != null) overlay2.Color = overlay2Color;
    }
    
    IEnumerator FadeOverlaysOut(float dur){
        float t = 0f;
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / dur);
            
            if(overlay1 != null){
                Color c = overlay1.Color;
                c.a = overlay1Color.a * a;
                overlay1.Color = c;
            }
            
            if(overlay2 != null){
                Color c = overlay2.Color;
                c.a = overlay2Color.a * a;
                overlay2.Color = c;
            }
            
            yield return null;
        }
    }
    #endregion

    #region UI ELEMENT ANIMATIONS
    IEnumerator ShowTitle(){
        if(titleChars != null && titleChars.Length > 0){
            List<Coroutine> charAnimations = new List<Coroutine>();
            
            for(int i = 0; i < titleChars.Length; i++){
                TextBlock charText = titleChars[i];
                if(charText == null) continue;
                
                charText.gameObject.SetActive(true);
                
                float delay = i * titleSpeed;
                charAnimations.Add(StartCoroutine(AnimateTitleCharWithDelay(charText, delay)));
            }
            
            foreach(var anim in charAnimations) yield return anim;
        }
        else if(titleText != null){
            titleText.gameObject.SetActive(true);
            string full = titleText.Text;
            titleText.Text = "";
            
            for(int i = 0; i < full.Length; i++){
                titleText.Text += full[i];
                yield return new WaitForSeconds(titleSpeed);
            }
        }
    }
    
    IEnumerator AnimateTitleCharWithDelay(TextBlock charText, float delay){
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(AnimateTitleChar(charText));
    }
    
    IEnumerator AnimateTitleChar(TextBlock charText){
        if(!titleCharColors.ContainsKey(charText) || !titleCharScales.ContainsKey(charText)) yield break;
        
        Color originalColor = titleCharColors[charText];
        Vector3 originalScale = titleCharScales[charText];
        
        charText.Color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
        charText.transform.localScale = originalScale * .5f;
        
        float t = 0f;
        while(t < titlePopDur){
            t += Time.deltaTime;
            float progress = t / titlePopDur;
            
            float scaleProgress;
            if(progress < .5f){
                scaleProgress = progress * 2f;
                float scale = Mathf.Lerp(.5f, titlePopScale, scaleProgress);
                charText.transform.localScale = originalScale * scale;
            }else{
                scaleProgress = (progress - .5f) * 2f;
                float scale = Mathf.Lerp(titlePopScale, 1f, scaleProgress);
                charText.transform.localScale = originalScale * scale;
            }
            
            float alpha = Mathf.Lerp(0f, 1f, progress);
            charText.Color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            yield return null;
        }
        
        charText.transform.localScale = originalScale;
        charText.Color = originalColor;
    }
    
    IEnumerator ShowSeparator(){
        if(separator == null) yield break;
        separator.gameObject.SetActive(true);
        
        Vector3 scale = separator.transform.localScale;
        separator.transform.localScale = new Vector3(0f, scale.y, scale.z);
        
        float t = 0f;
        while(t < separatorSpeed){
            t += Time.deltaTime;
            float x = Mathf.Lerp(0f, scale.x, t / separatorSpeed);
            separator.transform.localScale = new Vector3(x, scale.y, scale.z);
            yield return null;
        }
        
        separator.transform.localScale = scale;
    }
    
    IEnumerator ShowRecap(){
        if(recapText == null) yield break;
        recapText.gameObject.SetActive(true);
        
        string text = (dayCount < 5) ? actNames[dayCount - 1] : "Act 5: " + endingNames[GetEnding()];
        recapText.Text = "";
        
        for(int i = 0; i < text.Length; i++){
            recapText.Text += text[i];
            yield return new WaitForSeconds(recapSpeed);
        }
        
        Color c = recapText.Color;
        recapText.Color = new Color(c.r, c.g, c.b, 0f);
        
        float t = 0f;
        while(t < recapFade){
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, c.a, t / recapFade);
            recapText.Color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        
        recapText.Color = c;
    }
    
    IEnumerator ShowContainers(){
        if(containers == null || containers.Length == 0) yield break;
        
        for(int i = 0; i < containers.Length; i++){
            UIBlock2D container = containers[i];
            if(container == null) continue;
            
            container.gameObject.SetActive(true);
            
            Coroutine fadeAnim = StartCoroutine(FadeContainerChildren(container, containerFade));
            
            yield return new WaitForSeconds(containerFade * .7f);
            
            if(fillMap.ContainsKey(container)){
                UIBlock2D fill = fillMap[container];
                if(fill != null){
                    fill.Size.X.Percent = 0f;
                    
                    Coroutine fillAnim = StartCoroutine(FillBar(fill, fillSpeed));
                    yield return fillAnim;
                }
            }
            
            yield return fadeAnim;
            
            if(i < containers.Length - 1) yield return new WaitForSeconds(containerDelay * .5f);
        }
    }
    
    IEnumerator FadeContainerChildren(UIBlock2D container, float dur){
        if(!uiChildren.ContainsKey(container) && !textChildren.ContainsKey(container)) yield break;
        
        SetContainerChildrenAlpha(container, 0f);
        
        List<Coroutine> fadeAnimations = new List<Coroutine>();
        
        if(textChildren.ContainsKey(container)){
            foreach(var txt in textChildren[container]){
                if(txt != null){
                    fadeAnimations.Add(StartCoroutine(FadeText(txt, dur * .7f)));
                }
            }
        }
        
        if(uiChildren.ContainsKey(container)){
            foreach(var ui in uiChildren[container]){
                if(ui != null){
                    fadeAnimations.Add(StartCoroutine(FadeUI(ui, container, dur * .7f)));
                }
            }
        }
        
        foreach(var anim in fadeAnimations) yield return anim;
    }
    
    IEnumerator FadeText(TextBlock txt, float dur){
        float t = 0f;
        Color c = textColors.ContainsKey(txt) ? textColors[txt] : new Color(1f, 1f, 1f, 1f);
        txt.Color = new Color(c.r, c.g, c.b, 0f);
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, c.a, t / dur);
            txt.Color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        
        txt.Color = c;
    }
    
    IEnumerator FadeUI(UIBlock2D ui, UIBlock2D container, float dur){
        float t = 0f;
        
        Color uiColor = uiColors.ContainsKey(ui) ? uiColors[ui] : new Color(1f, 1f, 1f, 1f);
        Color borderColor = borderColors.ContainsKey(ui) ? borderColors[ui] : new Color(1f, 1f, 1f, 1f);
        
        if(ui.Border.Enabled){
            Color start = borderColor;
            start.a = 0f;
            ui.Border.Color = start;
        }
        
        Color startUI = uiColor;
        startUI.a = 0f;
        ui.Color = startUI;
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / dur);
            
            if(ui.Border.Enabled){
                Color c = borderColor;
                c.a = borderColor.a * a;
                ui.Border.Color = c;
            }
            
            Color c2 = uiColor;
            c2.a = uiColor.a * a;
            ui.Color = c2;
            
            yield return null;
        }
        
        if(ui.Border.Enabled) ui.Border.Color = borderColor;
        ui.Color = uiColor;
    }
    
    void SetContainerChildrenAlpha(UIBlock2D container, float a){
        if(textChildren.ContainsKey(container)){
            foreach(var txt in textChildren[container]){
                if(txt != null){
                    Color c = textColors.ContainsKey(txt) ? textColors[txt] : txt.Color;
                    txt.Color = new Color(c.r, c.g, c.b, a);
                }
            }
        }
        
        if(uiChildren.ContainsKey(container)){
            foreach(var ui in uiChildren[container]){
                if(ui != null){
                    Color c = uiColors.ContainsKey(ui) ? uiColors[ui] : ui.Color;
                    ui.Color = new Color(c.r, c.g, c.b, a);
                    
                    if(ui.Border.Enabled){
                        Color b = borderColors.ContainsKey(ui) ? borderColors[ui] : ui.Border.Color;
                        ui.Border.Color = new Color(b.r, b.g, b.b, a);
                    }
                }
            }
        }
    }
    
    IEnumerator FillBar(UIBlock2D fill, float dur){
        float target = Random.Range(.3f, .95f);
        float overshootTarget = target * 1.04f;
        float t = 0f;
        
        while(t < dur){
            t += Time.deltaTime;
            float progress = t / dur;
            
            if(progress < .75f){
                float phase = progress / .75f;
                float eased = 1f - Mathf.Pow(1f - phase, 3f);
                float current = Mathf.Lerp(0f, overshootTarget, eased);
                fill.Size.X.Percent = Mathf.Clamp01(current);
            }else{
                float phase = (progress - .75f) / .25f;
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                float eased = 1f + c3 * Mathf.Pow(phase - 1f, 3f) + c1 * Mathf.Pow(phase - 1f, 2f);
                
                float current = Mathf.Lerp(overshootTarget, target, eased);
                fill.Size.X.Percent = Mathf.Clamp01(current);
            }
            
            yield return null;
        }
        
        fill.Size.X.Percent = target;
    }
    
    IEnumerator ShowPlayerIcon(){
        if(playerIcon == null) yield break;
        playerIcon.gameObject.SetActive(true);
        yield return StartCoroutine(FadePlayerIcon(playerIconFade));
    }
    
    IEnumerator HidePlayerIcon(){
        if(playerIcon == null) yield break;
        
        yield return StartCoroutine(FadePlayerIconOut(playerIconFade * .67f));
        playerIcon.gameObject.SetActive(false);
    }
    
    IEnumerator FadePlayerIcon(float dur){
        float t = 0f;
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / dur);
            
            Color c = playerIconColor;
            c.a = a;
            playerIcon.Color = c;
            
            yield return null;
        }
        
        playerIcon.Color = playerIconColor;
    }
    
    IEnumerator FadePlayerIconOut(float dur){
        float t = 0f;
        
        if(playerIconState != null && playerIconState.Length > 1) SetPlayerIconImage(1);
        
        Color startColor = playerIcon.Color;
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / dur);
            
            Color c = startColor;
            c.a = a;
            playerIcon.Color = c;
            
            yield return null;
        }
    }
    
    void SetPlayerIconImage(int state){
        if(playerIconState == null || playerIconState.Length <= state) return;
        if(playerIconState[state] == null) return;
        
        playerIcon.SetImage(playerIconState[state]);
    }
    
    IEnumerator ShowInstruction(){
        if(instruction == null) yield break;

        instruction.gameObject.SetActive(true);
        instruction.transform.localPosition = instructionStart;
        yield return StartCoroutine(FadeInstruction(instructionFade));
    }
    
    IEnumerator HideInstruction(){
        if(instruction == null) yield break;
        
        yield return StartCoroutine(FadeInstructionOut(instructionFade * .67f));
        instruction.gameObject.SetActive(false);
    }
    
    IEnumerator FadeInstruction(float dur){
        float t = 0f;
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 1f, t / dur);
            
            if(instructionIcon != null){
                Color c = instructionIcon.Color;
                c.a = a;
                instructionIcon.Color = c;
            }
            
            if(instructionTxt != null){
                Color c = instructionTxt.Color;
                c.a = a;
                instructionTxt.Color = c;
            }
            
            yield return null;
        }
    }
    
    IEnumerator FadeInstructionOut(float dur){
        float t = 0f;
        
        Color iconStart = instructionIcon != null ? instructionIcon.Color : Color.white;
        Color textStart = instructionTxt != null ? instructionTxt.Color : Color.white;
        
        while(t < dur){
            t += Time.deltaTime;
            float a = Mathf.Lerp(1f, 0f, t / dur);
            
            if(instructionIcon != null){
                Color c = iconStart;
                c.a = a;
                instructionIcon.Color = c;
            }
            
            if(instructionTxt != null){
                Color c = textStart;
                c.a = a;
                instructionTxt.Color = c;
            }
            
            yield return null;
        }
    }
    #endregion

    #region UTIL
    void SetContainerAlpha(float a){
        foreach(var container in containers){
            if(container == null) continue;
            SetContainerChildrenAlpha(container, a);
        }
    }
    
    void SetPlayerIconAlpha(float a){
        if(playerIcon != null){
            Color c = playerIcon.Color;
            c.a = a;
            playerIcon.Color = c;
        }
    }
    
    void SetInstructionAlpha(float a){
        if(instructionIcon != null){
            Color c = instructionIcon.Color;
            c.a = a;
            instructionIcon.Color = c;
        }
        
        if(instructionTxt != null){
            Color c = instructionTxt.Color;
            c.a = a;
            instructionTxt.Color = c;
        }
    }
    
    void SetOverlayAlpha(float a){
        if(overlay1 != null){
            Color c = overlay1.Color;
            c.a = a * overlay1Color.a;
            overlay1.Color = c;
        }
        
        if(overlay2 != null){
            Color c = overlay2.Color;
            c.a = a * overlay2Color.a;
            overlay2.Color = c;
        }
    }

    public bool CanInteract(){
        if(forceDebug) return true;
        return DebateDataManager.Instance != null && DebateDataManager.Instance.currentState == DebateDataManager.DebateState.DebateEnded;
    }

    public string GetPrompt() => "Doze Off";
    #endregion
}