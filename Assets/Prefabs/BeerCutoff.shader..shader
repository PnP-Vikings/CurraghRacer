Shader "Custom/BeerCutoff"
{
    Properties
    {
        _Color("Beer Color", Color)         = (1,0.8,0.2,1)
        _CutoffHeight("Fill Height (0–1)", Range(0,1)) = 0
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

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float3 localPos : TEXCOORD0;
                float2 uv       : TEXCOORD1;
            };

            fixed4 _Color;
            float _CutoffHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos      = UnityObjectToClipPos(v.vertex);
                o.localPos = v.vertex.xyz;
                o.uv       = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                if (i.localPos.y > _CutoffHeight) 
                    discard;
                return _Color;
            }
            ENDCG
        }
    }
}
