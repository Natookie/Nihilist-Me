using UnityEngine;

public class UIFrosted : MonoBehaviour
{
    [Header("Blur Setup")]
    public Material blurMat;            // assign your frosted glass material
    public RenderTexture backgroundRT;  // assign the RenderTexture from the background camera

    void Start(){
        if(blurMat != null && backgroundRT != null){
            blurMat.SetTexture("_GrabTexture", backgroundRT);
            blurMat.SetVector("_GrabTexture_TexelSize",
                new Vector4(1f / backgroundRT.width, 1f / backgroundRT.height,
                            backgroundRT.width, backgroundRT.height));
        }else Debug.LogWarning("Blur material or background RT not assigned!");
    }
}
