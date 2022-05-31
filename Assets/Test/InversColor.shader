Shader "Hidden/InversColor"
{
    Properties
    {
        //[MainTexture] 
        _MainTex("MainTex", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline"}
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

        CBUFFER_START(UnityPerMaterial)
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        CBUFFER_END

        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionHCS  : SV_POSITION;
            };


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                //float3 scrNor = SampleSceneNormals(IN.uv);
                //return float4(scrNor, 1);
                return 1 - SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
            }
            ENDHLSL
        }
    }    
}