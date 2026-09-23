Shader "YKJ/Mimic Energy Laser"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (3, 2.6, 2.2, 1)
        _Intensity ("Energy Intensity", Range(0, 5)) = 1.2
        _CoreWidth ("Core Width", Range(0.02, 0.5)) = 0.16
        _FlowSpeed ("Flow Speed", Range(0, 12)) = 3
        _FlowDensity ("Flow Density", Range(1, 80)) = 28
        _EdgeNoise ("Edge Distortion", Range(0, 0.2)) = 0.06
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "EnergyLaser"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                float _Intensity;
                float _CoreWidth;
                float _FlowSpeed;
                float _FlowDensity;
                float _EdgeNoise;
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
                float flow = input.uv.x * _FlowDensity - _Time.y * _FlowSpeed;
                float wave = sin(flow * 6.2831853);
                float ripple = sin(flow * 17.1 + input.uv.y * 9.0);
                float across = abs(input.uv.y * 2.0 - 1.0);
                float distorted = max(0.0, across + (wave + ripple * 0.4) * _EdgeNoise * across);
                float aa = max(fwidth(distorted), 0.005);
                float core = 1.0 - smoothstep(_CoreWidth, _CoreWidth + aa + 0.06, distorted);
                float body = 1.0 - smoothstep(0.25, 0.68, distorted);
                float halo = pow(saturate(1.0 - across), 1.5);
                float streak = pow(saturate(wave * 0.5 + 0.5), 8.0);
                float rim = exp2(-90.0 * (distorted - 0.58) * (distorted - 0.58));
                float endFade = smoothstep(0.0, 0.006, input.uv.x) *
                    (1.0 - smoothstep(0.985, 1.0, input.uv.x));
                float pulse = 0.94 + 0.06 * sin(_Time.y * 17.0);
                half3 color = input.color.rgb * (halo * 0.4 + body * 0.9 + rim * streak * 0.7);
                color += _CoreColor.rgb * core * (0.8 + streak * 0.25);
                float alpha = saturate(core + body * 0.7 + halo * 0.5) * input.color.a * endFade;
                // Premultiplied edges remain soft without requiring a scene-wide Bloom change.
                return half4(color * _Intensity * pulse * alpha, alpha);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
