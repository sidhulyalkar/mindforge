Shader "Mindforge/ProductionTriplanarLitV09"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0,2)) = 1
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _MetersPerTile("World Meters Per Tile", Range(0.25,8)) = 2
        _BlendSharpness("Triplanar Blend Sharpness", Range(1,12)) = 5
        _NormalFadeDistance("Normal Sampling Distance", Range(10,180)) = 72
        [HideInInspector] _Cutoff("Cutoff", Range(0,1)) = 0.5
        [HideInInspector] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="Geometry"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        float4 _BaseColor;
        float _BumpScale;
        float _Metallic;
        float _Smoothness;
        float _MetersPerTile;
        float _BlendSharpness;
        float _NormalFadeDistance;
        float _Cutoff;
        float _Cull;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull [_Cull]
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half4 fogFactorAndVertexLight : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 TriplanarWeights(float3 normalWS)
            {
                float3 weights = pow(max(abs(normalWS), 0.0001), max(_BlendSharpness, 1.0));
                return weights / max(weights.x + weights.y + weights.z, 0.0001);
            }

            void BuildWorldUvs(float3 positionWS, float3 normalWS, out float2 uvX, out float2 uvY, out float2 uvZ)
            {
                float invScale = rcp(max(_MetersPerTile, 0.01));
                float3 axisSign = float3(
                    normalWS.x < 0.0 ? -1.0 : 1.0,
                    normalWS.y < 0.0 ? -1.0 : 1.0,
                    normalWS.z < 0.0 ? -1.0 : 1.0);

                // Mirroring the dominant coordinate on back-facing projections keeps the
                // texture orientation coherent around corners without object-scale UVs.
                uvX = float2(positionWS.z * axisSign.x, positionWS.y) * invScale;
                uvY = float2(positionWS.x, positionWS.z * axisSign.y) * invScale;
                uvZ = float2(positionWS.x * axisSign.z, positionWS.y) * invScale;
            }

            half3 SampleTriplanarAlbedo(float3 positionWS, float3 normalWS, float3 weights)
            {
                float2 uvX, uvY, uvZ;
                BuildWorldUvs(positionWS, normalWS, uvX, uvY, uvZ);
                half3 x = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX).rgb;
                half3 y = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY).rgb;
                half3 z = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ).rgb;
                return x * weights.x + y * weights.y + z * weights.z;
            }

            half3 SampleTriplanarNormal(float3 positionWS, half3 geometricNormalWS, float3 weights)
            {
                float2 uvX, uvY, uvZ;
                BuildWorldUvs(positionWS, geometricNormalWS, uvX, uvY, uvZ);

                half3 tx = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _BumpScale);
                half3 ty = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _BumpScale);
                half3 tz = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _BumpScale);

                half sx = geometricNormalWS.x < 0.0h ? -1.0h : 1.0h;
                half sy = geometricNormalWS.y < 0.0h ? -1.0h : 1.0h;
                half sz = geometricNormalWS.z < 0.0h ? -1.0h : 1.0h;

                // Convert each tangent-space normal into the world basis of its projection.
                half3 nx = normalize(half3(tx.z * sx, tx.y, tx.x * sx));
                half3 ny = normalize(half3(ty.x, ty.z * sy, ty.y * sy));
                half3 nz = normalize(half3(tz.x * sz, tz.y, tz.z * sz));
                half3 blended = normalize(nx * weights.x + ny * weights.y + nz * weights.z);

                // Numerical protection at projection seams. The detail normal must remain
                // in the same hemisphere as the actual geometric surface.
                if (dot(blended, geometricNormalWS) < 0.0h) blended = -blended;
                return blended;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.shadowCoord = GetShadowCoord(positionInputs);

                half fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                half3 vertexLight = VertexLighting(positionInputs.positionWS, output.normalWS);
                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half3 geometricNormalWS = NormalizeNormalPerPixel(input.normalWS);
                float3 weights = TriplanarWeights(geometricNormalWS);
                half3 albedo = SampleTriplanarAlbedo(input.positionWS, geometricNormalWS, weights) * _BaseColor.rgb;

                half3 normalWS = geometricNormalWS;
                float cameraDistance = distance(_WorldSpaceCameraPos.xyz, input.positionWS);
                UNITY_BRANCH
                if (cameraDistance < _NormalFadeDistance)
                {
                    half3 mapped = SampleTriplanarNormal(input.positionWS, geometricNormalWS, weights);
                    float fadeStart = _NormalFadeDistance * 0.68;
                    half normalWeight = 1.0h - saturate((cameraDistance - fadeStart) / max(_NormalFadeDistance - fadeStart, 0.01));
                    normalWS = normalize(lerp(geometricNormalWS, mapped, normalWeight));
                }

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = saturate(_Metallic);
                surfaceData.specular = half3(0.5h, 0.5h, 0.5h);
                surfaceData.smoothness = saturate(_Smoothness);
                surfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
                surfaceData.occlusion = 1.0h;
                surfaceData.emission = half3(0.0h, 0.0h, 0.0h);
                surfaceData.alpha = 1.0h;
                surfaceData.clearCoatMask = 0.0h;
                surfaceData.clearCoatSmoothness = 0.0h;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                inputData.shadowCoord = input.shadowCoord;
                inputData.fogCoord = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // These stock geometry passes are deliberately reused. V0.9 has no displacement or
        // alpha clipping, so shadow/depth geometry exactly matches the production mesh while
        // the more expensive world-space material work stays in ForwardLit only.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
}
