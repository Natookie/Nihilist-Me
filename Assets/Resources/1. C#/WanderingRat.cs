using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WanderingRat : MonoBehaviour
{
    public enum State { Wander, Idle }

    [Header("WANDER")]
    public float moveSpeed = 2f;
    private float changeDirectionInterval;

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
    private float changeDirectionTimer;
    private float squeakTimer;
    private float nextSqueakTime;


    void Start(){
        ChooseNewDirection();
        nextSqueakTime = Random.Range(minSqueakInterval, maxSqueakInterval);
    }

    void Update(){
        HandleSqueak();

        changeDirectionTimer += Time.deltaTime;

        if(changeDirectionTimer >= changeDirectionInterval){
            currentState = (Random.value >= .5f) ? State.Idle : State.Wander;
            changeDirectionInterval = Random.Range(1.5f, 4f);
            
            if(currentState == State.Wander) ChooseNewDirection();
            else rb.linearVelocity = Vector2.zero;

            changeDirectionTimer = 0f;
        }

        if(currentState == State.Wander) rb.linearVelocity = moveDirection * moveSpeed;
    }

    void ChooseNewDirection(){
        bool goRight = (Random.value >= .5f);
        moveDirection = (goRight) ? Vector2.right : Vector2.left;
        sr.flipX = !goRight;
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
