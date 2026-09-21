Shader "Custom/SleepTransition"
{
    Properties
    {
        _Color ("Color", Color) = (0, 0, 0, 1)
        _Radius ("Radius", Range(0, 2)) = 1.5
        _Softness ("Softness", Range(0.001, 0.2)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _Color;
            float _Radius;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Move UV coordinates so the centre of the screen is (0,0)
                float2 uv = i.uv - float2(0.5, 0.5);

                // Correct for screen aspect ratio so the transition stays circular
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;

                // Distance from the centre
                float distanceFromCentre = length(uv);

                // Black outside the radius, transparent inside
                float alpha = smoothstep(
                    _Radius - _Softness,
                    _Radius + _Softness,
                    distanceFromCentre
                );

                return float4(
                    _Color.rgb,
                    alpha * _Color.a * i.color.a
                );
            }

            ENDHLSL
        }
    }
}
