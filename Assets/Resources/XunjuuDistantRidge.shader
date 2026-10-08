Shader "Xunjuu/DistantRidge"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            struct A {float4 vertex:POSITION;float4 color:COLOR;};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;float4 color:COLOR;};
            V vert(A i) {V o;o.world=TransformObjectToWorld(i.vertex.xyz);o.position=TransformWorldToHClip(o.world);o.color=i.color;return o;}
            half4 frag(V i):SV_Target
            {
                // Broad, soft tonal variation: no repeating diagonal stripes.
                float grain=sin(i.world.x*.017)*sin(i.world.z*.023+i.world.y*.011)*.005;
                float crest=saturate((i.world.y+20)*.009);
                float3 paint=lerp(i.color.rgb,float3(.65,.74,.73),crest*.12);
                return half4(SRGBToLinear(saturate(paint+grain)),1);
            }
            ENDHLSL
        }
    }
}
