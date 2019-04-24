Shader "Hidden/ImageWaving"
{

	HLSLINCLUDE

		#include "PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

		float _Intensity;
		float _Frequency;
		float _Speed;

		float4 Frag(VaryingsDefault i) : SV_Target
		{
			return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + float2(sin(i.texcoord.y * _Frequency + _Time[1] * _Speed) * _Intensity, 0));
		}
	ENDHLSL

	SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
