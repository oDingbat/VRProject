
	/*	Class Created: August 8th, 2017, 12:42pm by Thomas Carrella
	 *	
	 */

Shader "Unlit/ProjectileHighlight"
{
	Properties
	{
		 _Color ("Color", Color) = (1, 1, 1, 1)
		 _ZOffset ("Z Offset", float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float _ZOffset;
			
			v2f vert (appdata_base v)
			{
				v2f o;
				float3 viewDirection = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex));
				o.vertex = UnityObjectToClipPos(v.vertex + (viewDirection * _ZOffset));
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float4 col = _Color;

				return col;
			}
			ENDCG
		}
	}
}