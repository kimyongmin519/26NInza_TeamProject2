Shader "KYM/2D/Wind Sway Sprite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] _ZWrite ("Z Write", Float) = 0

        [Header(Wind)]
        _WindStrength ("Wind Strength", Range(0, 0.5)) = 0.03
        _WindSpeed ("Wind Speed", Range(0, 10)) = 1.2
        _WindFrequency ("Wind Frequency", Range(0, 10)) = 1.5
        _SecondaryWave ("Secondary Wave", Range(0, 1)) = 0.25
        _PhaseOffset ("Phase Offset", Float) = 0

        [Header(Bending)]
        _AnchorY ("Fixed Base Y", Float) = 0
        _BendHeight ("Bend Height", Float) = 1
        _BendExponent ("Bend Exponent", Range(0.1, 5)) = 1.5

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _RendererColor ("Renderer Color", Color) = (1,1,1,1)
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
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex WindVertex
            #pragma fragment UnlitFragment
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
                float _WindStrength;
                float _WindSpeed;
                float _WindFrequency;
                float _SecondaryWave;
                float _PhaseOffset;
                float _AnchorY;
                float _BendHeight;
                float _BendExponent;
            CBUFFER_END

            Varyings WindVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(
                    input.positionOS,
                    unity_SpriteProps.xy);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float bendHeight = max(abs(_BendHeight), 0.0001);
                float heightMask = saturate(
                    (input.positionOS.y - _AnchorY) / bendHeight);
                heightMask = pow(heightMask, _BendExponent);

                float phase =
                    _Time.y * _WindSpeed +
                    positionWS.x * _WindFrequency +
                    _PhaseOffset;
                float primaryWave = sin(phase);
                float secondaryWave =
                    sin(phase * 1.73 + 1.21) * _SecondaryWave;

                input.positionOS.x +=
                    (primaryWave + secondaryWave) *
                    _WindStrength *
                    heightMask;

                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
