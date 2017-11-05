Shader "Custom/EntityShader" {
Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model  removed : addshadow fullforwardshadows
        #pragma surface surf Standard noshadow 
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:setup

        sampler2D _MainTex;
		uniform float4 _MainTex_TexelSize; 

        struct Input {
            float2 uv_MainTex;
			float4 screenPos;
        };

    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        StructuredBuffer<float4> positionBuffer;
    #endif
	

        void rotate2D(inout float2 v, float r)
        {
            float s, c;
            sincos(r, s, c);
            v = float2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        void setup()
        {
        #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            float4 data = positionBuffer[unity_InstanceID];

            float rotation = data.w * data.w * _Time.y * 0.5f;
            rotate2D(data.xz, rotation);

            unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
            unity_ObjectToWorld._12_22_32_42 = float4(0, data.w, 0, 0);
            unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);
			float4 pos = float4(data.xyz,1);
			float x = _Time.y;
			pos.y = (pos.y / pow(log(x*x + 2.718281828459045),1.1)) * 0.25 * abs(sin(x *10 + unity_InstanceID));
            unity_ObjectToWorld._14_24_34_44 = pos;
            unity_WorldToObject = unity_ObjectToWorld;
            unity_WorldToObject._14_24_34 *= -1;
            unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
        #endif
        }

        half _Glossiness;
        half _Metallic;
		
        void surf (Input IN, inout SurfaceOutputStandard o) {
			
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
			//clip(c.a - 0.1);
			

			//int xOdd = ((int)(IN.screenPos.x*_ScreenParams.x))&1;
			//int yOdd = ((int)(IN.screenPos.y*_ScreenParams.y))&1;
			//clip( (xOdd^yOdd) -0.1);

            o.Albedo = float3(IN.screenPos.xy, 0);
            o.Metallic = 0;// _Metallic;
            o.Smoothness = 0;//_Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}