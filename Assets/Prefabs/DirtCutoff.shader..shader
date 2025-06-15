Shader "Custom/DirtCutoff"
{
    Properties
    {
        _Color("Dirt Color", Color)         = (0.5,0.3,0.1,1)
        _CutoffHeight("Dirt Level (0–1)", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f {
                float4 pos      : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float2 uv       : TEXCOORD1;
            };

            fixed4 _Color;
            float _CutoffHeight;

            v2f vert(appdata v) {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                o.uv       = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1) if we're fully “clean,” discard all dirt
                if (_CutoffHeight <= 0)     
                    discard;

                // 2) otherwise remove dirt above the current height
                if (i.localPos.y > _CutoffHeight) 
                    discard;

                return _Color;
            }
            ENDCG
        }
    }
}
