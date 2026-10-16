Shader "Orbit Dominion/Scan"
{
 Properties { _Progress("Progress",Float)=0 _Flash("Discovery flash",Float)=0 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha One ZWrite Off Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float _Progress,_Flash;
 CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float3 local:TEXCOORD0;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.local=i.p.xyz*2;return o;}
 half4 frag(V i):SV_Target
 {
 float3 n=normalize(i.local);float sweep=1-smoothstep(.015,.09,abs(n.y-lerp(-1,1,_Progress)));
 float grid=pow(abs(sin(n.y*48)),30)*.12+pow(abs(sin(atan2(n.x,n.z)*24)),30)*.09;
 return half4(.08,.8,1,saturate(sweep*.7+grid*.35+_Flash*.16));
 }
 ENDHLSL }
 }
}
