// Stylized lit shader for blocks, characters, ground and props: soft wrapped diffuse with real
// main-light shadows, sky/ground hemisphere ambient, a sky-colored rim, gloss, emission, and
// procedural surface patterns (bricks, straw, planks, circuits, rock, tiles, stalk, grass).
Shader "StackJump/Toon"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _Gloss("Gloss", Range(0,1)) = 0.2
        _Emission("Emission", Range(0,4)) = 0
        _Rim("Rim", Range(0,1)) = 0.35
        _TopBoost("Top Boost", Range(0,1)) = 0.12
        _Grad("Vertical Gradient", Range(0,1)) = 0
        _GradHeight("Gradient Height", Float) = 0.6
        _Flat("Flat", Range(0,1)) = 0
        _FogAmount("Fog Amount", Range(0,1)) = 1
        _Pattern("Pattern", Float) = 0
        _PatternColor("Pattern Color", Color) = (0,0,0,1)
        _PatternScale("Pattern Scale", Float) = 1
        _PatternGlow("Pattern Glow", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _PatternColor;
            half _Gloss;
            half _Emission;
            half _Rim;
            half _TopBoost;
            half _Grad;
            float _GradHeight;
            half _Flat;
            half _FogAmount;
            float _Pattern;
            float _PatternScale;
            half _PatternGlow;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Set per world from script.
            half4 _SJ_ShadeColor;
            half4 _SJ_RimColor;
            half4 _SJ_SkyColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float vnoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash21(i), hash21(i + float2(1, 0)), u.x),
                            lerp(hash21(i + float2(0, 1)), hash21(i + float2(1, 1)), u.x), u.y);
            }

            float fbm(float2 p)
            {
                return vnoise(p) * 0.55 + vnoise(p * 2.1 + 7.3) * 0.3 + vnoise(p * 4.3 + 1.7) * 0.15;
            }

            // Distance to the nearest cell border of a jittered grid (crack lines).
            float cellEdge(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float d1 = 8.0, d2 = 8.0;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 o = float2(hash21(i + g), hash21(i + g + 19.7));
                    float d = length(g + o - f);
                    if (d < d1) { d2 = d1; d1 = d; }
                    else if (d < d2) { d2 = d; }
                }
                return d2 - d1;
            }

            // Picks the object-space plane the surface mostly faces: x = along, y = across.
            float2 planeUV(float3 p, float3 n, out float isTop)
            {
                float3 a = abs(n);
                isTop = step(a.x, a.y) * step(a.z, a.y);
                if (isTop > 0.5) return p.xz;
                return a.x > a.z ? float2(p.z, p.y) : float2(p.x, p.y);
            }

            // Returns the surface color; adds glowing parts to emission.
            half3 applyPattern(half3 base, float3 p, float3 n, inout half3 emission)
            {
                int pattern = (int)round(_Pattern);
                if (pattern == 0) return base;
                float isTop;
                float2 uv = planeUV(p, n, isTop) * _PatternScale;
                half3 pc = _PatternColor.rgb;

                if (pattern == 1) // bricks
                {
                    float2 b = uv / float2(0.42, 0.2);
                    b.x += fmod(floor(b.y), 2.0) * 0.5;
                    float2 cell = floor(b);
                    float2 f = frac(b);
                    float edge = min(min(f.x, 1.0 - f.x) * 0.42, min(f.y, 1.0 - f.y) * 0.2);
                    half3 brick = base * (0.86 + 0.24 * hash21(cell)) * lerp(0.82, 1.0, smoothstep(0.0, 0.035, edge));
                    return lerp(pc, brick, smoothstep(0.008, 0.016, edge));
                }
                if (pattern == 2) // straw / hay
                {
                    float2 s = isTop > 0.5 ? float2(uv.x * 3.0 + uv.y * 1.3, uv.y * 30.0) : float2(uv.x * 2.2, uv.y * 42.0);
                    float strands = vnoise(s) * 0.7 + vnoise(s * float2(1.7, 2.3) + 5.1) * 0.3;
                    float speck = step(0.93, hash21(floor(s * float2(4.0, 1.0))));
                    return base * lerp(0.72, 1.18, strands) * (1.0 - speck * 0.25) + pc * speck * 0.15;
                }
                if (pattern == 3) // wood planks
                {
                    float across = uv.y / 0.16;
                    float plank = frac(across);
                    float seam = smoothstep(0.0, 0.07, plank) * smoothstep(1.0, 0.93, plank);
                    float grain = vnoise(float2(uv.x * 1.6 + floor(across) * 3.1, uv.y * 34.0));
                    return base * lerp(0.62, 1.0, seam) * (0.86 + 0.2 * grain);
                }
                if (pattern == 4) // glowing circuit traces
                {
                    float2 g = uv / 0.18;
                    float2 cell = floor(g);
                    float2 f = frac(g);
                    float h = hash21(cell);
                    float w = 0.07;
                    float traceH = step(abs(f.y - 0.5), w) * step(0.35, h);
                    float traceV = step(abs(f.x - 0.5), w) * step(h, 0.65);
                    float pad = step(length(f - 0.5), 0.17) * step(0.75, hash21(cell + 3.3));
                    float m = saturate(traceH + traceV + pad);
                    emission += pc * m * _PatternGlow;
                    return lerp(base * 0.85, pc, m * 0.8);
                }
                if (pattern == 5) // cracked rock
                {
                    float e = cellEdge(uv * 2.6);
                    float crack = 1.0 - smoothstep(0.02, 0.08, e);
                    float tone = fbm(uv * 3.0);
                    return base * (0.78 + 0.4 * tone) * (1.0 - crack * 0.45) + pc * crack * 0.25;
                }
                if (pattern == 6) // square tiles with grout
                {
                    float2 t = uv / 0.5;
                    float2 f = frac(t);
                    float edge = min(min(f.x, 1.0 - f.x), min(f.y, 1.0 - f.y));
                    half3 tile = base * (0.9 + 0.16 * hash21(floor(t))) * lerp(0.85, 1.0, smoothstep(0.0, 0.08, edge));
                    return lerp(pc, tile, smoothstep(0.02, 0.04, edge));
                }
                if (pattern == 7) // stalk grooves (around the Y axis)
                {
                    float a = atan2(p.z, p.x);
                    float groove = 0.5 + 0.5 * cos(a * 9.0);
                    float streak = vnoise(float2(a * 6.0, p.y * 3.0));
                    return base * lerp(0.78, 1.12, groove) * (0.92 + 0.14 * streak);
                }
                if (pattern == 8) // grass / ground blotches
                {
                    float n1 = fbm(uv * 0.45);
                    float n2 = vnoise(uv * 6.0);
                    return lerp(base, pc, smoothstep(0.45, 0.75, n1)) * (0.92 + 0.12 * n2);
                }
                return base;
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.positionOS = v.positionOS.xyz;
                o.normalOS = v.normalOS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light l = GetMainLight(shadowCoord);

                half3 emission = 0;
                half3 base = _BaseColor.rgb * lerp(1.0 - _Grad, 1.0 + _Grad * 0.35, saturate(i.positionOS.y / _GradHeight + 0.5));
                base = applyPattern(base, i.positionOS, normalize(i.normalOS), emission);

                half ndl = dot(N, l.direction);
                half lit = smoothstep(-0.2, 0.6, ndl) * lerp(1.0, l.shadowAttenuation, 0.85);
                half3 ambient = lerp(_SJ_ShadeColor.rgb, _SJ_SkyColor.rgb, saturate(N.y * 0.5 + 0.5) * 0.55);

                half3 col = base * lerp(ambient, l.color, lit);
                col += base * saturate(N.y) * _TopBoost;
                half fres = pow(1.0 - saturate(dot(N, V)), 3.0);
                col += _SJ_RimColor.rgb * fres * _Rim;
                half3 H = normalize(l.direction + V);
                col += l.color * pow(saturate(dot(N, H)), lerp(16.0, 128.0, _Gloss)) * _Gloss * 0.8 * (0.3 + 0.7 * l.shadowAttenuation);

                col = lerp(col, base, _Flat);
                col += base * _Emission + emission;
                col = lerp(col, MixFog(col, i.fogFactor), _FogAmount);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 shadowVert(Attributes v) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDir = normalize(_LightPosition - positionWS);
            #else
                float3 lightDir = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 shadowFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
