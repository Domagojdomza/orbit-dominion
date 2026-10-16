Shader "Orbit Dominion/Rings"
{
 Properties { _Color("Ice and dust",Color)=(.42,.52,.63,1) _Radius("Planet radius",Float)=1 _PlanetCenter("Planet center",Vector)=(0,0,0,0) }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Color,_PlanetCenter;float _Radius;
 CBUFFER_END
 struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
 struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;float3 w:TEXCOORD1;float3 n:TEXCOORD2;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.w=TransformObjectToWorld(i.p.xyz);o.uv=i.uv;o.n=TransformObjectToWorldNormal(float3(0,1,0));return o;}
 float band(float x,float frequency){return sin(x*frequency)*saturate(1-fwidth(x)*frequency*.5);}
 half4 frag(V i):SV_Target
 {
 float x=i.uv.x;float grain=.65+.16*band(x,170)+.12*band(x,371)+.06*band(x,950);
 float wide=.55+.25*sin(x*19)+.15*sin(x*43);
 float gap=1-smoothstep(.58,.60,x)*(1-smoothstep(.64,.66,x));
 float edge=smoothstep(0,.04,x)*(1-smoothstep(.94,1,x));
 Light sun=GetMainLight();float3 r=i.w-_PlanetCenter.xyz;float b=dot(r,sun.direction);
 float shadow=(b<0&&b*b-dot(r,r)+_Radius*_Radius>0)?.16:1;
 float lighting=.22+abs(dot(normalize(i.n),sun.direction))*.8;
 return half4(_Color.rgb*(.6+grain*.65)*sun.color*lighting*shadow,saturate(grain*wide*gap*edge*.9));
 }
 ENDHLSL }
 }
}
