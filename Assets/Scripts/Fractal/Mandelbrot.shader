Shader "Custom/Mandelbrot"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Position("Position", vector) = (0,0, 1, 1) // x,y,zoom 
		_Rotation("Rotation", vector) = (1,0,0,0) // cos, sin
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:Setup

			
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
			float4 _Position;
			float4 _Rotation;
			uniform float4 _MainTex_TexelSize;

			float2 Rotate(float2 v)
			{
				return float2(v.x * _Rotation.x - v.y * _Rotation.y, v.x * _Rotation.y + v.y * _Rotation.x);
			}
		
			float2 Pos(float2 uv)
			{
				return (_Position.xy + Rotate(uv - 0.5) * _Position.zw);
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float count;
				float2 z = 0;
				float2 c = Pos(i.uv);
				
				for(count = 0; count < _MainTex_TexelSize.z; ++count)
				{
					z = float2(z.x*z.x-z.y*z.y, 2*z.x*z.y) + c;
					if(length(z)>2)
						break;
				}
				count *= _MainTex_TexelSize.x;
				fixed4 col = count;// tex2D (_MainTex, float2(count,0.5));
				return col;
			}
			ENDCG
		}
	}
}
