    Shader "Instanced/InstancedShader" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
    }
    SubShader {

        Pass {

            Tags {"LightMode"="ForwardBase"}

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
			

        #if SHADER_TARGET >= 45
            StructuredBuffer<float4> positionBuffer;
        #endif

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv_MainTex : TEXCOORD0;
            };

            void rotate2D(inout float2 v, float r)
            {
                float s, c;
                sincos(r, s, c);
                v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
            }

            v2f vert (appdata_full v, uint instanceID : SV_InstanceID)
            {
            #if SHADER_TARGET >= 45
                float4 data = positionBuffer[instanceID];
            #else
                float4 data = 0;
            #endif

                float3 localPosition = v.vertex.xyz;
                float3 worldPosition = data.xyz + localPosition;
                float3 worldNormal = v.normal;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));
                o.uv_MainTex = v.texcoord;
                return o;
            }
			/*
			{
				v2f o;
				float4 ori=mul(UNITY_MATRIX_MV,float4(0,0,0,1));
				float4 vt=v.vertex;
				vt.y=vt.z;
				vt.z=0;
				vt.xyz+=ori.xyz;//result is vt.z==ori.z ,so the distance to camera keeped ,and screen size keeped
				o.pos=mul(UNITY_MATRIX_P,vt);
 
				o.texc=v.texcoord;
				return o;
			}
			*/

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, i.uv_MainTex);
				clip(albedo.w - 0.01);
              
				//int oddx = ((int)i.pos.x)&1;
				//int oddy = ((int)i.pos.y)&1;
				//clip((oddy^oddx) -0.1);

				return albedo;//fixed4(i.pos.xy/_ScreenParams,0,1);
            }

            ENDCG
        }
    }
}