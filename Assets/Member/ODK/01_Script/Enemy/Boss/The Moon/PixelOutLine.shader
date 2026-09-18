Shader "Custom/PixelOutlineSpriteExpanded"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineSize ("Outline Size (Pixels)", Range(1,8)) = 1
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.01

        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile _ PIXELSNAP_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 canvasUV : TEXCOORD0; // 확장된 쿼드 전체 기준 UV
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize; // x,y = 1/size / z,w = size
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineSize;
            float _AlphaThreshold;

            float AlphaSample(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return 0.0;

                return tex2D(_MainTex, uv).a;
            }

            fixed4 ColorSample(float2 uv, fixed4 tint)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return fixed4(0,0,0,0);

                return tex2D(_MainTex, uv) * tint;
            }

            v2f vert(appdata v)
            {
                v2f o;

                float2 texSize = _MainTex_TexelSize.zw;
                float2 expandScale = (texSize + _OutlineSize * 2.0) / texSize;

                float4 pos = v.vertex;
                pos.xy *= expandScale;

                o.vertex = UnityObjectToClipPos(pos);

                #ifdef PIXELSNAP_ON
                    o.vertex = UnityPixelSnap(o.vertex);
                #endif

                o.canvasUV = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 texSize = _MainTex_TexelSize.zw;
                float2 padNorm = _OutlineSize / (texSize + _OutlineSize * 2.0);

                // 확장된 쿼드 UV -> 원본 텍스처 UV로 역변환
                float2 uv = (i.canvasUV - padNorm) / (1.0 - padNorm * 2.0);

                fixed4 sprite = ColorSample(uv, i.color);

                // 원래 스프라이트 영역은 그대로 출력
                if (sprite.a > _AlphaThreshold)
                    return sprite;

                float2 texel = _MainTex_TexelSize.xy;
                float neighbourAlpha = 0.0;

                [unroll]
                for (int x = -8; x <= 8; x++)
                {
                    [unroll]
                    for (int y = -8; y <= 8; y++)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        if (abs(x) > _OutlineSize || abs(y) > _OutlineSize)
                            continue;

                        float2 offset = float2(x, y) * texel;
                        neighbourAlpha = max(neighbourAlpha, AlphaSample(uv + offset));
                    }
                }

                if (neighbourAlpha > _AlphaThreshold)
                {
                    fixed4 outline = _OutlineColor;
                    outline.a *= i.color.a;
                    return outline;
                }

                return fixed4(0,0,0,0);
            }
            ENDCG
        }
    }
}