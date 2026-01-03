using UnityEngine;

public class PanoramaMaker : MonoBehaviour
{
    [Header("PANORAMA ROTATION")]
    public float minY = -60f;
    public float maxY = 60f;
    public float rotationSpeed = 20f;

    [Header("DEBUG")]
    public float completedTeleportY = 1000f;

    private enum State{
        ToMax,
        ToMin,
        Done
    }

    private State currentState = State.ToMax;
    private float baseX;
    private float baseZ;

    void Start(){
        Vector3 euler = transform.eulerAngles;
        baseX = euler.x;
        baseZ = euler.z;

        transform.rotation = Quaternion.Euler(baseX, minY, baseZ);
    }

    void Update(){
        if(currentState == State.Done) return;

        float currentY = NormalizeAngle(transform.eulerAngles.y);
        float targetY = currentState == (State.ToMax) ? maxY : minY;

        float newY = Mathf.MoveTowards(
            currentY,
            targetY,
            rotationSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(baseX, newY, baseZ);

        if(Mathf.Approximately(newY, targetY)){
            if(currentState == State.ToMax) currentState = State.ToMin;
            else CompletePanorama();
        }
    }

    void CompletePanorama(){
        currentState = State.Done;

        Vector3 pos = transform.position;
        pos.y = completedTeleportY;
        transform.position = pos;

        Debug.Log("Panorama sweep completed.");
    }

    float NormalizeAngle(float angle){
        if(angle > 180f) angle -= 360f;
        return angle;
    }
}
