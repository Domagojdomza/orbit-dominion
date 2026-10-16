Shader "Orbit Dominion/Atmosphere"
{
 Properties { _Color("Scattering",Color)=(.12,.5,1,1) }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha One ZWrite Off Cull Back
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};
 struct V{float4 p:SV_POSITION;float3 n:TEXCOORD0;float3 w:TEXCOORD1;};
 V vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.w=TransformObjectToWorld(i.p.xyz);o.n=TransformObjectToWorldNormal(i.n);return o;}
 half4 frag(V i):SV_Target {float3 n=normalize(i.n);float rim=pow(1-saturate(dot(n,normalize(GetCameraPositionWS()-i.w))),3.5);float lit=saturate(dot(n,GetMainLight().direction)+.4);return half4(_Color.rgb*1.4,rim*(.13+lit*.65));}
 ENDHLSL }
 }
}
