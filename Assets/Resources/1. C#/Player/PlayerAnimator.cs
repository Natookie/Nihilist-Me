using UnityEngine;

public class PlayerAnimator : MonoBehaviour{
    [SerializeField]private Animator anim;
    [SerializeField]private SpriteRenderer sr;
    private IMovable mover;

    void Awake(){
        mover = GetComponent<IMovable>();
    }

    void Update(){
        if(GameManager.Instance.isPaused) return;

        float moveX = Input.GetAxisRaw("Horizontal");
        if(moveX != 0) sr.flipX = moveX < 0;
        anim.SetBool("isMoving", mover.IsMoving);
    }
}