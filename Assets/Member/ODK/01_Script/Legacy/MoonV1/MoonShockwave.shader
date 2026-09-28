Shader "ODK/MoonShockwave"
{
    Properties
    {
        _Color ("Color", Color) = (0.45, 0.75, 1, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _RingWidth ("Ring Width", Range(0.01, 0.5)) = 0.12
        _Softness ("Softness", Range(0.001, 0.25)) = 0.04
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
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
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float _Progress;
            float _RingWidth;
            float _Softness;
            float _RippleStrength;

            v2f vert(appdata value)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(value.vertex);
                output.uv = value.uv;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float radius = length((input.uv - 0.5) * 2.0);
                float ringDistance = abs(radius - _Progress);
                float ring = 1.0 - smoothstep(_RingWidth, _RingWidth + _Softness, ringDistance);
                float innerRadius = max(0.0, _Progress - _RingWidth * 2.8);
                float rippleDistance = abs(radius - innerRadius);
                float ripple = (1.0 - smoothstep(_RingWidth * 0.45, _RingWidth * 0.45 + _Softness, rippleDistance));
                float edgeFade = 1.0 - smoothstep(0.92, 1.0, radius);
                float lifeFade = 1.0 - smoothstep(0.72, 1.0, _Progress);
                float alpha = saturate(ring + ripple * _RippleStrength) * edgeFade * lifeFade;
                return fixed4(_Color.rgb, _Color.a * alpha);
            }
            ENDCG
        }
    }
}
