Shader "Hidden/HDRP/SilhouetteOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 1, 0, 1)
        _OutlineThickness ("Outline Thickness (World Units)", Float) = 0.015
    }

    HLSLINCLUDE
        #pragma target 4.5
        #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch
        #pragma multi_compile_instancing
        #pragma instancing_options renderinglayer
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "HDRenderPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "ForwardOnly"
            Tags { "LightMode" = "ForwardOnly" }

            // Inverted hull for silhouette-only outline:
            // - Cull Front so only the backfaces of the expanded mesh render.
            // - ZTest LEqual so it doesn't draw through occluders.
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            // We need normals for hull expansion.
            #define ATTRIBUTES_NEED_NORMAL
            // We also want world position available in frag inputs (safe default).
            #define VARYINGS_NEED_POSITION_WS

            // Enables ApplyVertexModification() hook inside HDRP's VertMesh pipeline.
            #define HAVE_VERTEX_MODIFICATION

            // HDRP custom pass renderer include (sets up matrices + HDRP structs/functions)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/RenderPass/CustomPass/CustomPassRenderers.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float  _OutlineThickness;
            CBUFFER_END

            // Called by HDRP's VertMesh when HAVE_VERTEX_MODIFICATION is defined.
            void ApplyVertexModification(AttributesMesh input, float3 normalWS, inout float3 positionRWS, float3 timeParameters)
            {
                positionRWS += normalWS * _OutlineThickness;
            }

            // Unlit output: just a flat color, no lighting.
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 viewDirection, inout PositionInputs posInput,
                                          out SurfaceData surfaceData, out BuiltinData builtinData)
            {
                ZERO_INITIALIZE(BuiltinData, builtinData);
                ZERO_INITIALIZE(SurfaceData, surfaceData);

                builtinData.opacity = _OutlineColor.a;
                builtinData.emissiveColor = 0.0;

                surfaceData.color = _OutlineColor.rgb;
            }

            // Provides Vert/Frag implementations for SHADERPASS_FORWARD_UNLIT (set by CustomPassRenderers.hlsl)
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            ENDHLSL
        }
    }

    Fallback Off
}