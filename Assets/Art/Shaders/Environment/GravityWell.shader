Shader "Custom/GravityWellArc"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" { }
        _Color("Tint", Color) = (1, 1, 1, 1)

        _MinAlpha("Minimum Alpha", Range(0, 1)) = 0.05
        _MaxAlpha("Maximum Alpha", Range(0, 1)) = 0.85

        _PulseCount("Pulse Count", Float) = 2.5
        _PulseSpeed("Pulse Speed", Float) = 0.45
        _PulseSharpness("Pulse Sharpness", Float) = 1.4
        _PulseStrength("Pulse Strength", Range(0, 2)) = 1.0
        _InwardAlphaPower("Inward Alpha Power", Float) = 0.75
        _PullDirection("Pull Direction", Float) = -1.0

        _ArcCenter("Arc Center", Vector) = (0, 0, 0, 0)
        _ArcRadius("Arc Radius", Float) = 5.0
        _ArcThickness("Arc Thickness", Float) = 1.0
        _ArcStartAngle("Arc Start Angle Radians", Float) = 0.0
        _ArcAngle("Arc Angle Radians", Float) = 1.5708
        _ArcAngleSoftness("Arc Angle Softness Radians", Float) = 0.0
        _ArcRadiusSoftness("Arc Radius Softness", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
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
                float2 positionWS : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _MinAlpha;
                float _MaxAlpha;

                float _PulseCount;
                float _PulseSpeed;
                float _PulseSharpness;
                float _PulseStrength;
                float _InwardAlphaPower;
                float _PullDirection;

                float4 _ArcCenter;
                float _ArcRadius;
                float _ArcThickness;
                float _ArcStartAngle;
                float _ArcAngle;
                float _ArcAngleSoftness;
                float _ArcRadiusSoftness;
            CBUFFER_END

            float HardOrSoftRangeMask(float value, float minValue, float maxValue, float softness)
            {
                if (softness <= 0.00001)
                {
                    return step(minValue, value) * step(value, maxValue);
                }

                float innerMask = smoothstep(minValue, minValue + softness, value);
                float outerMask = 1.0 - smoothstep(maxValue - softness, maxValue, value);

                return innerMask * outerMask;
            }

            float HardOrSoftAngleMask(float angleDistance, float halfArcAngle, float softness)
            {
                if (softness <= 0.00001)
                {
                    return step(angleDistance, halfArcAngle);
                }

                return 1.0 - smoothstep(max(0.0, halfArcAngle - softness), halfArcAngle, angleDistance);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                output.positionWS = positionWS.xy;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * unity_SpriteColor * _Color;

                float2 fromCenter = input.positionWS - _ArcCenter.xy;
                float radius = length(fromCenter);
                float angle = atan2(fromCenter.y, fromCenter.x);

                float arcCenterAngle = _ArcStartAngle + _ArcAngle * 0.5;
                float angleDelta = atan2(sin(angle - arcCenterAngle), cos(angle - arcCenterAngle));
                float angleDistance = abs(angleDelta);

                float halfArcAngle = _ArcAngle * 0.5;
                float angleMask = HardOrSoftAngleMask(angleDistance, halfArcAngle, _ArcAngleSoftness);

                float innerRadius = _ArcRadius - _ArcThickness * 0.5;
                float outerRadius = _ArcRadius + _ArcThickness * 0.5;
                float radiusMask = HardOrSoftRangeMask(radius, innerRadius, outerRadius, _ArcRadiusSoftness);

                float arcMask = angleMask * radiusMask;

                float radialT = saturate((radius - innerRadius) / max(_ArcThickness, 0.0001));
                float pullSign = _PullDirection >= 0.0 ? 1.0 : -1.0;

                float phase = radialT * _PulseCount - _Time.y * _PulseSpeed * pullSign;

                float layerA = 0.5 + 0.5 * sin((phase + 0.00) * TWO_PI);
                float layerB = 0.5 + 0.5 * sin((phase + 0.23) * TWO_PI);
                float layerC = 0.5 + 0.5 * sin((phase + 0.47) * TWO_PI);

                layerA = pow(layerA, _PulseSharpness);
                layerB = pow(layerB, _PulseSharpness * 1.35);
                layerC = pow(layerC, _PulseSharpness * 0.75);

                float layeredField = layerA * 0.45 + layerB * 0.35 + layerC * 0.25;
                layeredField = saturate(layeredField * _PulseStrength);

                float pullAccumulationT = pullSign > 0.0 ? radialT : 1.0 - radialT;
                float inwardAccumulation = pow(pullAccumulationT, _InwardAlphaPower);

                float fieldStrength = saturate(layeredField * inwardAccumulation);

                float alphaMultiplier = lerp(_MinAlpha, _MaxAlpha, fieldStrength);

                color.a *= alphaMultiplier * arcMask;

                return color;
            }

            ENDHLSL
        }
    }
}
