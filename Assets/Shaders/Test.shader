// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Simplified VertexLit shader, optimized for high-poly meshes. Differences from regular VertexLit one:
// - less per-vertex work compared with Mobile-VertexLit
// - supports only DIRECTIONAL lights and ambient term, saves some vertex processing power
// - no per-material color
// - no specular
// - no emission

Shader "Custom/Test" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
		_Displacement ("Base (RGB)", 2D) = "white" {}
		_Shininess ("Shininess", Float) = 0.078125
		_displacementFactor("Factor", Float) = 0.5
		_speed("Speed", Float) = 1
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 80

    Pass {
        Name "FORWARD"
        Tags { "LightMode" = "ForwardBase" }
CGPROGRAM
#pragma vertex vert_surf
#pragma fragment frag_surf
#pragma target 2.0
#pragma multi_compile_fwdbase
#pragma multi_compile_fog
#include "HLSLSupport.cginc"
#include "UnityCG.cginc"
#include "Lighting.cginc"
#include "AutoLight.cginc"

        inline float3 LightingLambertVS (float3 normal, float3 lightDir)
        {
            fixed diff = max (0, dot (normal, lightDir));
            return _LightColor0.rgb * diff;
        }

        sampler2D _MainTex;
		sampler2D _Displacement;
		float _displacementFactor;
		float _speed;

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutput o) {
			float2 uv = IN.uv_MainTex;
			half4 d = tex2D(_Displacement, uv);
			half4 n = tex2D (_MainTex, uv);
			float s = 0.5 * (sin(_Time.y * _speed)+1);
			uv.x += (d.x - 0.5) * _displacementFactor * s;
			uv.y += (d.y - 0.5) * _displacementFactor * s;
            half4 c = tex2D (_MainTex, uv);

            o.Albedo = c.rgb;//lerp(n.rgb, c.rgb, s);
            o.Alpha = c.a;
        }
        struct v2f_surf {
  float4 pos : SV_POSITION;
  float2 pack0 : TEXCOORD0;
  #ifndef LIGHTMAP_ON
  fixed3 normal : TEXCOORD1;
  #endif
  #ifdef LIGHTMAP_ON
  float2 lmap : TEXCOORD2;
  #endif
  #ifndef LIGHTMAP_ON
  fixed3 vlight : TEXCOORD2;
  #endif
  LIGHTING_COORDS(3,4)
  UNITY_FOG_COORDS(5)
  UNITY_VERTEX_OUTPUT_STEREO
};
float4 _MainTex_ST;
float _Shininess;
v2f_surf vert_surf (appdata_full v)
{
    v2f_surf o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(v.vertex);
    o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
    #ifdef LIGHTMAP_ON
    o.lmap.xy = v.texcoord1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
    #endif
    float3 worldN = UnityObjectToWorldNormal(v.normal);
    #ifndef LIGHTMAP_ON
    o.normal = worldN;
    #endif
    #ifndef LIGHTMAP_ON

    o.vlight = ShadeSH9 (float4(worldN,1.0));
    o.vlight += LightingLambertVS (worldN, _WorldSpaceLightPos0.xyz);

    #endif
    TRANSFER_VERTEX_TO_FRAGMENT(o);
    UNITY_TRANSFER_FOG(o,o.pos);
    return o;
}

float rand_1_05(in float2 uv)
{
    float2 noise = (frac(sin(dot(uv ,float2(12.9898,78.233)*2.0)) * 43758.5453));
    return abs(noise.x + noise.y) * 0.5;
}

float2 rand_2_10(in float2 uv) {
    float noiseX = (frac(sin(dot(uv, float2(12.9898,78.233) * 2.0)) * 43758.5453));
    float noiseY = sqrt(1 - noiseX * noiseX);
    return float2(noiseX, noiseY);
}

float2 rand_2_0004(in float2 uv)
{
    float noiseX = (frac(sin(dot(uv, float2(12.9898,78.233)      )) * 43758.5453));
    float noiseY = (frac(sin(dot(uv, float2(12.9898,78.233) * 2.0)) * 43758.5453));
    return float2(noiseX, noiseY) * 0.004;
}

bool RandThreshold(in float f, in v2f_surf IN)
{
 return (f < _Shininess*rand_1_05(IN.pack0.xy));
}

fixed4 frag_surf (v2f_surf IN) : SV_Target
{
    Input surfIN;
    surfIN.uv_MainTex = IN.pack0.xy;
    SurfaceOutput o;
    o.Albedo = 0.0;
    o.Emission = 0.0;
    o.Specular = 0.0;
    o.Alpha = 0.0;
    o.Gloss = 0.0;
    #ifndef LIGHTMAP_ON
    o.Normal = IN.normal;
    #else
    o.Normal = 0;
    #endif
    surf (surfIN, o);
    fixed atten = LIGHT_ATTENUATION(IN);
    fixed4 c = 0;
    #ifndef LIGHTMAP_ON
    c.rgb = o.Albedo * IN.vlight * atten;
	fixed4 base=fixed4(o.Albedo,1);
    #endif
    #ifdef LIGHTMAP_ON
    fixed3 lm = DecodeLightmap (UNITY_SAMPLE_TEX2D(unity_Lightmap, IN.lmap.xy));
    #ifdef SHADOWS_SCREEN
    c.rgb += o.Albedo * min(lm, atten*2);
    #else
    c.rgb += o.Albedo * lm;
    #endif
    c.a = o.Alpha;
    #endif
    UNITY_APPLY_FOG(IN.fogCoord, c);
    UNITY_OPAQUE_ALPHA(c.a);
//	float f = c.r * 0.3 + c.g * 0.59 + c.b * 0.11;
//	float2 pos = IN.pack0.xy + float2(sin(_Time.y), cos(_Time.y));
//	return (f ) < rand_1_05(pos)
//	  ?	f : _Shininess;
	return c;
}

ENDCG
    }
}

FallBack "Mobile/VertexLit"
}
