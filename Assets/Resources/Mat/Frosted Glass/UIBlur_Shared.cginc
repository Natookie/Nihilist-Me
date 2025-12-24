#include "UnityCG.cginc"
#include "UnityUI.cginc"

struct appdata_t
{
    float4 vertex : POSITION;
    float2 texcoord: TEXCOORD0;
    float4 color : COLOR;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float2 uv : TEXCOORD0;
    float4 color : COLOR;
};

v2f vert(appdata_t v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.texcoord;
    o.color = v.color;
    return o;
}

// Separable Gaussian-like blur in a direction
half4 GetBlurInDir(v2f IN, sampler2D tex, float4 texelSize, half dirX, half dirY, float radius, half4 overlayColor)
{
    int steps = int(radius) * 2 + 1;
    half4 sum = tex2D(tex, IN.uv) * 1.0;
    float step = 1.0;

    for (int i = 1; i <= int(radius); i++)
    {
        float2 offset = float2(i * dirX, i * dirY) * texelSize.xy;
        sum += tex2D(tex, IN.uv + offset);
        sum += tex2D(tex, IN.uv - offset);
    }

    sum /= steps;

    // Overlay blending
    half4 result;
    result.rgb = lerp(sum.rgb, overlayColor.rgb, overlayColor.a);
    result.a = overlayColor.a;
    return result;
}
