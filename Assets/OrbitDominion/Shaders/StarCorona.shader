Shader "Orbit Dominion/StarCorona"
{
 Properties { _Color("Corona",Color)=(1,.45,.08,1) _Seed("Seed",Float)=1 _Intensity("Intensity",Float)=1 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha One ZWrite Off Cull Front
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial)
  float4 _Color;float _Seed,_Intensity;
  CBUFFER_END
  struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 local:TEXCOORD0;float3 n:TEXCOORD1;float3 world:TEXCOORD2;};
  V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz*2;o.n=TransformObjectToWorldNormal(i.n);o.world=TransformObjectToWorld(i.p.xyz);return o;}
  float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
  float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
  half4 frag(V i):SV_Target{
   float3 view=normalize(GetCameraPositionWS()-i.world);
   // The photosphere occludes the shell centre. Fade the exposed shell to zero
   // at its outer silhouette instead of creating a hard luminous ring there.
   float falloff=pow(saturate(abs(dot(normalize(i.n),view))),2.4);
   float activity=lerp(.72,1.15,noise(normalize(i.local)*18+float3(_Seed,_Time.y*.07,0)));
   float alpha=saturate(falloff*activity*.68*_Intensity);return half4(_Color.rgb*_Intensity,alpha);
  }
  ENDHLSL
 }
 }
}
