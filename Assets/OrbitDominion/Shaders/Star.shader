Shader "Orbit Dominion/Star"
{
 Properties { _ColorA("Core",Color)=(1,.72,.24,1) _ColorB("Granulation",Color)=(1,.2,.03,1) _Seed("Seed",Float)=1 _Intensity("Intensity",Float)=3 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass {
  HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  CBUFFER_START(UnityPerMaterial)
  float4 _ColorA,_ColorB;float _Seed,_Intensity;
  CBUFFER_END
  struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 local:TEXCOORD0;float3 n:TEXCOORD1;float3 world:TEXCOORD2;};
  V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz*2;o.n=TransformObjectToWorldNormal(i.n);o.world=TransformObjectToWorld(i.p.xyz);return o;}
  float hash(float3 p){p=frac(p*.3183099+float3(.13,.27,.41));p*=17;return frac(p.x*p.y*p.z*(p.x+p.y+p.z));}
  float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
  float fbm(float3 p){float v=0,a=.55;for(int j=0;j<4;j++){v+=noise(p)*a;p=p*2.07+9.3;a*=.48;}return v;}
  half4 frag(V i):SV_Target{
   float3 p=normalize(i.local);float time=_Time.y*.025;
   float cells=fbm(p*13+float3(time,-time*.7,_Seed));
   float flow=fbm(p*4.5+float3(-time*.25,_Seed,time*.3));
   float spots=smoothstep(.72,.9,fbm(p*7+float3(_Seed*3,18,time*.08)));
   float3 view=normalize(GetCameraPositionWS()-i.world);float limb=pow(saturate(dot(normalize(i.n),view)),.28);
   float3 color=lerp(_ColorB.rgb,_ColorA.rgb,saturate(cells*.72+flow*.42));color*=lerp(1,.36,spots*.48);
   color*=lerp(.58,1,limb)*_Intensity;return half4(color,1);
  }
  ENDHLSL
 }
 }
}
