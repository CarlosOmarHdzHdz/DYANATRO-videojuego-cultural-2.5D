Shader "Xunjuu/FootpathSoil"
{
    Properties { _BaseColor("Soil tint", Color)=(0.60,0.47,0.32,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct A {float4 positionOS:POSITION;};
            struct V {float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            V vert(A i){V o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float2 p=i.world.xz;
                float grain=noise(p*38),soil=noise(p*2.7),wear=noise(p*.43);
                float2 cell=floor(p*8),local=frac(p*8)-.5;
                float pebble=step(.88,hash(cell))*(1-smoothstep(.08,.18,length(local-float2(hash(cell+7),hash(cell+13))*.20)));
                float3 color=_BaseColor.rgb*(.83+.15*soil+.10*wear)+(grain-.5)*.075;
                color=lerp(color,float3(.47,.44,.36),pebble*.7);
                Light light=GetMainLight();color*=.74+.26*saturate(light.direction.y);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
