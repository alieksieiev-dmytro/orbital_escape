Shader "Custom/Aurora"
{
    Properties
    {
        _CenterColor("Center Color", Color) = (1,1,1,1)
        _EdgeColor("Edge Color", Color) = (0,1,1,1)
        _GradientPower("Gradient Power", Range(0.25, 32.0)) = 1.0

        _Softness("Edge Softness", Range(0,0.25)) = 0.01

        _FlowSpeed("Flow Speed", Range(-2.0, 2.0)) = 0.35
        _StreakFreq("Streak Frequency", Range(1.0, 40.0)) = 18.0
        _StreakSharpness("Streak Sharpness", Range(0.5, 12.0)) = 5.0
        _StreakAmount("Streak Amount", Range(0.0, 2.0)) = 0.6

        _WarpAmount("Warp Amount", Range(0.0, 0.25)) = 0.06
        _WarpFreq("Warp Frequency", Range(0.25, 6.0)) = 1.8
        _WarpSpeed("Warp Speed", Range(-2.0, 2.0)) = 0.45

        _BandAmount("Band Amount", Range(0.0, 2.0)) = 0.35
        _BandFreq("Band Frequency", Range(0.5, 30.0)) = 7.0
        _BandSpeed("Band Speed", Range(-5.0, 5.0)) = 1.2

        _Emission("Emission", Range(0.0, 8.0)) = 1.0
        _EmissionFromBands("Emission From Bands", Range(0.0, 1.0)) = 0.75

        _AuroraAngle("Aurora Angle", Range(0.0, 360.0)) = 0.0

        _AuroraHeight("Aurora Height Center", Range(0.0, 1.0)) = 0.65
        _AuroraHeightWidth("Aurora Height Width", Range(0.01, 1.0)) = 0.55
        _AuroraHeightSoftness("Aurora Height Softness", Range(0.001, 0.5)) = 0.12
        _AuroraMaskStrength("Aurora Mask Strength", Range(0.0, 1.0)) = 0.9
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "UniversalMaterialType"="Unlit"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Tags { "LightMode"="Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float4 _CenterColor;
                float4 _EdgeColor;
                float _GradientPower;
                float _Softness;

                float _FlowSpeed;
                float _StreakFreq;
                float _StreakSharpness;
                float _StreakAmount;

                float _WarpAmount;
                float _WarpFreq;
                float _WarpSpeed;

                float _BandAmount;
                float _BandFreq;
                float _BandSpeed;

                float _Emission;
                float _EmissionFromBands;

                float _AuroraAngle;

                float _AuroraHeight;
                float _AuroraHeightWidth;
                float _AuroraHeightSoftness;
                float _AuroraMaskStrength;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float2 Rotate2D(float2 v, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float2(c * v.x - s * v.y, s * v.x + c * v.y);
            }

            float SoftBandMask(float v, float center, float halfWidth, float softness)
            {
                float d = abs(v - center);
                float inner = halfWidth;
                float outer = halfWidth + max(softness, 1e-5);
                return 1.0 - smoothstep(inner, outer, d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.uv * 2.0 - 1.0;
                float r = length(p);

                float edge0 = 1.0;
                float edge1 = 1.0 - max(_Softness, 1e-5);
                float alpha = 1.0 - smoothstep(edge1, edge0, r);

                float t = saturate(r);
                t = pow(max(t, 1e-5), _GradientPower);

                float3 baseRgb = lerp(_CenterColor.rgb, _EdgeColor.rgb, t);
                float baseA = lerp(_CenterColor.a, _EdgeColor.a, t) * alpha;

                float time = _Time.y;

                float radians = _AuroraAngle * 0.01745329252;
                float2 uvCentered = input.uv - 0.5;
                float2 uvRot = Rotate2D(uvCentered, radians) + 0.5;

                float h = uvRot.y;

                float auroraMask = SoftBandMask(h, _AuroraHeight, _AuroraHeightWidth * 0.5, _AuroraHeightSoftness);
                auroraMask = lerp(1.0, auroraMask, saturate(_AuroraMaskStrength));

                float warpN = ValueNoise(float2(h * _WarpFreq, time * _WarpSpeed));
                float warp = (warpN - 0.5) * 2.0 * _WarpAmount;

                float2 nUv = float2((uvRot.x + warp) * _StreakFreq, h * 2.0 + time * _FlowSpeed);

                float n = ValueNoise(nUv);
                n = pow(saturate(n), _StreakSharpness);

                float streakMul = 1.0 + (n - 0.5) * 2.0 * _StreakAmount;

                float bandPhase = (h * _BandFreq - time * _BandSpeed) * 6.2831853;
                float bands = 0.5 + 0.5 * sin(bandPhase);
                float bandMul = 1.0 + (bands - 0.5) * 2.0 * _BandAmount;

                float3 litRgb = baseRgb * max(streakMul, 0.0);

                float emissionMul = lerp(1.0, max(bandMul, 0.0), saturate(_EmissionFromBands));
                float3 auroraRgb = litRgb + litRgb * (_Emission * emissionMul - 1.0);

                float3 rgb = lerp(baseRgb, auroraRgb, auroraMask);

                return half4(rgb, baseA);
            }
            ENDHLSL
        }
    }
}
