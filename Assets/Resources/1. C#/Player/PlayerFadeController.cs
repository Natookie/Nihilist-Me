using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerFadeController : MonoBehaviour
{
    [Header("PLAYER FADE")]
    public float playerFadeStartDistance = 5f;
    public float playerFadeEndDistance = .3f;

    [Header("CURSOR FADE")]
    public float cursorFadeStartDistance = 8f;
    public float cursorFadeEndDistance = 3f;

    [Header("REFERENCES")]
    public SpriteRenderer sr;
    public Camera cam;
    public Material cursorMaterial;

    Material _playerMat;
    Color _cursorBaseColor;

    static readonly int PlayerColorID = Shader.PropertyToID("_BaseColor");
    static readonly int CursorColorID = Shader.PropertyToID("_Color");

    void Awake(){
        _playerMat = sr.material;
        if(cursorMaterial != null) _cursorBaseColor = cursorMaterial.GetColor(CursorColorID);
    }

    void LateUpdate(){
        float camZ = cam.transform.position.z;
        float playerZ = transform.position.z;
        float dist = Mathf.Abs(camZ - playerZ);

        float playerAlpha = Mathf.InverseLerp(playerFadeEndDistance, playerFadeStartDistance, dist);
        float cursorAlpha = Mathf.InverseLerp(cursorFadeEndDistance, cursorFadeStartDistance, dist);

        ApplyPlayerAlpha(playerAlpha);
        ApplyCursorAlpha(cursorAlpha);
    }

    void ApplyPlayerAlpha(float alpha){
        if(_playerMat == null) return;
        Color c = _playerMat.GetColor(PlayerColorID);
        c.a = alpha;
        _playerMat.SetColor(PlayerColorID, c);
    }

    void ApplyCursorAlpha(float alpha){
        if(cursorMaterial == null) return;

        Color c = _cursorBaseColor;
        c.a = alpha;
        cursorMaterial.SetColor(CursorColorID, c);
    }
}