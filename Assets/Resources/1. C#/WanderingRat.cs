using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class WanderingRat : MonoBehaviour
{
    public enum State { Wander, Idle }

    [Header("WANDER")]
    public float moveSpeed = 2f;
    private float changeDirectionInterval;
    [Space(5)]
    public float minX;
    public float maxX;

    [Header("SOUND EFFECT")]
    public AudioSource squeakSource;
    public AudioClip[] squeakClips;
    public float minSqueakInterval = 2.5f;
    public float maxSqueakInterval = 6f;
    [Range(0f, 1f)] public float squeakVolume = .6f;

    [Header("REFERENCES")]
    public Rigidbody2D rb;
    public SpriteRenderer sr;

    private State currentState = State.Wander;
    private Vector2 moveDirection;
    private Vector2 targetVelocity;
    private float changeDirectionTimer;
    private float squeakTimer;
    private float nextSqueakTime;


    void Start(){
        ChooseNewDirection();
        SetVelocity();

        changeDirectionInterval = Random.Range(1.5f, 4f);
        nextSqueakTime = Random.Range(minSqueakInterval, maxSqueakInterval);
    }

    void Update(){
        HandleSqueak();

        changeDirectionTimer += Time.deltaTime;

        if(changeDirectionTimer >= changeDirectionInterval){
            SwitchState();
        }

        if(currentState == State.Wander){
            HandleBoundaries();
        }
    }

    void FixedUpdate(){
        rb.linearVelocity = targetVelocity;
    }

    void SwitchState(){
        currentState = (Random.value >= .5f) ? State.Idle : State.Wander;
        changeDirectionInterval = Random.Range(1.5f, 4f);

        if(currentState == State.Wander){
            ChooseNewDirection();
        }

        SetVelocity();
        changeDirectionTimer = 0f;
    }

    void ChooseNewDirection(){
        bool goRight = (Random.value >= .5f);
        moveDirection = goRight ? Vector2.right : Vector2.left;
        sr.flipX = !goRight;
    }

    void SetVelocity(){
        if(currentState == State.Wander){
            targetVelocity = moveDirection * moveSpeed;
        }
        else{
            targetVelocity = Vector2.zero;
        }
    }

    void HandleBoundaries(){
        if(transform.position.x < minX && moveDirection.x < 0){
            SetDirection(Vector2.right);
        }
        else if(transform.position.x > maxX && moveDirection.x > 0){
            SetDirection(Vector2.left);
        }
    }

    void SetDirection(Vector2 dir){
        moveDirection = dir;
        sr.flipX = dir.x < 0;
        SetVelocity();
    }

    void HandleSqueak(){
        if(squeakSource == null) return;
        if(squeakClips == null || squeakClips.Length == 0) return;
        if(!AudioManager.Instance.CanPlay(AudioChannel.Ambience)) return;

        squeakTimer += Time.deltaTime;

        if(squeakTimer >= nextSqueakTime){
            AudioClip clip = squeakClips[Random.Range(0, squeakClips.Length)];

            squeakSource.pitch = Random.Range(0.9f, 1.1f);
            squeakSource.volume = squeakVolume;
            squeakSource.PlayOneShot(clip);

            nextSqueakTime = Random.Range(minSqueakInterval, maxSqueakInterval);
            squeakTimer = 0f;
        }
    }
}
