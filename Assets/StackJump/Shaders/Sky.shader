// Full-screen sky drawn on a camera-facing quad: three-stop gradient, twinkling stars,
// a nebula band, a sun/moon disc (optionally synthwave-striped) and soft drifting clouds.
Shader "StackJump/Sky"
{
    Properties
    {
        _TopColor("Top", Color) = (0.05,0.02,0.15,1)
        _MidColor("Mid", Color) = (0.15,0.06,0.3,1)
        _BottomColor("Bottom", Color) = (0.25,0.1,0.45,1)
        _MidPoint("Mid Point", Range(0.05,0.95)) = 0.5
        _Stars("Stars", Range(0,1)) = 0
        _Nebula("Nebula", Range(0,1)) = 0
        _NebulaColor("Nebula Color", Color) = (0.5,0.2,1,1)
        _SunColor("Sun Color", Color) = (1,1,1,1)
        _SunPos("Sun Pos", Vector) = (0.5,0.8,0,0)
        _SunSize("Sun Size", Range(0,0.5)) = 0
        _SunGlow("Sun Glow", Range(0.5,20)) = 4
        _SunStripes("Sun Stripes", Range(0,1)) = 0
        _Clouds("Clouds", Range(0,1)) = 0
        _CloudColor("Cloud Color", Color) = (1,1,1,1)
        _Aspect("Aspect", Float) = 1
        _Scroll("Scroll", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" "RenderPipeline"="UniversalPipeline" "PreviewType"="Plane" }
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Sky"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _MidColor;
                half4 _BottomColor;
                half4 _NebulaColor;
                half4 _SunColor;
                half4 _CloudColor;
                float4 _SunPos;
                half _MidPoint;
                half _Stars;
                half _Nebula;
                half _SunSize;
                half _SunGlow;
                half _SunStripes;
                half _Clouds;
                float _Aspect;
                float _Scroll;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

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
                float s = 0.0;
                float a = 0.5;
                for (int k = 0; k < 4; k++)
                {
                    s += a * vnoise(p);
                    p = p * 2.03 + 17.1;
                    a *= 0.5;
                }
                return s;
            }

            float starLayer(float2 p, float scale, float threshold, float t)
            {
                float2 g = p * scale;
                float2 id = floor(g);
                float2 f = frac(g) - 0.5;
                float h = hash21(id);
                float2 o = (float2(hash21(id + 11.3), hash21(id + 31.7)) - 0.5) * 0.6;
                float d = length(f - o);
                float twinkle = 0.55 + 0.45 * sin(t * (1.5 + h * 3.0) + h * 40.0);
                float s = (1.0 - smoothstep(0.0, 0.12 * (0.4 + h), d)) * step(threshold, h);
                return s * twinkle;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                half3 col = uv.y < _MidPoint
                    ? lerp(_BottomColor.rgb, _MidColor.rgb, smoothstep(0.0, 1.0, uv.y / _MidPoint))
                    : lerp(_MidColor.rgb, _TopColor.rgb, smoothstep(0.0, 1.0, (uv.y - _MidPoint) / (1.0 - _MidPoint)));

                float2 p = float2((uv.x - 0.5) * _Aspect, uv.y + _Scroll);
                float t = _Time.y;

                if (_Nebula > 0.001)
                {
                    float band = exp(-pow((uv.y - 0.55 - (uv.x - 0.5) * 0.5) * 3.0, 2.0));
                    float n = fbm(p * 2.2 + float2(t * 0.01, 0.0));
                    col += _NebulaColor.rgb * (smoothstep(0.35, 0.9, n) * 0.9 + 0.08) * band * _Nebula;
                }

                if (_Stars > 0.001)
                {
                    float s = starLayer(p, 28.0, 0.86, t) * 1.6 + starLayer(p + 7.3, 70.0, 0.8, t * 1.3) * 0.7;
                    col += s * _Stars * half3(1.6, 1.5, 1.8) * smoothstep(0.05, 0.5, uv.y);
                }

                if (_SunSize > 0.001)
                {
                    float d = length((uv - _SunPos.xy) * float2(_Aspect, 1.0));
                    float disc = 1.0 - smoothstep(_SunSize * 0.94, _SunSize, d);
                    if (_SunStripes > 0.001)
                    {
                        float sy = (uv.y - _SunPos.y) / _SunSize;
                        float stripe = step(0.45, frac(sy * 5.0 - t * 0.25));
                        disc *= sy > 0.0 ? 1.0 : lerp(1.0, stripe, _SunStripes);
                    }
                    col += _SunColor.rgb * exp(-d * _SunGlow) * 0.35;
                    col = lerp(col, _SunColor.rgb * 1.25, disc);
                }

                if (_Clouds > 0.001)
                {
                    float c = fbm(float2(p.x * 1.2 + t * 0.02, p.y * 3.0));
                    float m = smoothstep(0.52, 0.72, c) * smoothstep(0.0, 0.25, uv.y) * (1.0 - smoothstep(0.45, 0.85, uv.y));
                    col = lerp(col, _CloudColor.rgb, m * _Clouds * 0.85);
                }

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
