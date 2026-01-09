Shader "Custom/CircleShader"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
        _ThicknessWS("Ring Thickness (world units)", Float) = 0.5
        _EdgeSoftness("Edge Softness", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "DisableBatching"="True"
            "RenderPipeline"="UniversalPipeline"            
        }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                float2 posWS : TEXCOORD0;
            };

            float4 _Color;
            float _ThicknessWS;
            float _EdgeSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = TransformObjectToWorld(v.vertex.xyz);
                o.posCS = TransformObjectToHClip(v.vertex.xyz);
                o.posWS = worldPos.xy;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 centerWS = float2(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13);

                float sx = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
                float sy = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));

                float R = 0.5 * min(sx, sy) - (_ThicknessWS * 0.5);
                float distance = length(i.posWS - centerWS);
                float sdf = abs(distance - R) - (_ThicknessWS * 0.5);
                float aa = max(fwidth(distance), 1e-4) * _EdgeSoftness;
                float alpha = saturate(0.5 - sdf / (2.0 * aa));

                float4 col = _Color;
                col.a *= alpha;
                col.rgb *= col.a;
                return col;
            }
            ENDHLSL
        }
    }
}
