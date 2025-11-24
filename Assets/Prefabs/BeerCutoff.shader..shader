Shader "Custom/BeerCutoff"
{
    Properties
    {
        _Color("Beer Color", Color) = (1, 0.8, 0.2, 1)
        _CutoffHeight("Fill Height", Range(0, 2)) = 0
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        _FoamHeight("Foam Height", Range(0, 2)) = 0
        _FoamThickness("Foam Thickness", Range(0.005, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline" }
        LOD 100

        Pass
        {
            Name "BeerFill"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS  : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _FoamColor;
                float _CutoffHeight;
                float _FoamHeight;
                float _FoamThickness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionOS = IN.positionOS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float value = IN.positionOS.y;
                float cutoff = _CutoffHeight;
                float foamTop = max(_FoamHeight, cutoff);
                float thickness = max(0.0001, _FoamThickness);
                float foamStart = max(cutoff, foamTop - thickness);

                if (value > foamTop)
                    discard;

                if (_FoamHeight > cutoff && value >= foamStart)
                {
                    float denom = max(0.0001, foamTop - foamStart);
                    float t = saturate((value - foamStart) / denom);
                    half4 foam = lerp(_FoamColor * 1.15h, _FoamColor, t);
                    foam.a = lerp(0.4h, 0.95h, t);
                    return foam;
                }

                if (value <= cutoff)
                {
                    half4 beer = _Color;
                    beer.a = 1;
                    return beer;
                }

                discard;
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
