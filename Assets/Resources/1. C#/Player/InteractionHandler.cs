using UnityEngine;
using TMPro;
using System.Collections;

public class InteractionHandler : MonoBehaviour
{
    [Header("INTERACTION SETTINGS")]
    [SerializeField] private float interactRadius = 1f;
    [SerializeField] private LayerMask interactLayer;
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private Vector3 promptRightOffset = new Vector3(0.8f, 0f, 0f);
    [SerializeField] private Vector3 promptLeftOffset = new Vector3(-1.8f, 0f, 0f);
    [SerializeField] private Transform door;
    
    [Header("ANIMATION SETTINGS")]
    [SerializeField] private float enterSpeed = 5f;
    [SerializeField] private float exitSpeed = 15f;
    [SerializeField] private float enterRotationAngle = -90f;
    [SerializeField] private float exitRotationAngle = 90f;

    private IInteractable currentInteractable;
    private Coroutine animationCoroutine;
    private bool isPromptActive = false;
    private SpriteRenderer promptSprite;
    private CanvasGroup textCanvasGroup;
    private Vector3 originalTextPosition;

    void Start(){
        if(interactPrompt != null){
            promptSprite = interactPrompt.GetComponent<SpriteRenderer>();
            if(promptText != null){
                textCanvasGroup = promptText.GetComponent<CanvasGroup>();
                if(textCanvasGroup == null) textCanvasGroup = promptText.gameObject.AddComponent<CanvasGroup>();
                originalTextPosition = promptText.transform.localPosition;
            }
            
            SetPromptAlpha(0f);
            SetPromptRotation(enterRotationAngle);
            interactPrompt.SetActive(false);
        }
    }

    void Update(){
        if(GameManager.Instance.isPaused) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRadius, interactLayer);
        if(hit != null){
            var interactable = hit.GetComponent<IInteractable>();
            if(interactable != null && interactable.CanInteract()){
                if(currentInteractable != interactable){
                    currentInteractable = interactable;
                    promptText.text = currentInteractable.GetPrompt();
                    
                    bool forceLeft = (door != null && hit.transform == door);
                    interactPrompt.transform.position = transform.position + (forceLeft ? promptLeftOffset : promptRightOffset);
                    
                    ShowPrompt();
                }

                if(Input.GetKeyDown(GameManager.Instance.interactKey)) currentInteractable.Interact();
            }else{
                if(currentInteractable != null) HidePrompt();
                currentInteractable = null;
            }
        }else{
            if(currentInteractable != null) HidePrompt();
            currentInteractable = null;
        }
    }

    void ShowPrompt(){
        if(isPromptActive) return;
        
        isPromptActive = true;
        interactPrompt.SetActive(true);
        
        if(animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateEnter());
    }

    void HidePrompt(){
        if(!isPromptActive) return;
        
        isPromptActive = false;
        
        if(animationCoroutine != null) StopCoroutine(animationCoroutine);
        animationCoroutine = StartCoroutine(AnimateExit());
    }

    IEnumerator AnimateEnter(){
        float timer = 0f;
        float duration = 1f / enterSpeed;
        
        while(timer < duration){
            timer += Time.deltaTime;
            float t = timer / duration;
            
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            float rotation = Mathf.Lerp(enterRotationAngle, 0f, easedT);
            SetPromptRotation(rotation);
            
            float alpha = Mathf.Lerp(0f, 1f, easedT);
            SetPromptAlpha(alpha);
            
            if(promptText != null && textCanvasGroup != null){
                float textY = Mathf.Lerp(-0.1f, 0f, easedT);
                Vector3 textPos = originalTextPosition;
                textPos.y += textY;
                promptText.transform.localPosition = textPos;
            }
            
            yield return null;
        }
        
        SetPromptRotation(0f);
        SetPromptAlpha(1f);
        animationCoroutine = null;
    }

    IEnumerator AnimateExit(){
        float timer = 0f;
        float duration = 1f / exitSpeed;
        
        while(timer < duration){
            timer += Time.deltaTime;
            float t = timer / duration;
            
            float easedT = Mathf.Pow(t, 2f);
            
            float rotation = Mathf.Lerp(0f, exitRotationAngle, easedT);
            SetPromptRotation(rotation);
            
            float alpha = Mathf.Lerp(1f, 0f, easedT);
            SetPromptAlpha(alpha);
            
            if(promptText != null && textCanvasGroup != null){
                float textY = Mathf.Lerp(0f, 0.1f, easedT);
                Vector3 textPos = originalTextPosition;
                textPos.y += textY;
                promptText.transform.localPosition = textPos;
            }
            
            yield return null;
        }
        
        SetPromptRotation(exitRotationAngle);
        SetPromptAlpha(0f);
        interactPrompt.SetActive(false);
        animationCoroutine = null;
    }

    void SetPromptRotation(float angle){
        Vector3 rotation = interactPrompt.transform.localEulerAngles;
        rotation.x = angle;
        interactPrompt.transform.localEulerAngles = rotation;
    }

    void SetPromptAlpha(float alpha){
        if(promptSprite != null){
            Color spriteColor = promptSprite.color;
            spriteColor.a = alpha;
            promptSprite.color = spriteColor;
        }
        
        if(textCanvasGroup != null) textCanvasGroup.alpha = alpha;
    }

    void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}