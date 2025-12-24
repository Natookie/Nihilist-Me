using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chair : MonoBehaviour
{
    [Header("ROTATION")]
    public float rotFactor;
    [Range(0f, 1f)] public float spinChance = .85f;
    public float smoothTime = 1.5f;
    private float defaultTime;

    [Header("MOVEMENT")]
    public float zTargetPos;
    public float zOriginalPos;
    public float moveSpeed;

    [Header("REFERENCES")]
    public GameObject chairTop;
    public GameObject chairObj;
    public Rigidbody2D playerRb;

    private float targetYRot;
    private float currentYVelocity;

    private bool hasTriggered;
    private bool moveToTarget;

    void Start(){
        defaultTime = smoothTime;
    }

    void Update(){
        HandleRot();
        HandleMovement();
    }
    
    void HandleRot(){
        float newY = Mathf.SmoothDampAngle(
            chairTop.transform.eulerAngles.y,
            targetYRot,
            ref currentYVelocity,
            smoothTime
        );
        chairTop.transform.rotation = Quaternion.Euler(0f, newY, 0f);
    }

    void HandleMovement(){
        Vector3 pos = chairObj.transform.position;
        float targetZ = (moveToTarget) ? zTargetPos : zOriginalPos;
        pos.z = Mathf.Lerp(pos.z, targetZ, Time.deltaTime * moveSpeed);
        chairObj.transform.position = pos; 
    }

    void OnTriggerEnter2D(Collider2D other){
        if(!hasTriggered && other.attachedRigidbody == playerRb){
            if(Random.value <= spinChance){
                smoothTime = defaultTime;
                Vector2 vel = playerRb.linearVelocity;
                float speed = vel.magnitude;

                if(speed > .05f){
                    float dir = Mathf.Sign(vel.x);
                    float spinAmount = dir * rotFactor * speed;

                    targetYRot += spinAmount;
                }
            }

            hasTriggered = true;
        }
    }

    void OnTriggerExit2D(Collider2D other){
        if(other.attachedRigidbody == playerRb) hasTriggered = false;
    }

    public void ResetRot(){
        targetYRot = 0f;
        smoothTime = .2f;
    }
    public void Move(bool value) => moveToTarget = value;
}
