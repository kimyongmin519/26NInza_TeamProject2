Shader "Custom/StoneCircleRaymarchURP"
{
    Properties
    {
        _GrassColor ("Grass Color", Color) = (0.3, 0.5, 0.2, 1)
        _StoneColor ("Stone Color", Color) = (0.3, 0.315, 0.33, 1)
        _SkyColor ("Sky Color", Color) = (1.0, 1.5, 2.0, 1)
        _SunColor ("Sun Color", Color) = (1.0, 0.25, 0.1, 1)

        _CircleRadius ("Circle Radius", Float) = 5.0
        _StoneCount ("Stone Count", Range(3, 30)) = 13.0

        _CameraDistance ("Camera Distance", Float) = 10.0
        _CameraHeight ("Camera Height", Float) = 1.5
        _FOV ("FOV", Range(0.1, 2.0)) = 0.5

        _TimeSpeed ("Time Speed", Float) = 1.0

        _FogDensity ("Fog Density", Float) = 0.02
        _ShadowSoftness ("Shadow Softness", Float) = 0.1

        _MaxDistance ("Max Distance", Float) = 100.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Raymarch"

            Cull Off
            ZWrite Off
            ZTest Always

            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define TAU 6.28318530718
            #define PI 3.14159265359

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)

            float4 _GrassColor;
            float4 _StoneColor;
            float4 _SkyColor;
            float4 _SunColor;

            float _CircleRadius;
            float _StoneCount;

            float _CameraDistance;
            float _CameraHeight;
            float _FOV;

            float _TimeSpeed;

            float _FogDensity;
            float _ShadowSoftness;

            float _MaxDistance;

            CBUFFER_END


            // =========================================================
            // Vertex
            // =========================================================

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionHCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;

                return output;
            }


            // =========================================================
            // Random
            // =========================================================

            float Hash21(float2 p)
            {
                p = frac(
                    p *
                    float2(
                        123.34,
                        456.21
                    )
                );

                p += dot(
                    p,
                    p + 45.32
                );

                return frac(
                    p.x * p.y
                );
            }


            float3 Hash32(float2 p)
            {
                float3 p3 =
                    frac(
                        float3(
                            p.x,
                            p.y,
                            p.x
                        )
                        *
                        float3(
                            0.1031,
                            0.1030,
                            0.0973
                        )
                    );

                p3 += dot(
                    p3,
                    p3.yxz + 33.33
                );

                return frac(
                    (
                        p3.xxy +
                        p3.yzz
                    )
                    *
                    p3.zyx
                );
            }


            // =========================================================
            // SDF
            // =========================================================

            float SdBox(
                float3 p,
                float3 r
            )
            {
                float3 q =
                    abs(p) - r;

                return
                    length(
                        max(
                            q,
                            0.0
                        )
                    )
                    +
                    min(
                        max(
                            q.x,
                            max(
                                q.y,
                                q.z
                            )
                        ),
                        0.0
                    );
            }


            float3 Tri(float3 p)
            {
                return
                    abs(
                        frac(p) -
                        0.5
                    )
                    -
                    0.25;
            }


            // =========================================================
            // Scene map
            // =========================================================

            float MapScene(
                float3 originalP,
                out float3 outQ
            )
            {
                float3 p = originalP;

                // Terrain deformation
                p.z +=
                    cos(
                        p.y * 0.1
                    )
                    -
                    cos(
                        p.x * 0.162
                    );


                float nStones =
                    max(
                        _StoneCount,
                        1.0
                    );

                float circleRadius =
                    _CircleRadius;

                float d = 100.0;


                // -----------------------------------------------------
                // Stone selection
                // -----------------------------------------------------

                float angle =
                    atan2(
                        p.y,
                        p.x
                    );

                float id =
                    round(
                        angle /
                        TAU *
                        nStones
                    );


                float stoneAngle =
                    TAU *
                    id /
                    nStones;

                float2 cs =
                    float2(
                        cos(stoneAngle),
                        cos(
                            stoneAngle -
                            TAU * 0.25
                        )
                    );


                float3 q = p;


                // Bring point into local stone rotation space
                float2x2 rotateMatrix =
                    float2x2(
                        cs.x,
                        cs.y,
                        -cs.y,
                        cs.x
                    );

                q.xy =
                    mul(
                        q.xy,
                        rotateMatrix
                    );


                // -----------------------------------------------------
                // Section clipping
                // -----------------------------------------------------

                float sectionAngle =
                    TAU /
                    nStones;

                float2 sectionCos =
                    float2(
                        cos(sectionAngle),
                        cos(
                            sectionAngle -
                            TAU * 0.25
                        )
                    );

                float dSection =
                    length(
                        abs(q.xy)
                        -
                        circleRadius *
                        sectionCos
                    )
                    -
                    1.0;

                dSection =
                    max(
                        dSection,
                        p.z - 2.0
                    );

                d =
                    min(
                        d,
                        dSection
                    );


                // -----------------------------------------------------
                // Random stone size
                // -----------------------------------------------------

                float3 stoneRandom =
                    Hash32(cs);

                float3 blockSize =
                    0.1
                    +
                    float3(
                        0.5,
                        0.5,
                        1.5
                    )
                    *
                    stoneRandom;


                q.x -=
                    circleRadius;

                q.z -=
                    0.5 *
                    blockSize.z;


                // -----------------------------------------------------
                // Stone tilt
                // -----------------------------------------------------

                float2 csAbs =
                    abs(cs);

                if (csAbs.x >= csAbs.y)
                {
                    csAbs =
                        csAbs.yx;
                }


                float2x2 tiltMatrix =
                    float2x2(
                        csAbs.y,
                        csAbs.x,
                        -csAbs.x,
                        csAbs.y
                    );

                q.xz =
                    mul(
                        q.xz,
                        tiltMatrix
                    );


                float dBox =
                    SdBox(
                        q,
                        blockSize
                    );


                // -----------------------------------------------------
                // Rough stone surface
                // -----------------------------------------------------

                float3 q2 = p;

                float2x2 roughRotation =
                    float2x2(
                        0.6,
                        0.8,
                        -0.8,
                        0.6
                    );

                q2.yz =
                    mul(
                        q2.yz,
                        roughRotation
                    );


                dBox +=
                    0.3 *
                    dot(
                        Tri(q2),
                        float3(
                            0.666,
                            0.666,
                            0.666
                        )
                    );


                d =
                    min(
                        d,
                        dBox
                    );


                // -----------------------------------------------------
                // Ground
                // -----------------------------------------------------

                d =
                    min(
                        d,
                        p.z
                    );


                // -----------------------------------------------------
                // Ground stones / rough terrain
                // -----------------------------------------------------

                float2 groundCell =
                    floor(
                        p.xy + 0.5
                    );

                q2 +=
                    Hash32(
                        groundCell
                    );


                float groundDetail =
                    p.z
                    +
                    0.15
                    -
                    0.1 *
                    sin(
                        p.y * 0.2
                    )
                    *
                    cos(
                        p.x * 0.4
                    )
                    +
                    0.5 *
                    dot(
                        Tri(q2),
                        float3(
                            0.666,
                            0.666,
                            0.666
                        )
                    );


                d =
                    min(
                        d,
                        groundDetail
                    );


                // Original Shadertoy does this too
                outQ = p;

                return d;
            }


            float MapScene(float3 p)
            {
                float3 dummy;

                return
                    MapScene(
                        p,
                        dummy
                    );
            }


            // =========================================================
            // Normal
            // =========================================================

            float3 CalculateNormal(float3 p)
            {
                const float epsilon =
                    0.001;

                float2 e =
                    float2(
                        epsilon,
                        -epsilon
                    );


                float3 normalValue =
                    e.xxx *
                    MapScene(
                        p + e.xxx
                    );

                normalValue +=
                    e.xyy *
                    MapScene(
                        p + e.xyy
                    );

                normalValue +=
                    e.yxy *
                    MapScene(
                        p + e.yxy
                    );

                normalValue +=
                    e.yyx *
                    MapScene(
                        p + e.yyx
                    );


                return
                    normalize(
                        normalValue
                    );
            }


            // =========================================================
            // Ambient Occlusion
            // =========================================================

            float CalculateAO(
                float3 position,
                float3 normal
            )
            {
                float occ =
                    0.0;

                float scale =
                    1.0;


                [loop]
                for (
                    int i = 0;
                    i < 5;
                    i++
                )
                {
                    float fi =
                        (float)i;

                    float height =
                        0.01
                        +
                        0.12 *
                        fi /
                        4.0;


                    float distance =
                        MapScene(
                            position
                            +
                            height *
                            normal
                        );


                    occ +=
                        (
                            height
                            -
                            distance
                        )
                        *
                        scale;


                    scale *=
                        0.95;


                    if (
                        occ >
                        0.5
                    )
                    {
                        break;
                    }
                }


                return
                    saturate(
                        1.0
                        -
                        2.0 *
                        occ
                    )
                    *
                    (
                        0.5
                        +
                        0.5 *
                        normal.z
                    );
            }


            // =========================================================
            // Soft shadows
            // =========================================================

            float CalculateSoftShadow(
                float3 rayOrigin,
                float3 rayDirection,
                float softness,
                float2 pixelCoord
            )
            {
                float transmission =
                    1.0;


                // Shadertoy iChannel0 noise replacement
                float noise =
                    Hash21(
                        floor(
                            pixelCoord
                        )
                    );


                float t =
                    0.01
                    +
                    noise *
                    max(
                        MapScene(
                            rayOrigin
                        ),
                        0.0
                    );


                [loop]
                for (
                    int i = 0;
                    i < 128;
                    i++
                )
                {
                    float width =
                        softness *
                        t;


                    float distance =
                        MapScene(
                            rayOrigin
                            +
                            t *
                            rayDirection
                        );


                    transmission =
                        min(
                            transmission,
                            smoothstep(
                                -width,
                                width,
                                distance
                            )
                        );


                    if (
                        transmission <
                        0.01
                        ||
                        t >
                        20.0
                    )
                    {
                        break;
                    }


                    t +=
                        max(
                            distance
                            +
                            width,
                            0.005
                        );
                }


                return
                    transmission;
            }


            // =========================================================
            // Fragment
            // =========================================================

            half4 Frag(Varyings input) : SV_Target
            {
                // -----------------------------------------------------
                // Shadertoy UV
                // -----------------------------------------------------

                float2 fragCoord =
                    input.uv *
                    _ScreenParams.xy;


                float2 uv =
                    (
                        2.0 *
                        fragCoord
                        -
                        _ScreenParams.xy
                    )
                    /
                    max(
                        _ScreenParams.y,
                        1.0
                    );


                float time =
                    _Time.y *
                    _TimeSpeed;


                // -----------------------------------------------------
                // Camera
                // -----------------------------------------------------

                float theta =
                    PI
                    *
                    0.5
                    *
                    (
                        0.5
                        +
                        0.5
                        *
                        asin(
                            0.9
                            *
                            sin(
                                0.1 *
                                time
                            )
                        )
                    );


                float3 rayOrigin =
                    float3(
                        -_CameraDistance *
                        cos(theta),

                        _CameraDistance *
                        sin(theta),

                        _CameraHeight
                    );


                float3 cameraForward =
                    normalize(
                        -rayOrigin
                    );


                float3 cameraRight =
                    normalize(
                        cross(
                            cameraForward,
                            float3(
                                0.0,
                                0.0,
                                1.0
                            )
                        )
                    );


                float3 cameraUp =
                    cross(
                        cameraRight,
                        cameraForward
                    );


                float3 rayDirection =
                    normalize(
                        cameraForward
                        +
                        _FOV
                        *
                        (
                            uv.x *
                            cameraRight
                            +
                            uv.y *
                            cameraUp
                        )
                    );


                // -----------------------------------------------------
                // Raymarch
                // -----------------------------------------------------

                float totalDistance =
                    0.0;

                float sceneDistance =
                    0.0;

                bool hit =
                    false;


                [loop]
                for (
                    int i = 0;
                    i < 256;
                    i++
                )
                {
                    float3 samplePosition =
                        rayOrigin
                        +
                        totalDistance
                        *
                        rayDirection;


                    sceneDistance =
                        MapScene(
                            samplePosition
                        );


                    if (
                        sceneDistance <
                        0.001
                    )
                    {
                        hit =
                            true;

                        break;
                    }


                    if (
                        totalDistance >
                        _MaxDistance
                    )
                    {
                        break;
                    }


                    totalDistance +=
                        sceneDistance *
                        0.7;
                }


                float3 position =
                    rayOrigin
                    +
                    totalDistance
                    *
                    rayDirection;


                float3 color;


                // -----------------------------------------------------
                // Lighting
                // -----------------------------------------------------

                float3 skyColor =
                    _SkyColor.rgb;


                float3 sunDirection =
                    normalize(
                        float3(
                            1.0,
                            -1.0,
                            0.1
                        )
                    );


                float3 sunColor =
                    _SunColor.rgb;


                // -----------------------------------------------------
                // Sky
                // -----------------------------------------------------

                if (
                    !hit
                    ||
                    totalDistance >
                    _MaxDistance
                )
                {
                    color =
                        skyColor *
                        0.4;


                    float3 modifiedDirection =
                        normalize(
                            float3(
                                rayDirection.xy
                                +
                                sunDirection.xy,

                                rayDirection.z
                                *
                                5.0
                            )
                        );


                    float sunsetGlow =
                        pow(
                            saturate(
                                dot(
                                    modifiedDirection,
                                    sunDirection
                                )
                            ),
                            10.0
                        );


                    color +=
                        sunColor
                        *
                        sunsetGlow
                        *
                        0.6;


                    float sunDisc =
                        pow(
                            saturate(
                                dot(
                                    rayDirection,
                                    sunDirection
                                )
                            ),
                            10000.0
                        );


                    color +=
                        5.0
                        *
                        sunColor
                        *
                        sunDisc;
                }

                // -----------------------------------------------------
                // Surface
                // -----------------------------------------------------

                else
                {
                    float3 normal =
                        CalculateNormal(
                            position
                        );


                    color =
                        float3(
                            0.0,
                            0.0,
                            0.0
                        );


                    float3 surfaceQ;

                    MapScene(
                        position,
                        surfaceQ
                    );


                    float3 grassColor =
                        _GrassColor.rgb;


                    float3 stoneColor =
                        _StoneColor.rgb;


                    float stoneBlend =
                        smoothstep(
                            0.0,
                            0.01,
                            surfaceQ.z
                        );


                    float3 surfaceColor =
                        lerp(
                            grassColor,
                            stoneColor,
                            stoneBlend
                        );


                    // Ambient Occlusion
                    float ao =
                        CalculateAO(
                            position,
                            normal
                        );


                    // Soft shadow
                    float shadow =
                        CalculateSoftShadow(
                            position
                            +
                            0.01 *
                            normal,

                            sunDirection,

                            _ShadowSoftness,

                            fragCoord
                        );


                    color +=
                        sunColor
                        *
                        shadow
                        *
                        surfaceColor
                        *
                        8.0;


                    color +=
                        ao
                        *
                        0.1
                        *
                        skyColor
                        *
                        (
                            0.5
                            +
                            0.5 *
                            normal.z
                        );


                    // -------------------------------------------------
                    // Fog
                    // -------------------------------------------------

                    float fog =
                        1.0
                        -
                        exp(
                            -_FogDensity
                            *
                            max(
                                totalDistance
                                -
                                10.0,
                                0.0
                            )
                        );


                    color =
                        lerp(
                            color,
                            skyColor *
                            0.4,
                            fog
                        );
                }


                // -----------------------------------------------------
                // Sun glow
                // -----------------------------------------------------

                float finalGlow =
                    pow(
                        saturate(
                            dot(
                                rayDirection,
                                sunDirection
                            )
                        ),
                        8.0
                    );


                color +=
                    sunColor
                    *
                    finalGlow
                    *
                    1.5;


                // -----------------------------------------------------
                // Tonemapping
                // -----------------------------------------------------

                float3 toneMapped =
                    1.0
                    -
                    (4.0 / 27.0)
                    /
                    max(
                        color *
                        color,
                        0.0001
                    );


                color =
                    lerp(
                        color,
                        toneMapped,
                        step(
                            2.0 / 3.0,
                            color
                        )
                    );


                // Gamma correction
                color =
                    pow(
                        max(
                            color,
                            0.0
                        ),
                        1.0 / 2.2
                    );


                return
                    half4(
                        color,
                        1.0
                    );
            }

            ENDHLSL
        }
    }
}