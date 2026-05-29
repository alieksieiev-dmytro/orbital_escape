Shader "Custom/URP2D/CircleGradientPulse"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Sprite Texture", 2D) = "white" {}
        _CenterColor("Center Color", Color) = (1,1,1,1)
        _EdgeColor("Edge Color", Color) = (0,0,0,1)
        _GradientThickness("Gradient Thickness", Range(0,1)) = 1
        _PulseAmplitude("Pulse Amplitude", Range(0,1)) = 0.15
        _PulseSpeed("Pulse Speed", Range(0,20)) = 3
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _CenterColor;
            float4 _EdgeColor;
            float _GradientThickness;
            float _PulseAmplitude;
            float _PulseSpeed;

            v2f Vert(Attributes input)
            {
                v2f output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float4 Frag(v2f input) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float tSec = _Time.y;

                float pulse01 = 0.5 + 0.5 * sin(tSec * _PulseSpeed);
                float thicknessPulsed = saturate(_GradientThickness + (pulse01 - 0.5) * 2.0 * _PulseAmplitude);

                float2 p = input.uv * 2.0 - 1.0;
                float r = saturate(length(p));

                float denom = max(1e-4, 1.0 - thicknessPulsed);
                float a = thicknessPulsed / denom;
                float g = pow(r, a);

                float4 grad = lerp(_CenterColor, _EdgeColor, g);

                float4 outColor = grad * input.color;
                outColor.a *= tex.a;
                return outColor;
            }
            ENDHLSL
        }
    }
}
