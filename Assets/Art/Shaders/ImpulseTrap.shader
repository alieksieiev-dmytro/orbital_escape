Shader "Custom/ImpulseTrap"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" { }
        _Color("Color", Color) = (1, 1, 1, 1)
        _GlowSpeed("Glow Speed", Float) = 1.0
        _NeonZoneHeight("Neon Zone Height", Float) = 0.1
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

            struct v2f
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4  _Color;
                float  _GlowSpeed;
                float  _NeonZoneHeight;
            CBUFFER_END

            v2f vert(Attributes IN)
            {
                v2f OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(v2f IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                float y = IN.uv.y;

                float time = _Time.y;
                float pulse = sin(time * _GlowSpeed) * 0.5 + 0.5;

                float alphaExponent = lerp(2.0, 4.0, pulse);
                float alpha = pow(1.0 - y, alphaExponent);

                float localY = saturate(y / _NeonZoneHeight);

                float centerDistance = abs(localY - 0.5) * 2.0;
                float coreMask = 1.0 - saturate(centerDistance);
                float glowMask = pow(coreMask, 1.5);

                float3 baseColor = color.rgb;
                float3 hotColor = float3(1.0, 1.0, 1.0);

                color.rgb = lerp(baseColor, hotColor, glowMask);
                color.rgb += baseColor * glowMask * lerp(0.5, 1.0, pulse);

                color.a = saturate(alpha + glowMask * 0.35);

                return color;
            }
            ENDHLSL
        }
    }
}