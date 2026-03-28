Shader "Custom/HandySkin"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Fresnel ("Fresnel", Range(0.005, 0.1)) = 0.1
        _MainTex ("Diffuse (RGB) Alpha (A)", 2D) = "white" {}
        _BumpMap ("Normal (Normal)", 2D) = "bump" {}
        _RampTex ("Toon Ramp (RGB)", 2D) = "white" {}
        _FakeLightDirection ("Fake Light Direction", Vector) = (0.35, 0.8, 0.45, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float4 _BumpMap_ST;
                float _Fresnel;
                float4 _FakeLightDirection;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_RampTex);
            SAMPLER(sampler_RampTex);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0 : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv0, _MainTex);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

                return output;
            }

            float3 GetNormalWS(Varyings input)
            {
                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                float3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));

                return normalize(
                    tangentWS * normalTS.x +
                    bitangentWS * normalTS.y +
                    normalWS * normalTS.z);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float3 albedo = tex.rgb * _Color.rgb;
                float specularStrength = tex.a;

                float3 normalWS = GetNormalWS(input);
                float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - input.positionWS);
                float3 lightDirWS = SafeNormalize(_FakeLightDirection.xyz);
                float3 halfDirWS = SafeNormalize(lightDirWS + viewDirWS);

                float ndotl = saturate(dot(normalWS, lightDirWS) * 0.5 + 0.5);
                float ndoth = saturate(dot(normalWS, halfDirWS));
                float baseTerm = 1.0 - saturate(dot(halfDirWS, viewDirWS));
                float fresnelTerm = pow(baseTerm, 5.0) + _Fresnel * (1.0 - pow(baseTerm, 5.0));

                float3 ramp = SAMPLE_TEXTURE2D(_RampTex, sampler_RampTex, float2(ndotl, ndotl)).rgb;
                float specularPower = max(specularStrength * 128.0, 1.0);
                float normalizationTerm = (specularPower + 2.0) / 8.0;
                float blinnPhong = pow(ndoth, specularPower);
                float specularTerm = normalizationTerm * blinnPhong * ndotl * fresnelTerm * specularStrength;

                float3 color = albedo * ramp + specularTerm.xxx;
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
