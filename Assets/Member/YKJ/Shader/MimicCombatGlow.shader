Shader "YKJ/Mimic Combat Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Alpha", 2D) = "white" {}
        [HDR] _Tint ("Effect Tint", Color) = (1, 1, 1, 1)
        _Intensity ("Emission", Float) = 2
        [HideInInspector] _Flip ("Sprite Flip", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float4 _Flip;
                float _Intensity;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz * _Flip.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half alpha = saturate(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a * input.color.a * _Tint.a);
                return half4(input.color.rgb * _Tint.rgb * _Intensity * alpha, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
