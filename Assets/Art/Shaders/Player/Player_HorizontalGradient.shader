Shader "Custom/Player/Horizontal Gradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Toggle] _Vertical ("Vertical Gradient", Float) = 0
        [IntRange] _ColorCount ("Colour Count", Range(2, 8)) = 2
        _EmissionIntensity ("Emission Intensity", Range(0, 20)) = 1

        [HDR] _Color0 ("Colour 1", Color) = (1, 1, 1, 1)
        _Color0Position ("Colour 1 Position", Range(0, 1)) = 0
        [HDR] _Color1 ("Colour 2", Color) = (0, 0, 0, 1)
        _Color1Position ("Colour 2 Position", Range(0, 1)) = 1
        [HDR] _Color2 ("Colour 3", Color) = (0, 0, 0, 1)
        _Color2Position ("Colour 3 Position", Range(0, 1)) = 1
        [HDR] _Color3 ("Colour 4", Color) = (0, 0, 0, 1)
        _Color3Position ("Colour 4 Position", Range(0, 1)) = 1
        [HDR] _Color4 ("Colour 5", Color) = (0, 0, 0, 1)
        _Color4Position ("Colour 5 Position", Range(0, 1)) = 1
        [HDR] _Color5 ("Colour 6", Color) = (0, 0, 0, 1)
        _Color5Position ("Colour 6 Position", Range(0, 1)) = 1
        [HDR] _Color6 ("Colour 7", Color) = (0, 0, 0, 1)
        _Color6Position ("Colour 7 Position", Range(0, 1)) = 1
        [HDR] _Color7 ("Colour 8", Color) = (0, 0, 0, 1)
        _Color7Position ("Colour 8 Position", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags
            {
                "LightMode" = "Universal2D"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color : COLOR;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float _EmissionIntensity;
                half4 _Color0;
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                half4 _Color4;
                half4 _Color5;
                half4 _Color6;
                half4 _Color7;
                float _Vertical;
                float _ColorCount;
                float _Color0Position;
                float _Color1Position;
                float _Color2Position;
                float _Color3Position;
                float _Color4Position;
                float _Color5Position;
                float _Color6Position;
                float _Color7Position;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color * unity_SpriteColor;

                return output;
            }

            half4 BlendStop(
                half4 previousColor,
                half4 nextColor,
                float previousPosition,
                float nextPosition,
                float position)
            {
                float width = max(nextPosition - previousPosition, 0.0001);
                float blend = saturate((position - previousPosition) / width);
                return lerp(previousColor, nextColor, blend);
            }

            half4 EvaluateGradient(float position)
            {
                half4 color = _Color0;

                if(_ColorCount > 1.5) color = BlendStop(color, _Color1, _Color0Position, _Color1Position, position);
                if(_ColorCount > 2.5) color = BlendStop(color, _Color2, _Color1Position, _Color2Position, position);
                if(_ColorCount > 3.5) color = BlendStop(color, _Color3, _Color2Position, _Color3Position, position);
                if(_ColorCount > 4.5) color = BlendStop(color, _Color4, _Color3Position, _Color4Position, position);
                if(_ColorCount > 5.5) color = BlendStop(color, _Color5, _Color4Position, _Color5Position, position);
                if(_ColorCount > 6.5) color = BlendStop(color, _Color6, _Color5Position, _Color6Position, position);
                if(_ColorCount > 7.5) color = BlendStop(color, _Color7, _Color6Position, _Color7Position, position);

                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 sprite = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float gradientPosition =
                lerp(input.uv.x, input.uv.y, step(0.5, _Vertical));

                half4 gradient =
                EvaluateGradient(saturate(gradientPosition));

                half4 result = sprite * input.color * gradient;

                // Boost RGB into HDR range, but don't modify transparency.
                result.rgb *= _EmissionIntensity;

                return result;
            }
            ENDHLSL
        }
    }
}