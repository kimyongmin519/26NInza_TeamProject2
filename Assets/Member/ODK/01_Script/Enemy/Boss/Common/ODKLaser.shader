Shader "ODK/Laser"
{
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (1, 1, 1, 1)
        [HDR] _CoreColor ("Core Color", Color) = (1, 1, 1, 1)
        _Intensity ("Intensity", Range(0, 8)) = 2.4
        _CoreWidth ("Core Width", Range(0.01, 0.9)) = 0.22
        _EdgeSoftness ("Edge Softness", Range(0.1, 8)) = 2.2
        _FlowSpeed ("Flow Speed", Range(-20, 20)) = 7
        _PulseDensity ("Pulse Density", Range(0, 40)) = 12
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.28
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent+20"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha One
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            fixed4 _Color;
            fixed4 _CoreColor;
            float _Intensity;
            float _CoreWidth;
            float _EdgeSoftness;
            float _FlowSpeed;
            float _PulseDensity;
            float _PulseStrength;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float centerDistance = abs(input.uv.y - 0.5) * 2.0;
                float glow = pow(saturate(1.0 - centerDistance), _EdgeSoftness);
                float core = 1.0 - smoothstep(
                    _CoreWidth,
                    min(1.0, _CoreWidth + 0.16),
                    centerDistance
                );
                float travellingPulse = sin(
                    input.uv.x * _PulseDensity - _Time.y * _FlowSpeed
                ) * 0.5 + 0.5;
                float pulse = lerp(1.0, travellingPulse, _PulseStrength);
                float endSpark = smoothstep(0.82, 1.0, frac(input.uv.x - _Time.y * _FlowSpeed * 0.08));

                float3 glowColor = _Color.rgb * input.color.rgb;
                float3 coreColor = lerp(input.color.rgb, _CoreColor.rgb, 0.88);
                float3 rgb = (glowColor * glow + coreColor * core * 1.65);
                rgb *= _Intensity * (pulse + endSpark * 0.18);

                float alpha = saturate(glow * 0.8 + core) * _Color.a * input.color.a;
                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
