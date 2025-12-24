Shader "Custom/UIBlur"
{
    Properties
    {
        _GrabTexture("Background Texture", 2D) = "white" {}
        _Radius("Blur Radius", Range(0, 32)) = 4
        _OverlayColor("Overlay Color/Opacity", Color) = (1,1,1,0.5)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "UIBlur_Y"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UIBlur_Shared.cginc"

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _Radius;
            float4 _OverlayColor;

            half4 frag(v2f IN) : SV_Target
            {
                return GetBlurInDir(IN, _GrabTexture, _GrabTexture_TexelSize, 0, 1, _Radius, _OverlayColor);
            }
            ENDCG
        }

        Pass
        {
            Name "UIBlur_X"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UIBlur_Shared.cginc"

            sampler2D _GrabTexture;
            float4 _GrabTexture_TexelSize;
            float _Radius;
            float4 _OverlayColor;

            half4 frag(v2f IN) : SV_Target
            {
                return GetBlurInDir(IN, _GrabTexture, _GrabTexture_TexelSize, 1, 0, _Radius, _OverlayColor);
            }
            ENDCG
        }
    }

    Fallback "UI/Default"
}
