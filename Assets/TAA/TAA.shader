Shader "Hidden/TAA"
{
    Properties
    {
        [MainTexture] _MainTex("MainTex", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalRenderPipeline"}
        
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_PreTex);
        SAMPLER(sampler_PreTex);
        float _Blend;
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

            float3 RGBToYCoCg(float3 RGB)
            {
                float Y = dot(RGB, float3(1, 2, 1));
                float Co = dot(RGB, float3(2, 0, -2));
                float Cg = dot(RGB, float3(-1, 2, -1));
                
                float3 YCoCg = float3(Y, Co, Cg);
                return YCoCg;
            }

            float3 YCoCgToRGB(float3 YCoCg)
            {
                float Y = YCoCg.x * 0.25;
                float Co = YCoCg.y * 0.25;
                float Cg = YCoCg.z * 0.25;

                float R = Y + Co - Cg;
                float G = Y + Cg;
                float B = Y - Co - Cg;

                float3 RGB = float3(R, G, B);
                return RGB;
            }
            float3 ClipHistory(float3 History, float3 BoxMin, float3 BoxMax)
            {
                float3 Filtered = (BoxMin + BoxMax) * 0.5f;
                float3 RayOrigin = History;
                float3 RayDir = Filtered - History;
                RayDir = abs(RayDir) < (1.0 / 65536.0) ?(1.0 / 65536.0) : RayDir;
                float3 InvRayDir = rcp(RayDir);
                
                float3 MinIntersect = (BoxMin - RayOrigin) * InvRayDir;
                float3 MaxIntersect = (BoxMax - RayOrigin) * InvRayDir;
                float3 EnterIntersect = min(MinIntersect, MaxIntersect);
                float ClipBlend = max(EnterIntersect.x, max(EnterIntersect.y, EnterIntersect.z));
                ClipBlend = saturate(ClipBlend);
                return lerp(History, Filtered, ClipBlend);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 ori = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float4 pre = SAMPLE_TEXTURE2D(_PreTex, sampler_PreTex, IN.uv);

                float3 AABBMin, AABBMax;
                AABBMax = AABBMin = RGBToYCoCg(ori.xyz);

                for (int x = -1; x <= 1; ++x) 
                {
                    for (int y = -1; y <= 1; ++y) 
                    {
                        float2 duv = float2(x, y) / _ScaledScreenParams.xy;
                        float3 C = RGBToYCoCg(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + duv).xyz);
                        AABBMin = min(AABBMin, C);
                        AABBMax = max(AABBMax, C);
                    }
                }
                float3 preYCoCg = RGBToYCoCg(pre.xyz);
                pre.xyz = YCoCgToRGB(ClipHistory(preYCoCg, AABBMin, AABBMax));

                float4 final = lerp(pre, ori, _Blend);
                return final;
            }
            ENDHLSL
        }
    }    
}