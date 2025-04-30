Shader "Custom/Blur"
{
    Properties
    {
        _MainTex   ("Mask (A)", 2D) = "white" {}   // UI sprite / mask alpha
        _BlurRadius ("Blur Radius (px)", Float) = 2.0
        _Opacity    ("Panel Opacity", Range(0,1)) = 0.7
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Built‑in opaque texture provided by URP (enable in the pipeline asset!)
            TEXTURE2D(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);
            float4 _CameraOpaqueTexture_TexelSize;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // user properties
            float _BlurRadius;
            float _Opacity;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            // simple 9‑tap Gaussian kernel
            float4 frag (Varyings i) : SV_Target
            {
                float2 texel = _CameraOpaqueTexture_TexelSize.xy * _BlurRadius;

                float4 col = 0;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2(-1,  1)) * 0.0625;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2( 0,  1)) * 0.125;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2( 1,  1)) * 0.0625;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2(-1,  0)) * 0.125;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv)                      * 0.25;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2( 1,  0)) * 0.125;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2(-1, -1)) * 0.0625;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2( 0, -1)) * 0.125;
                col += SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, i.uv + texel * float2( 1, -1)) * 0.0625;

                // read UI sprite alpha as mask
                float maskA = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).a;

                clip(maskA - 0.001);        // discard fully transparent pixels

                col.a = _Opacity * maskA;   // preserve sprite shape
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}