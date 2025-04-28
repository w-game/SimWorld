Shader "Custom/DirectionalShadow"
{
    Properties
    {
        [PerRendererData]_MainTex   ("Sprite Texture", 2D) = "white" {}
        _Color     ("Shadow Color", Color) = (0,0,0,0.5)
        _Root      ("Root Pos", Vector)   = (0,0,0,0)
        _Dir       ("Shadow Dir", Vector) = (1,-1,0,0)
        _Scale     ("Scale Factor", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"      ="Transparent"
            "IgnoreProjector"="True"
            "RenderType"  ="Transparent"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            ZTest Always
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4   _Color;
            float2   _Root;    // 根位置（本地坐标）
            float2   _Dir;     // 单位方向向量
            float    _Scale;   // 缩放因子 k

            // 按任意方向拉伸算法（博客中那段公式）
            float2 scaleByDir(float2 p, float2 dir, float k)
            {
                float dx = dir.x, dy = dir.y;
                float a = 1 + (k - 1) * dx * dx;
                float b = (k - 1) * dx * dy;
                float c = (k - 1) * dx * dy;
                float d = 1 + (k - 1) * dy * dy;
                return float2(a * p.x + b * p.y,
                              c * p.x + d * p.y);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                // 本地空间顶点
                float2 localPos = IN.vertex.xy;

                // 以 _Root 为“轴心”，对 localPos - root 做拉伸，再加回 root
                float2 rel = localPos - _Root;
                float2 stretched = scaleByDir(rel, normalize(_Dir), _Scale);
                float2 finalPos = stretched + _Root;

                // finalPos 已经是在对象局部空间，直接送入投影
                OUT.vertex = UnityObjectToClipPos(float4(finalPos, IN.vertex.z, 1));
                OUT.uv = IN.texcoord;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed a = tex2D(_MainTex, IN.uv).a;
                return fixed4(_Color.rgb, a * _Color.a);   // use sprite alpha as mask, tint with shadow colour
            }
            ENDCG
        }
        // Pass 2: draw original sprite
        Pass
        {
            Tags { "LightMode"="Universal2D" }
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            Lighting Off
            ZWrite Off

            CGPROGRAM
            #pragma vertex vertUnmodified
            #pragma fragment fragUnmodified
            #include "UnityCG.cginc"

            struct appdata_un
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
            };
            struct v2f_un
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f_un vertUnmodified(appdata_un IN)
            {
                v2f_un OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.uv     = IN.texcoord;
                return OUT;
            }
            sampler2D _MainTex;
            fixed4 fragUnmodified(v2f_un IN) : SV_Target
            {
                return tex2D(_MainTex, IN.uv);
            }
            ENDCG
        }
    }
}