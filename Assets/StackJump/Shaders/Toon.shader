// Stylized lit shader for blocks, characters and props: soft wrapped diffuse tinted toward a
// per-world shade color, a sky-colored rim, a gloss highlight and optional emission (for bloom).
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
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Gloss;
                half _Emission;
                half _Rim;
                half _TopBoost;
                half _Grad;
                float _GradHeight;
                half _Flat;
                half _FogAmount;
            CBUFFER_END

            // Set per world from script.
            half4 _SJ_ShadeColor;
            half4 _SJ_RimColor;

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
                float yOS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS;
                o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.yOS = v.positionOS.y;
                o.fogFactor = ComputeFogFactor(p.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 N = normalize(i.normalWS);
                float3 V = GetWorldSpaceNormalizeViewDir(i.positionWS);
                Light l = GetMainLight();

                half ndl = dot(N, l.direction);
                half lit = smoothstep(-0.25, 0.55, ndl);
                half3 base = _BaseColor.rgb * lerp(1.0 - _Grad, 1.0 + _Grad * 0.35, saturate(i.yOS / _GradHeight + 0.5));

                half3 col = base * lerp(_SJ_ShadeColor.rgb, l.color, lit);
                col += base * saturate(N.y) * _TopBoost;
                half fres = pow(1.0 - saturate(dot(N, V)), 3.0);
                col += _SJ_RimColor.rgb * fres * _Rim;
                half3 H = normalize(l.direction + V);
                col += l.color * pow(saturate(dot(N, H)), lerp(16.0, 128.0, _Gloss)) * _Gloss * 0.8;

                col = lerp(col, _BaseColor.rgb, _Flat);
                col += _BaseColor.rgb * _Emission;
                col = lerp(col, MixFog(col, i.fogFactor), _FogAmount);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
