Shader "KYM/2D/Projectile Grab Cue"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0, 1, 0, 1)
        _OutlineWidth ("Outline Width (Pixels)", Range(1, 3)) = 1
        [HideInInspector] _Color ("Tint", Color) = (1, 1, 1, 1)
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex OutlineVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            float4 _MainTex_TexelSize;

            Varyings OutlineVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                half4 sprite = CommonUnlitFragment(input, input.color);
                if (sprite.a <= 0.05h)
                    return half4(0, 0, 0, 0);

                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                half neighborAlpha = 1;
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(offset.x, 0)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(offset.x, 0)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, offset.y)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - float2(0, offset.y)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + offset).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv - offset).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(offset.x, -offset.y)).a);
                neighborAlpha = min(neighborAlpha, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-offset.x, offset.y)).a);

                if (neighborAlpha <= 0.05h)
                    return half4(_OutlineColor.rgb, sprite.a * _OutlineColor.a);

                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
