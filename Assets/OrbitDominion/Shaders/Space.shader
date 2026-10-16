Shader "Orbit Dominion/Space"
{
 Properties { _Tint("Nebula",Color)=(.045,.12,.24,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" "RenderPipeline"="UniversalPipeline" }
 Pass { Cull Off ZWrite Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 float4 _Tint;
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 dir:TEXCOORD0;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.dir=i.p.xyz;return o;}
 float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
 float noise(float3 p){float3 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),f.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),f.x),f.y),lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),f.x),lerp(hash(a+float3(0,1,1)),hash(a+1),f.x),f.y),f.z);}
 half4 frag(V i):SV_Target{
 float3 d=normalize(i.dir);float n=noise(d*4)*.6+noise(d*11)*.3+noise(d*27)*.1;
 float band=pow(saturate(1-abs(d.y+d.x*.23-.1)),8);
 float3 c=float3(.002,.004,.012)+_Tint.rgb*pow(n,2)*band*1.4;
 c+=float3(.09,.025,.13)*pow(noise(d*7+30),3)*band*1.4;
 float3 cell=floor(d*310),f=frac(d*310)-.5;
 float star=step(.985,hash(cell))*pow(saturate(1-length(f)*2),4);
 c+=lerp(float3(.6,.75,1),float3(1,.8,.55),hash(cell+1))*star*7;
 float sun=dot(d,normalize(float3(-.65,.45,-.55)));
 c+=float3(1,.62,.25)*(pow(saturate(sun),1300)*6+pow(saturate(sun),100)*.15);
 return half4(c,1);}
 ENDHLSL }
 }
}
