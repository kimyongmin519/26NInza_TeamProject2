Shader "YKJ/Mimic Tongue"
{
    Properties
    {
        _FleshColor ("Flesh Color", Color) = (0.95, 0.38, 0.48, 1)
        _EdgeColor ("Edge Color", Color) = (0.4, 0.07, 0.13, 1)
        _Gloss ("Wet Highlight", Range(0, 1)) = 0.45
        _Groove ("Center Groove", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _FleshColor;
                half4 _EdgeColor;
                float _Gloss;
                float _Groove;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float y = input.uv.y * 2.0 - 1.0;
                float tip = saturate((input.uv.x - 0.93) / 0.07);
                float radius = sqrt(saturate(1.0 - tip * tip));
                float across = abs(y);
                float aa = max(fwidth(across - radius), 0.01);
                float mask = 1.0 - smoothstep(radius - aa, radius, across);
                float roundness = sqrt(saturate(1.0 - y * y));
                half3 flesh = lerp(_EdgeColor.rgb, _FleshColor.rgb, roundness);
                float groove = exp2(-250.0 * y * y) * (1.0 - smoothstep(0.85, 1.0, input.uv.x));
                flesh *= 1.0 - groove * _Groove;
                float grain = sin(input.uv.x * 410.0 + sin(y * 53.0)) * sin(y * 97.0);
                flesh *= 1.0 + grain * 0.035;
                float shine = exp2(-110.0 * (y - 0.32) * (y - 0.32));
                shine *= 0.7 + 0.3 * sin(input.uv.x * 37.0);
                flesh = lerp(flesh, half3(1.0, 0.85, 0.84), shine * _Gloss);
                // Vertex tint preserves the attack's yellow warning without making the flesh emissive.
                half3 tint = lerp(half3(1.0, 1.0, 1.0), input.color.rgb, 0.2);
                return half4(flesh * tint, mask * _FleshColor.a * input.color.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
