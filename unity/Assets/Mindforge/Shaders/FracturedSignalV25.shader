Shader "Mindforge/FracturedSignalV25"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.04, 0.03, 0.07, 1)
        [HDR] _EmissionColor ("Emission", Color) = (0.8, 0.05, 1.0, 1)
        _Displacement ("Displacement", Range(0, 0.16)) = 0.035
        _SpatialFrequency ("Spatial Frequency", Range(0.25, 12)) = 3.8
        _MotionScale ("Motion Scale", Range(0, 1)) = 1
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.2
        _FresnelStrength ("Fresnel Strength", Range(0, 4)) = 1.2
        _Roughness ("Roughness", Range(0.05, 1)) = 0.42
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half3 viewDirWS : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                float _Displacement;
                float _SpatialFrequency;
                float _MotionScale;
                float _FresnelPower;
                float _FresnelStrength;
                float _Roughness;
            CBUFFER_END

            float FractureField(float3 p, float t)
            {
                float3 q = p * max(0.25, _SpatialFrequency);
                float a = sin(q.x * 1.17 + q.y * 0.71 + t * 1.31);
                float b = sin(q.y * 1.53 - q.z * 0.83 - t * 1.07);
                float c = sin(q.z * 1.91 + q.x * 0.59 + t * 0.73);
                return (a + b + c) * 0.3333333;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionOS = input.positionOS.xyz;
                float time = _Time.y * _MotionScale;
                float field = FractureField(positionOS, time);
                float split = sign(sin((positionOS.x + positionOS.z * 0.71) * 8.0 + time * 0.43));
                float displacement = (field * 0.72 + split * 0.14) * _Displacement * _MotionScale;
                positionOS += normalize(input.normalOS) * displacement;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalize(normalInputs.normalWS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionInputs.positionWS);
                output.shadowCoord = GetShadowCoord(positionInputs);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 n = normalize(input.normalWS);
                half3 v = normalize(input.viewDirWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                half ndotl = saturate(dot(n, mainLight.direction));
                half wrap = saturate((ndotl + 0.28h) / 1.28h);
                half shadow = mainLight.shadowAttenuation * mainLight.distanceAttenuation;
                half3 direct = mainLight.color * wrap * lerp(0.72h, 1.0h, shadow);

                half fresnel = pow(saturate(1.0h - dot(n, v)), max(0.5h, (half)_FresnelPower));
                half3 baseLit = _BaseColor.rgb * (0.18h + direct * (1.0h - _Roughness * 0.35h));
                half3 fractureGlow = _EmissionColor.rgb * (0.34h + fresnel * _FresnelStrength);
                return half4(baseLit + fractureGlow, _BaseColor.a);
            }
            ENDHLSL
        }

        ShadowCaster
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmissionColor;
                float _Displacement;
                float _SpatialFrequency;
                float _MotionScale;
                float _FresnelPower;
                float _FresnelStrength;
                float _Roughness;
            CBUFFER_END

            float ShadowField(float3 p, float t)
            {
                float3 q = p * max(0.25, _SpatialFrequency);
                return (sin(q.x * 1.17 + q.y * 0.71 + t * 1.31) +
                        sin(q.y * 1.53 - q.z * 0.83 - t * 1.07) +
                        sin(q.z * 1.91 + q.x * 0.59 + t * 0.73)) * 0.3333333;
            }

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 p = input.positionOS.xyz;
                float t = _Time.y * _MotionScale;
                p += normalize(input.normalOS) * ShadowField(p, t) * _Displacement * 0.72 * _MotionScale;
                output.positionHCS = TransformObjectToHClip(p);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
