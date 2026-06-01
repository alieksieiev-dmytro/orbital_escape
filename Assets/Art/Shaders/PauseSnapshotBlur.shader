Shader "Hidden/OrbitalEscape/UI/PauseSnapshotBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Direction ("Direction", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Direction;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 offset = _Direction.xy * _MainTex_TexelSize.xy;

                fixed4 color = tex2D(_MainTex, input.texcoord) * 0.2270270270;
                color += tex2D(_MainTex, input.texcoord + offset * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, input.texcoord - offset * 1.3846153846) * 0.3162162162;
                color += tex2D(_MainTex, input.texcoord + offset * 3.2307692308) * 0.0702702703;
                color += tex2D(_MainTex, input.texcoord - offset * 3.2307692308) * 0.0702702703;

                return color;
            }
            ENDCG
        }
    }
}
