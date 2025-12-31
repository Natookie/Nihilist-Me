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

    float delayMovementTimer;
    float resetTimer;
    float currentSpeed;
    float moveX;

    const float RESET_TIME = .15f;

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
    [Range(0f, 1f)] public float baseFootstepVolume = .6f;
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

    IInteractable currentInteractable;
    Collider2D[] interactBuffer = new Collider2D[1];
    Collider2D rbCollider;

    bool isOnFuton;
    bool allowToMove;
    bool wasMoving;

    float footstepTimer;

    GameManager gm;

    void Start(){
        gm = GameManager.Instance;
        rbCollider = rb.GetComponent<Collider2D>();
        interactPrompt.SetActive(false);
    }

    void Update(){
        if(gm.isPaused) return;

        moveX = Input.GetAxisRaw("Horizontal");

        HandleInteraction();
        HandleAnim();
        HandleFootsteps();
    }

    void FixedUpdate(){
        HandleMovement();
    }

    void HandleMovement(){
        if(gm.isAnyUiActive) return;

        bool wantsToMove = Mathf.Abs(moveX) > .01f;
        Vector2 moveDir = new Vector2(moveX, 0f).normalized;

        if(wantsToMove){
            resetTimer = 0f;
            delayMovementTimer += Time.fixedDeltaTime;
            if(delayMovementTimer >= delayMovement) allowToMove = true;
        }else{
            delayMovementTimer = 0f;
            resetTimer += Time.fixedDeltaTime;
            if(resetTimer >= RESET_TIME){
                allowToMove = false;
                currentSpeed = 0f;
            }
        }

        if(!allowToMove){
            rb.linearVelocity = Vector2.zero;
            return;
        }

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            moveSpeed,
            Time.fixedDeltaTime * (moveSpeed / accelerationTime)
        );

        rb.linearVelocity = moveDir * currentSpeed;
        if(moveX != 0 && IsOnSlope()) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 2f);
    }

    void HandleInteraction(){
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            interactRadius,
            interactLayer
        );

        if(hit != null){
            currentInteractable = hit.GetComponent<IInteractable>();
            interactPrompt.SetActive(true);
            promptText.text = currentInteractable.GetPrompt();

            if(Input.GetKeyDown(gm.interactKey)) currentInteractable.Interact();
        }else{
            currentInteractable = null;
            interactPrompt.SetActive(false);
        }
    }

    void HandleAnim(){
        if(moveX != 0) sr.flipX = moveX < 0;
        anim.SetBool("isMoving", IsMoving());
    }

    bool IsMoving() => allowToMove && Mathf.Abs(rb.linearVelocity.x) > .25f;

    void HandleFootsteps(){
        bool moving = IsMoving();

        if(moving && !wasMoving) footstepTimer = footstepInterval * .5f;
        if(moving){
            footstepTimer += Time.deltaTime;
            if(footstepTimer >= footstepInterval){
                PlayFootstep();
                footstepTimer = 0f;
            }
        }

        wasMoving = moving;
    }

    void PlayFootstep(){
        if(!footstepSource || footstepSource.isPlaying) return;

        AudioClip clip = isOnFuton ? futonFootstepSound : GetRandomWoodenStep();
        if(!clip) return;

        footstepSource.pitch = Random.Range(1f - pitchVariation, 1f + pitchVariation);
        footstepSource.volume = Mathf.Clamp(
            baseFootstepVolume * Random.Range(1f - volumeVariation, 1f + volumeVariation),
            0f, 1f
        );

        footstepSource.PlayOneShot(clip);
        TryPlayWoodCreak();
    }

    AudioClip GetRandomWoodenStep(){
        if(woodenFootstepSounds == null || woodenFootstepSounds.Length == 0) return null;
        return woodenFootstepSounds[Random.Range(0, woodenFootstepSounds.Length)];
    }

    void TryPlayWoodCreak(){
        if(isOnFuton || !woodenCreakingSound) return;
        if(Random.value > .15f) return;

        footstepSource.pitch = Random.Range(.85f, .95f);
        footstepSource.volume = baseFootstepVolume * Random.Range(.5f, .65f);
        footstepSource.PlayOneShot(woodenCreakingSound);
    }

    bool IsOnSlope(){
        if(!IsMoving()) return false;

        Vector2 origin = sr.bounds.center;
        origin.y = sr.bounds.min.y;

        return slopeCheck.IsTouching(rbCollider) && Physics2D.Raycast(origin, Vector2.down, slopeRayDistance, groundLayer);
    }

    void OnTriggerEnter2D(Collider2D other){
        if(other == futonCollider) isOnFuton = true;
    }
    void OnTriggerExit2D(Collider2D other){
        if(other == futonCollider) isOnFuton = false;
    }

    void OnDrawGizmosSelected(){
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
