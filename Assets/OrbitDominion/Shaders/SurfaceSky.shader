Shader "Orbit Dominion/SurfaceSky"
{
 Properties {
  _Horizon("Horizon",Color)=(.6,.75,.8,1) _Zenith("Zenith",Color)=(.15,.4,.65,1)
  _Cloud("Cloud",Color)=(.95,.95,.9,1) _Seed("Variation",Float)=1 _CloudAmount("Clouds",Range(0,1))=.3
 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Opaque" }
  Pass
  {
   Cull Front ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Horizon,_Zenith,_Cloud; float _Seed,_CloudAmount;
   CBUFFER_END
   struct A { float4 positionOS:POSITION; };
   struct V { float4 positionCS:SV_POSITION; float3 direction:TEXCOORD0; };
   V vert(A v) { V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.direction=v.positionOS.xyz; return o; }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p)
   {
    float2 n=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(hash(n),hash(n+float2(1,0)),f.x),lerp(hash(n+float2(0,1)),hash(n+1),f.x),f.y);
   }
   half4 frag(V i):SV_Target
   {
    float3 d=normalize(i.direction);
    float h=saturate(d.y*1.2+.12);
    float3 color=lerp(_Horizon.rgb,_Zenith.rgb,pow(h,.72));
    color+=_Horizon.rgb*pow(1-saturate(abs(d.y)),7)*.18;
    float2 p=d.xz/max(.18,d.y+.42);
    float n=noise(p*3+_Seed+_Time.y*.005)*.58+noise(p*8+_Seed*2+_Time.y*.012)*.3+noise(p*18)*.12;
    float clouds=smoothstep(.59,.74,n)*_CloudAmount*smoothstep(-.04,.2,d.y);
    float silver=pow(saturate(n-.5)*2.1,3)*clouds;
    color=lerp(color,_Cloud.rgb,clouds*.86)+_Cloud.rgb*silver*.12;
    float sunDirection=saturate(dot(d,normalize(float3(.58,.36,.72))));
    float sun=pow(sunDirection,520),halo=pow(sunDirection,22);
    color+=float3(1,.78,.49)*(sun*1.8+halo*.085)*(1-clouds*.55);
    return half4(color,1);
   }
   ENDHLSL
  }
 }
}
