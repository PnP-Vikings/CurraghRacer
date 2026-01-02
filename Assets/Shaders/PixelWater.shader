Shader "Custom/PixelWater"
{
    Properties
    {
        [Header(Main Colors)]
        _WaterColor("Base Water Color", Color) = (0.4, 0.6, 0.75, 0.85)
        _DarkColor("Dark Stripe Color", Color) = (0.2, 0.4, 0.6, 0.9)
        _HighlightColor("Light Stripe Color", Color) = (0.6, 0.8, 0.9, 1.0)
        _FoamColor("Foam Color", Color) = (0.9, 0.95, 1.0, 1.0)
        _Transparency("Water Transparency", Range(0, 1)) = 0.85
        
        [Header(Wave Settings)]
        _WaveSpeed("Wave Speed", Range(0, 3)) = 0.5
        _WaveDirectionX("Wave Direction X", Range(-1, 1)) = 1.0
        _WaveDirectionY("Wave Direction Y", Range(-1, 1)) = 0.25
        _WaveSpacing("Wave Spacing", Range(0.1, 5)) = 1.5
        _WaveStrength("Wave Strength", Range(0, 1)) = 0.5
        
        [Header(Stripe Pattern)]
        _StripeCount("Number of Stripes", Range(1, 30)) = 10
        _StripeSharpness("Stripe Edge Softness", Range(0.01, 0.5)) = 0.1
        _StripeDistortion("Stripe Distortion", Range(0, 1)) = 0.3
        
        [Header(Foam Details)]
        _FoamAmount("Foam Amount", Range(0, 1)) = 0.3
        _FoamScale("Foam Detail Scale", Range(0.5, 5)) = 2.0
        _FoamSpeed("Foam Speed", Range(0, 3)) = 1.0
        _FoamCutoff("Foam Cutoff", Range(0, 1)) = 0.7
        
        [Header(Pixelation Optional)]
        _PixelDensity("Pixel Density (0=smooth)", Range(0, 100)) = 0
        
        [Header(Style Tweaks)]
        _Smoothness("Overall Smoothness", Range(0, 1)) = 0.5
        _ColorVariation("Color Variation", Range(0, 1)) = 0.2
        _Brightness("Brightness Adjust", Range(-0.3, 0.3)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

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
                float2 uv       : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _WaterColor;
            fixed4 _DarkColor;
            fixed4 _HighlightColor;
            fixed4 _FoamColor;
            float _Transparency;
            
            float _WaveSpeed;
            float _WaveDirectionX;
            float _WaveDirectionY;
            float _WaveSpacing;
            float _WaveStrength;
            
            float _StripeCount;
            float _StripeSharpness;
            float _StripeDistortion;
            
            float _FoamAmount;
            float _FoamScale;
            float _FoamSpeed;
            float _FoamCutoff;
            
            float _PixelDensity;
            
            float _Smoothness;
            float _ColorVariation;
            float _Brightness;

            // Hash function for pseudo-random noise
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Value noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // smoothstep
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Fractal noise for more organic look
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }

            v2f vert(appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 worldUV = i.worldPos.xz;
                
                // Optional pixelation
                float2 uv = worldUV;
                if (_PixelDensity > 0)
                {
                    uv = floor(worldUV * _PixelDensity) / _PixelDensity;
                }
                
                float time = _Time.y * _WaveSpeed;
                
                // Wave direction
                float2 waveDir = normalize(float2(_WaveDirectionX, _WaveDirectionY) + 0.001);
                
                // Main wave pattern - creates the stripe effect
                float wavePattern = dot(uv, waveDir) * _StripeCount + time * 2.0;
                
                // Add smooth flowing distortion
                float distortion1 = sin(uv.x * 2.0 + time * 1.5) * cos(uv.y * 2.0 + time * 1.2);
                float distortion2 = sin(uv.x * 1.3 - time * 1.0) * sin(uv.y * 1.7 + time * 0.8);
                float distortion = (distortion1 + distortion2) * 0.5 * _StripeDistortion;
                
                wavePattern += distortion;
                
                // Create smooth sine wave
                float wave = sin(wavePattern) * 0.5 + 0.5;
                
                // Apply wave strength and smoothness
                wave = lerp(0.5, wave, _WaveStrength);
                wave = pow(wave, 1.0 / max(0.01, _Smoothness));
                
                // Add subtle color variation
                float variation = fbm(uv * 0.5 + time * 0.2) * _ColorVariation;
                wave = saturate(wave + variation);
                
                // Create three-tone color bands with smooth transitions
                fixed4 waterColor;
                
                // Smooth gradient between three colors
                if (wave < 0.4)
                {
                    // Dark to middle
                    float t = wave / 0.4;
                    t = smoothstep(0.0, 1.0, t); // Smooth transition
                    waterColor = lerp(_DarkColor, _WaterColor, t);
                }
                else if (wave < 0.7)
                {
                    // Middle to light
                    float t = (wave - 0.4) / 0.3;
                    t = smoothstep(0.0, 1.0, t);
                    waterColor = lerp(_WaterColor, _HighlightColor, t);
                }
                else
                {
                    // Light highlights
                    float t = (wave - 0.7) / 0.3;
                    t = smoothstep(0.0, 1.0, t);
                    waterColor = lerp(_HighlightColor, _HighlightColor * 1.1, t);
                }
                
                // Foam generation on wave peaks
                float foamNoise = fbm(uv * _FoamScale + time * _FoamSpeed * 0.5);
                float foamMask = smoothstep(_FoamCutoff, _FoamCutoff + 0.1, wave + foamNoise * 0.2);
                
                // Add foam with smooth blending
                waterColor = lerp(waterColor, _FoamColor, foamMask * _FoamAmount);
                
                // Apply brightness adjustment
                waterColor.rgb += _Brightness;
                waterColor.rgb = saturate(waterColor.rgb);
                
                // Apply transparency
                waterColor.a *= _Transparency;
                
                return waterColor;
            }
            ENDCG
        }
    }
    FallBack "Transparent/Diffuse"
}

