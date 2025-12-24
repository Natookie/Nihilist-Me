using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("TWEAKS")]
    public float moveSpeed = 5f;
    public float accelerationTime = .4f;
    public float delayMovement = .25f;
    private float delayMovementTimer;

    private float resetTime = .15f;
    private float resetTimer;
    private float moveX;

    [Header("SLOPE")]
    public Collider2D slopeCheck;
    public LayerMask groundLayer;
    public float slopeRayDistance = .2f;

    [Header("ANIMATION")]
    public Animator anim;

    [Header("FOOTSTEP SFX")]
    public AudioSource footstepSource;
    public AudioClip[] woodenFootstepSounds;
    public AudioClip futonFootstepSound;
    public AudioClip woodenCreakingSound;
    [Range(0f, 1f)]
    public float baseFootstepVolume = .6f;
    [Space(10)]
    public float footstepInterval = .4f;
    public float pitchVariation = .15f;
    public float volumeVariation = .1f;


    [Header("INTERACTION")]
    public float interactRadius;
    public LayerMask interactLayer;
    public GameObject interactPrompt;
    public TextMeshProUGUI promptText;

    [Header("REFERENCES")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;
    public BoxCollider2D futonCollider;
    private IInteractable currentInteractable;

    private bool isOnFuton;

    private bool allowToMove;
    private float currentSpeed;
    private float footstepTimer = 0f;
    private bool wasMoving = false;

    GameManager gm;

    void Start(){
        gm = GameManager.Instance;
        interactPrompt.SetActive(false);
    }

    void Update(){
        if(gm.isPaused) return;
        
        HandleInteraction();
        HandleAnim();
        HandleFootsteps();
    }

    void LateUpdate(){
        HandleMovement();
    }

    bool isMoving() => (Mathf.Abs(rb.linearVelocity.x) > .25f && allowToMove);
    void HandleMovement(){
        if(gm.isAnyUiActive) return;

        moveX = Input.GetAxisRaw("Horizontal");
        Vector2 moveDir = new Vector2(moveX, 0f).normalized;

        bool wantsToMove = Mathf.Abs(moveX) > .01f;
        if(wantsToMove){
            resetTimer = 0f;
            delayMovementTimer += Time.deltaTime;
            if(delayMovementTimer >= delayMovement) allowToMove = true;
        }else{
            resetTimer += Time.deltaTime;
            if(resetTimer >= resetTime){
                allowToMove = false;
                resetTimer = 0f;
            }
        }

        if(allowToMove){
            currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, Time.deltaTime * (moveSpeed / accelerationTime));
            rb.linearVelocity = moveDir * currentSpeed;

            if(IsOnSlope() && moveX != 0) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 2f);
        }else{
            currentSpeed = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    void HandleInteraction(){
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactLayer);
        if(hits.Length > 0){
            currentInteractable = hits[0].GetComponent<IInteractable>();
            interactPrompt.SetActive(true);
            promptText.text = currentInteractable.GetPrompt();

            if(Input.GetKeyDown(gm.interactKey)) currentInteractable.Interact();
        }else{
            currentInteractable = null;
            interactPrompt.SetActive(false);
        }
    }

    void HandleAnim(){
        if(moveX != 0) sr.flipX = (moveX < 0);
        anim.SetBool("isMoving", isMoving());
    }

   void HandleFootsteps(){
        bool moving = isMoving();
        
        if(moving && !wasMoving) footstepTimer = footstepInterval * 0.5f;
        if(moving){
            footstepTimer += Time.deltaTime;
            
            if(footstepTimer >= footstepInterval){
                PlayFootstep();
                footstepTimer = 0f;
            }
        }
        
        wasMoving = moving;
    }

    AudioClip GetRandomWoodenStep(){
        if(woodenFootstepSounds == null || woodenFootstepSounds.Length == 0) return null;

        int index = Random.Range(0, woodenFootstepSounds.Length);
        return woodenFootstepSounds[index];
    }


    void PlayFootstep(){
        if(footstepSource == null) return;
        if(footstepSource.isPlaying) return;

        AudioClip stepClip = isOnFuton ? futonFootstepSound : GetRandomWoodenStep();
        if(stepClip == null) return;

        float randomPitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        float randomVolume = baseFootstepVolume * Random.Range(1f - volumeVariation, 1f + volumeVariation);

        footstepInterval = Random.Range(0.2f, 0.3f);
        footstepSource.pitch = randomPitch;
        footstepSource.volume = Mathf.Clamp(randomVolume, 0f, 1f);

        footstepSource.PlayOneShot(stepClip);
        TryPlayWoodCreak();
    }

    void TryPlayWoodCreak(){
        if(isOnFuton) return;
        if(woodenCreakingSound == null) return;

        float creakChance = 0.15f;
        if(Random.value > creakChance) return;

        footstepSource.pitch = Random.Range(0.85f, 0.95f);
        footstepSource.volume = baseFootstepVolume * Random.Range(0.5f, 0.65f);

        footstepSource.PlayOneShot(woodenCreakingSound);
    }

    bool IsOnSlope(){
        Vector2 rayOrigin = sr.bounds.center;
        rayOrigin.y = sr.bounds.min.y;
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, slopeRayDistance, groundLayer);
        Debug.DrawRay(rayOrigin, Vector2.down * slopeRayDistance, Color.red);

        return slopeCheck.IsTouching(rb.GetComponent<Collider2D>()) && hit.collider != null;
    }

    void OnTriggerEnter2D(Collider2D other){ if(other == futonCollider) isOnFuton = true; }
    void OnTriggerExit2D(Collider2D other){ if(other == futonCollider) isOnFuton = false; }

    void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
