Shader "Custom/URP2D/SpriteRadialGradient"
{
    Properties
    {
        [PerRendererData] [NoScaleOffset] _MainTex("Sprite Texture", 2D) = "white" {}
        _CenterColor("Center Color", Color) = (1, 1, 1, 1)
        _EdgeColor("Edge Color", Color) = (1, 1, 1, 0)
        _Center("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius("Radius", Range(0.01, 1.5)) = 1
        _Falloff("Falloff", Range(0.1, 8)) = 1
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
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _CenterColor;
                half4 _EdgeColor;
                float4 _Center;
                float _Radius;
                float _Falloff;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float radialDistance = length((input.uv - _Center.xy) * 2.0);
                float gradientPosition = pow(saturate(radialDistance / max(_Radius, 0.0001)), _Falloff);
                half4 gradientColor = lerp(_CenterColor, _EdgeColor, gradientPosition);

                half4 output = gradientColor * input.color;
                output.a *= sprite.a;
                return output;
            }
            ENDHLSL
        }
    }
}
