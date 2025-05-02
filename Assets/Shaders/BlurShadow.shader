Shader "UI/BlurShadow"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ShadowOffset ("Shadow Offset", Vector) = (2, -2, 0, 0)
        _BlurSize ("Blur Size", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ShadowColor;
            float4 _ShadowOffset;
            float _BlurSize;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 offset = _ShadowOffset.xy / _ScreenParams.xy * _BlurSize;

                float4 col = float4(0, 0, 0, 0);
                col += tex2D(_MainTex, i.uv + offset * -1.0);
                col += tex2D(_MainTex, i.uv + offset * -0.5);
                col += tex2D(_MainTex, i.uv + offset *  0.0);
                col += tex2D(_MainTex, i.uv + offset *  0.5);
                col += tex2D(_MainTex, i.uv + offset *  1.0);
                col /= 5.0;

                col *= _ShadowColor;

                float4 original = tex2D(_MainTex, i.uv);
                return max(original, col); // 将阴影和原始图混合（简单方式）
            }
            ENDCG
        }
    }
}