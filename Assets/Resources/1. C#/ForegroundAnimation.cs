using UnityEngine;

public class ForegroundAnimation : MonoBehaviour
{
    [Header("SWING SETTINGS")]
    public float swingAngle = 2.5f;
    public float swingSpeed = .6f;
    public float phaseOffset = 0f;

    private Quaternion startRotation;

    void Start(){
        startRotation = transform.localRotation;
        if(phaseOffset == 0f) phaseOffset = Random.Range(0f, 10f);
    }

    void Update(){
        float z = Mathf.Sin(Time.time * swingSpeed + phaseOffset) * swingAngle;
        transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, z);
    }
}
