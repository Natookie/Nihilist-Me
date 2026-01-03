using UnityEngine;

public class PlayerMover : MonoBehaviour, IMovable
{
    [Header("MOVEMENT")]
    [SerializeField]private float moveSpeed = 2f;
    [SerializeField]private float accelerationTime = .8f;
    [SerializeField]private float delayMovement = .15f;

    [Header("SLOPE")]
    [SerializeField]private Collider2D slopeCheck;
    [SerializeField]private LayerMask groundLayer;
    [SerializeField]private float slopeRayDistance = .2f;

    [Header("REFERENCES")]
    [SerializeField]private Rigidbody2D rb;
    [SerializeField]private SpriteRenderer sr;
    [SerializeField]private Collider2D rbCollider;

    const float RESET_TIME = .15f;

    float delayTimer;
    float resetTimer;
    float currentSpeed;
    float inputX;
    bool allowToMove;

    public bool IsMoving => allowToMove && Mathf.Abs(rb.linearVelocity.x) > .25f;

    public void Move(float horizontalInput){
        inputX = horizontalInput;
    }

    void Update(){
        if(GameManager.Instance.isAnyUiActive || GameManager.Instance.isPaused) return;

        inputX = Input.GetAxisRaw("Horizontal");
    }

    void FixedUpdate(){
        if(GameManager.Instance.isAnyUiActive || GameManager.Instance.isPaused) return;

        bool wantsToMove = Mathf.Abs(inputX) > .01f;
        Vector2 moveDir = new Vector2(inputX, 0f).normalized;

        if(wantsToMove){
            resetTimer = 0f;
            delayTimer += Time.fixedDeltaTime;
            if(delayTimer >= delayMovement) allowToMove = true;
        }else{
            delayTimer = 0f;
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

        currentSpeed = Mathf.MoveTowards(currentSpeed, moveSpeed, Time.fixedDeltaTime * (moveSpeed / accelerationTime));
        rb.linearVelocity = moveDir * currentSpeed;

        if(inputX != 0 && IsOnSlope()) rb.linearVelocity = new Vector2(rb.linearVelocity.x, 2f);
    }

    bool IsOnSlope(){
        if(!IsMoving) return false;
        Vector2 origin = sr.bounds.center;
        origin.y = sr.bounds.min.y;

        return (slopeCheck.IsTouching(rbCollider) && Physics2D.Raycast(origin, Vector2.down, slopeRayDistance, groundLayer));
    }
}