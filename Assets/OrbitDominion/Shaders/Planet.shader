Shader "Orbit Dominion/Planet"
{
 Properties {
 _Ocean("Ocean", Color)=(.01,.06,.15,1) _Land("Terrain",Color)=(.12,.28,.16,1)
 _Highlands("Highlands",Color)=(.35,.42,.3,1) _LandThreshold("Land threshold",Float)=.52
 _Clouds("Cloud cover",Float)=.6 _Cities("Cities",Float)=0 _Seed("Seed",Float)=1
 _Ice("Polar ice",Float)=1 _Lava("Lava emission",Float)=0 _CloudTint("Cloud tint",Color)=(.82,.89,.96,1) _RimTint("Rim tint",Color)=(.06,.25,.5,1)
 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass { HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Ocean,_Land,_Highlands,_CloudTint,_RimTint; float _LandThreshold,_Clouds,_Cities,_Seed,_Ice,_Lava;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
 struct V { float4 pos:SV_POSITION; float3 local:TEXCOORD0; float3 normal:TEXCOORD1; float3 world:TEXCOORD2; };
 V vert(A i) { V o; o.pos=TransformObjectToHClip(i.positionOS.xyz); o.local=i.positionOS.xyz*2; o.normal=TransformObjectToWorldNormal(i.normalOS); o.world=TransformObjectToWorld(i.positionOS.xyz); return o; }
 float hash(float3 p) { p=frac(p*.3183099+float3(.1,.2,.3)); p*=17; return frac(p.x*p.y*p.z*(p.x+p.y+p.z)); }
 float noise(float3 p) { float3 i=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(lerp(hash(i),hash(i+float3(1,0,0)),f.x),lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),f.x),lerp(hash(i+float3(0,1,1)),hash(i+1),f.x),f.y),f.z); }
 float fbm(float3 p) { float v=0,a=.53; for(int j=0;j<5;j++){v+=noise(p)*a;p=p*2.03+7.1;a*=.5;}return v; }
 half4 frag(V i):SV_Target {
 float3 p=normalize(i.local), n=normalize(i.normal), view=normalize(GetCameraPositionWS()-i.world);
 float height=fbm(p*3.8+_Seed); float land=smoothstep(_LandThreshold-.025,_LandThreshold+.025,height);
 float detail=noise(p*58+_Seed*7);
 float3 ocean=lerp(_Ocean.rgb*.52,_Ocean.rgb*1.32,smoothstep(_LandThreshold-.22,_LandThreshold+.01,height));
 float3 terrain=lerp(_Land.rgb,_Highlands.rgb,smoothstep(_LandThreshold,.78,height));
 terrain*=lerp(.86,1.12,detail);
 float3 color=lerp(ocean,terrain,land);
 float polar=smoothstep(.83,.98,abs(p.y)+noise(p*9)*.08); color=lerp(color,float3(.7,.85,.89),polar*_Ice*_Clouds);
 Light sun=GetMainLight(); float light=dot(n,sun.direction); float diffuse=saturate(light);
 float3 cloudDrift=float3(_Time.y*.007,10,20);
 float clouds=smoothstep(.53,.72,fbm(p*6+cloudDrift))*_Clouds;
 float cloudShadow=smoothstep(.55,.73,fbm((p+sun.direction*.025)*6+cloudDrift))*_Clouds;
 color=lerp(color,_CloudTint.rgb,clouds);
 float spec=pow(saturate(dot(reflect(-sun.direction,n),view)),80)*(1-land)*(1-clouds)*.65;
 float3 result=color*(.035+diffuse*sun.color*1.3*lerp(1,.72,cloudShadow*(1-clouds)))+spec*sun.color;
 float city=step(.88,noise(p*420))*step(.58,noise(p*22))*land;
 result+=float3(1,.62,.2)*city*_Cities*(1-smoothstep(-.2,.15,light))*2;
 result+=_Ocean.rgb*(1-land)*_Lava*(1-clouds)*1.7;
 result+=_RimTint.rgb*pow(1-saturate(dot(n,view)),4)*saturate(light+.3)*.42;
 return half4(result,1);
 }
 ENDHLSL }
 }
}
