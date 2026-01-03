using UnityEngine;
using TMPro;

public class InteractionHandler : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField]private float interactRadius = 1f;
    [SerializeField]private LayerMask interactLayer;
    [SerializeField]private GameObject interactPrompt;
    [SerializeField]private TextMeshProUGUI promptText;
    [SerializeField]private Vector3 promptRightOffset = new Vector3(0.8f, 0f, 0f);
    [SerializeField]private Vector3 promptLeftOffset = new Vector3(-1.8f, 0f, 0f);
    [SerializeField]private Transform door;

    IInteractable currentInteractable;

    void Update(){
        if(GameManager.Instance.isPaused) return;

        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRadius, interactLayer);
        if(hit != null){
            var interactable = hit.GetComponent<IInteractable>();
            if(interactable != null && interactable.CanInteract()){
                currentInteractable = interactable;
                interactPrompt.SetActive(true);
                promptText.text = currentInteractable.GetPrompt();

                bool forceLeft = (door != null && hit.transform == door);
                interactPrompt.transform.position = transform.position + (forceLeft ? promptLeftOffset : promptRightOffset);

                if(Input.GetKeyDown(GameManager.Instance.interactKey)) currentInteractable.Interact();
            }else{
                currentInteractable = null;
                interactPrompt.SetActive(false);
            }
        }else{
            currentInteractable = null;
            interactPrompt.SetActive(false);
        }
    }

    void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}