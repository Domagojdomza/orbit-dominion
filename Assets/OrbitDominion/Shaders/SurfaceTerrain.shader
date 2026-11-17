Shader "Orbit Dominion/SurfaceTerrain"
{
 Properties {
  _Seed("Variation", Float) = 1
  _WaterLevel("Water level", Float) = 0
  _Wetness("Shore wetness", Range(0,1)) = .45
  _DetailAlbedo("Photoreal detail",2D)="gray" {}
  _DetailNormal("Photoreal normal",2D)="bump" {}
  _DetailScale("Detail metres",Float)=3.25
  _DetailStrength("Detail strength",Range(0,1))=.7
  _NormalStrength("Normal strength",Range(0,2))=.62
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
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float _Seed,_WaterLevel,_Wetness,_DetailScale,_DetailStrength,_NormalStrength;
   CBUFFER_END
   TEXTURE2D(_DetailAlbedo); SAMPLER(sampler_DetailAlbedo);
   TEXTURE2D(_DetailNormal); SAMPLER(sampler_DetailNormal);
   struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct V
   {
    float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 world:TEXCOORD1;
    float3 local:TEXCOORD2; float2 uv:TEXCOORD3; float fog:TEXCOORD4; float4 color:COLOR;
   };
   V vert(A v)
   {
    V o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz);
    o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.world=TransformObjectToWorld(v.positionOS.xyz);
    o.local=v.positionOS.xyz; o.uv=v.uv; o.color=v.color; o.fog=ComputeFogFactor(o.positionCS.z); return o;
   }
   float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
   float noise(float2 p)
   {
    float2 n=floor(p),f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(hash(n),hash(n+float2(1,0)),f.x),lerp(hash(n+float2(0,1)),hash(n+1),f.x),f.y);
   }
   float terrainNoise(float2 p)
   {
    return noise(p*.19+_Seed)*.55+noise(p*.73-_Seed*2.1)*.28+noise(p*2.9+_Seed*4.3)*.17;
   }
   half4 frag(V i):SV_Target
   {
    float grain=terrainNoise(i.uv);
    float2 texel=float2(.12,0);
    float dx=terrainNoise(i.uv+texel)-terrainNoise(i.uv-texel);
    float dz=terrainNoise(i.uv+texel.yx)-terrainNoise(i.uv-texel.yx);
    float slope=1-saturate(i.normalWS.y);
    float2 detailUv=i.local.xz/max(.25,_DetailScale);
    float3 detailAlbedo=SAMPLE_TEXTURE2D(_DetailAlbedo,sampler_DetailAlbedo,detailUv).rgb;
    float3 detailNormal=UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormal,sampler_DetailNormal,detailUv),_NormalStrength);
    float3 normal=normalize(i.normalWS+float3(-dx+detailNormal.x*.28,0,-dz+detailNormal.y*.28)*lerp(.7,.24,slope));
    float mineral=noise(i.uv*5.7+_Seed*11);
    float3 color=i.color.rgb*lerp(.83,1.13,grain);
    float detailLuma=dot(detailAlbedo,float3(.2126,.7152,.0722));
    color*=lerp(1,lerp(.58,1.42,detailLuma),_DetailStrength);
    color=lerp(color,color*float3(.72,.75,.78),smoothstep(.82,.97,mineral)*(.2+slope*.35));
    float shoreline=1-smoothstep(_WaterLevel+.15,_WaterLevel+2.2,i.local.y);
    color=lerp(color,color*.58,shoreline*_Wetness);
    float4 shadowCoord=TransformWorldToShadowCoord(i.world);
    Light sun=GetMainLight(shadowCoord);
    float diffuse=saturate(dot(normal,sun.direction));
    float3 ambient=SampleSH(normal)*color;
    float3 view=normalize(GetCameraPositionWS()-i.world);
    float roughness=lerp(.9,.48,shoreline*_Wetness);
    float specular=pow(saturate(dot(reflect(-sun.direction,normal),view)),lerp(18,70,1-roughness));
    float occlusion=lerp(.7,1,saturate(grain+slope*.22));
    float3 lit=color*sun.color*(.12+diffuse*sun.shadowAttenuation)*occlusion+ambient*.68;
    lit+=sun.color*specular*shoreline*_Wetness*.32;
    lit=MixFog(lit,i.fog);
    return half4(lit,1);
   }
   ENDHLSL
  }
 }
}
