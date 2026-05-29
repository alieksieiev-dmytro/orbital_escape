Shader "Custom/ImpulseTrap"
{
    Properties
    {
        _MainTex("Main Texture", 2D) = "white" { }
        _Color("Color", Color) = (1, 1, 1, 1)
        _GlowSpeed("Glow Speed", Float) = 1.0
        _NeonZoneHeight("Neon Zone Height", Float) = 0.1

        _SparkSpeed("Spark Speed", Float) = 3.0
        _SparkHeight("Spark Max Height", Float) = 0.35
        _SparkIntensity("Spark Intensity", Float) = 1.5
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

                float _SparkSpeed;
                float _SparkHeight;
                float _SparkIntensity;
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

                float x = IN.uv.x;
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

                float columnCount = 14.0;
                float column = floor(x * columnCount);

                float baseHash = frac(sin(column * 37.13) * 43758.5453);

                float sparkProgress = frac(time * _SparkSpeed + baseHash);
                float sparkCycle = floor(time * _SparkSpeed + baseHash);

                float spawnSeed = column * 91.73 + sparkCycle * 157.31;
                float spawnHash = frac(sin(spawnSeed) * 43758.5453);
                float heightHash = frac(sin(spawnSeed + 13.7) * 24634.6345);
                float widthHash = frac(sin(spawnSeed + 41.2) * 19421.1231);

                float sparkHeight = max(0.05, heightHash * _SparkHeight);
                float sparkWidth = lerp(0.015, 0.045, widthHash);

                float columnWidth = 1.0 / columnCount;
                float randomOffset = lerp(0.2, 0.8, spawnHash);
                float columnCenter = (column + randomOffset) * columnWidth;

                float horizontalDistance = abs(x - columnCenter);
                float sparkShape = 1.0 - saturate(horizontalDistance / sparkWidth);
                sparkShape = pow(sparkShape, 3.0);

                float sparkY = saturate((y - _NeonZoneHeight) / max(1.0 - _NeonZoneHeight, 0.0001));

                float verticalMask = 1.0 - saturate(sparkY / max(sparkHeight, 0.0001));
                verticalMask *= step(sparkY, sparkHeight * sparkProgress);
                verticalMask *= step(_NeonZoneHeight, y);

                float sparkMask = sparkShape * verticalMask;
                sparkMask *= 1.0 - sparkProgress;

                color.rgb += float3(1.0, 1.0, 1.0) * sparkMask * _SparkIntensity;

                color.a = saturate(alpha + glowMask * 0.35 + sparkMask);

                return color;
            }
            ENDHLSL
        }
    }
}