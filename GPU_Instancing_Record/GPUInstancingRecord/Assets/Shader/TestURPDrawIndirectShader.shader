Shader "Custom/TestURPDrawIndirectShader"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        _Gloss("Gloss", Range(8, 256)) = 16
        _SpecularColor("Specular Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalRenderPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST;
        half4 _BaseColor;
        half _Gloss;
        half4 _SpecularColor;
#if SHADER_TARGET >= 45
        // 修改：所有物体的复合变换矩阵
        StructuredBuffer<float4x4> matricesBuffer;
        StructuredBuffer<float4> colorsBuffer;
#endif
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        ENDHLSL

        Pass
        {
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX_ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 normalWSAndFogFactor : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 color :TEXCOORD3;
            };

            Varyings Vertex(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
            #if SHADER_TARGET >= 45
                float4x4 data = matricesBuffer[instanceID];
                float4 color = colorsBuffer[instanceID];
            #else
                float4x4 data = 0;
                float4 color = 1
            #endif
                float4 positionWS = mul(data, IN.positionOS);
                OUT.positionWS = positionWS;

                OUT.positionCS = mul(unity_MatrixVP, positionWS);
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                // 法线与雾效因子
                float3 normalWS = normalize(mul(data, float4(IN.normalOS, 0))).xyz;
                float fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.normalWSAndFogFactor = float4(normalWS, fogFactor);
                OUT.color = color;
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_TARGET
            {
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                // 获取主光源
                Light light = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 lighting = light.color * light.distanceAttenuation * light.shadowAttenuation;

                // 计算光照
                float3 normalWS = IN.normalWSAndFogFactor.xyz;
                half3 diffuse = saturate(dot(normalWS, light.direction)) * lighting;
                float3 v = normalize(_WorldSpaceCameraPos - IN.positionWS);
                float3 h = normalize(v + light.direction);
                half3 specular = pow(saturate(dot(normalWS, h)), _Gloss) * _SpecularColor.rgb * lighting;
                half3 ambient = SampleSH(normalWS);

                half4 color = half4(albedo.rgb * diffuse + specular + ambient, 1.0);
                float fogFactor = IN.normalWSAndFogFactor.w;
                color.rgb = MixFog(color.rgb, fogFactor);
                color = color * IN.color;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings Vertex(Attributes IN, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4x4 data = matricesBuffer[instanceID];
            #else
                float4x4 data = 0;
            #endif
                Varyings OUT;
                float3 positionWS = mul(data, IN.positionOS).xyz;
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = positionCS;
                OUT.uv = TRANSFORM_TEX(IN.texcoord, _BaseMap);
                return OUT;
            }

            half4 Fragment(Varyings IN) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}