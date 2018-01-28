Shader "Custom/Mandelbrot"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_MaxDistance("Max Distance", float) = 2
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			uniform float4 _MainTex_TexelSize;
			float4x4 _rTW;
			float _MaxDistance;
		
			float2 Pos(float2 uv)
			{
				return mul(_rTW, float4(uv,0,1)).xy;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float count;
				float2 z = 0;

				float2 c = Pos(i.uv);
				for(count = 0; count < _MainTex_TexelSize.z; ++count)
				{
					z = float2(z.x*z.x-z.y*z.y, 2*z.x*z.y) + c;
					if(length(z)>_MaxDistance)
						break;
				}
				count *= _MainTex_TexelSize.x;
				return tex2D (_MainTex, float2(count, 0.5));
			}
			ENDCG
		}
	}
}
