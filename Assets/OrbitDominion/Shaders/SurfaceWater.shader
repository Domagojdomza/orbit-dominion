Shader "Orbit Dominion/SurfaceWater"
{
 Properties {
  _Shallow("Shallows",Color)=(.1,.45,.5,1) _Deep("Deep water",Color)=(.02,.14,.28,1)
  _Seed("Variation",Float)=1 _WaveHeight("Wave height",Float)=.17
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Shallow,_Deep; float _Seed,_WaveHeight;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 uv:TEXCOORD1; float3 normalWS:TEXCOORD2; float fog:TEXCOORD3; float shallow:TEXCOORD4; };
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p)
   {
    float2 n=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(hash(n),hash(n+float2(1,0)),f.x),lerp(hash(n+float2(0,1)),hash(n+1),f.x),f.y);
   }
   V vert(A v)
   {
    V o; float3 p=v.positionOS.xyz;
    float phaseX=p.x*.17+_Time.y*1.1+_Seed,phaseZ=p.z*.23-_Time.y*.8;
    p.y+=_WaveHeight*(sin(phaseX)+sin(phaseZ)*.5);
    float dx=_WaveHeight*.17*cos(phaseX),dz=_WaveHeight*.115*cos(phaseZ);
    o.world=TransformObjectToWorld(p); o.positionCS=TransformWorldToHClip(o.world);
    o.normalWS=TransformObjectToWorldNormal(normalize(float3(-dx,1,-dz)));
    o.uv=v.uv; o.shallow=v.color.a; o.fog=ComputeFogFactor(o.positionCS.z); return o;
   }
   half4 frag(V i):SV_Target
   {
    float variation=noise(i.uv*.035+float2(_Time.y*.018,_Seed));
    float fine=noise(i.uv*.21+float2(_Seed,-_Time.y*.11));
    float fineX=noise((i.uv+float2(.18,0))*.21+float2(_Seed,-_Time.y*.11))-fine;
    float fineZ=noise((i.uv+float2(0,.18))*.21+float2(_Seed,-_Time.y*.11))-fine;
    float3 normal=normalize(i.normalWS+float3(-fineX*.48,0,-fineZ*.48));
    float3 view=normalize(GetCameraPositionWS()-i.world);
    float fresnel=pow(1-saturate(dot(view,normal)),4);
    float3 color=lerp(_Deep.rgb,_Shallow.rgb,saturate(.25+variation*.28+fine*.08+i.shallow*.46));
    Light sun=GetMainLight();
    float sparkle=pow(saturate(dot(reflect(-sun.direction,normal),view)),150);
    float shoreBand=smoothstep(.62,.92,i.shallow)*(1-smoothstep(.93,1,i.shallow));
    float shorefoam=shoreBand*smoothstep(.48,.82,variation+fine*.24);
    float whitecaps=smoothstep(.88,.985,fine)*saturate(_WaveHeight*5);
    float diffuse=.28+saturate(dot(normal,sun.direction))*.42;
    color*=diffuse;
    color=lerp(color,lerp(_Shallow.rgb,float3(.55,.72,.82),.5),fresnel*.46);
    color+=sun.color*(sparkle*.7+shorefoam*.32+whitecaps*.14);
    color=MixFog(color,i.fog);
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
