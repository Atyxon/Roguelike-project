Shader "Custom/FloatingIslandBlend"
{
    Properties
    {
        _BlendMap ("Blend Map (RGB)", 2D) = "white" {}
        _TexStone ("Stone Texture", 2D) = "white" {}
        _TexDirt  ("Dirt Texture", 2D) = "white" {}
        _TexGrass ("Grass Texture", 2D) = "white" {}

        _TileStone ("Stone Tiling", Float) = 1
        _TileDirt  ("Dirt Tiling", Float) = 1
        _TileGrass ("Grass Tiling", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        // ===========================
        // FORWARD LIT (Receives shadows)
        // ===========================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
            };

            TEXTURE2D(_BlendMap);  SAMPLER(sampler_BlendMap);
            TEXTURE2D(_TexStone);  SAMPLER(sampler_TexStone);
            TEXTURE2D(_TexDirt);   SAMPLER(sampler_TexDirt);
            TEXTURE2D(_TexGrass);  SAMPLER(sampler_TexGrass);

            float _TileStone, _TileDirt, _TileGrass;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float4 blend = SAMPLE_TEXTURE2D(_BlendMap, sampler_BlendMap, IN.uv);

                half4 stone = SAMPLE_TEXTURE2D(_TexStone, sampler_TexStone, IN.uv * _TileStone);
                half4 dirt  = SAMPLE_TEXTURE2D(_TexDirt, sampler_TexDirt, IN.uv * _TileDirt);
                half4 grass = SAMPLE_TEXTURE2D(_TexGrass, sampler_TexGrass, IN.uv * _TileGrass);

                half4 color = stone * blend.r + dirt * blend.g + grass * blend.b;

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 lighting = mainLight.color * mainLight.shadowAttenuation;

                return half4(color.rgb * lighting, 1);
            }
            ENDHLSL
        }


        // ===========================
        // FIXED SHADOW CASTER (no errors)
        // ===========================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
