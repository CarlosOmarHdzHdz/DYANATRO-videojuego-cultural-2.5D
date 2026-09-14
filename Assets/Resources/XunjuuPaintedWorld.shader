Shader "Xunjuu/PaintedWorld"
{
    Properties { _BaseColor("Tint",Color)=(1,1,1,1) _Visibility("Visibility",Range(0,1))=1 _ZWrite("Depth write",Float)=1 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float _Visibility;
            CBUFFER_END
            float4 _XunjuuPlayerPosition;
            struct A { float4 vertex:POSITION;float3 normal:NORMAL;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct V { float4 pos:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID };
            V vert(A i) { V o; UNITY_SETUP_INSTANCE_ID(i);UNITY_TRANSFER_INSTANCE_ID(i,o);o.world=TransformObjectToWorld(i.vertex.xyz);o.pos=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.normal);o.color=i.color;return o; }
            half4 frag(V i, FRONT_FACE_TYPE front:FRONT_FACE_SEMANTIC):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n=normalize(i.normal)*IS_FRONT_VFACE(front,1,-1);
                float3 viewRay=_XunjuuPlayerPosition.xyz-_WorldSpaceCameraPos;
                float t=dot(i.world-_WorldSpaceCameraPos,viewRay)/max(.01,dot(viewRay,viewRay));
                float radius=length(i.world-(_WorldSpaceCameraPos+t*viewRay));
                float visibility=_Visibility;
                // Feather the reveal around the camera ray to avoid a hard circular hole.
                if(_XunjuuPlayerPosition.w>.5 && t>0 && t<.97)
                    visibility=min(visibility,lerp(.24,1,smoothstep(.45,1.55,radius)));
                const float threshold[16]={0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                uint2 pixel=(uint2)i.pos.xy%4;clip(visibility-(threshold[pixel.y*4+pixel.x]+.5)/16);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half diffuse=saturate(dot(n,sun.direction));
                half3 illumination=max(SampleSH(n),half3(.15,.17,.15)) + sun.color*diffuse*lerp(.35,1,sun.shadowAttenuation)*.80;
                float grain=frac(sin(dot(floor(i.world*35),float3(12.9898,78.233,37.719)))*43758.5453);
                return half4(SRGBToLinear(i.color.rgb)*_BaseColor.rgb*illumination*lerp(.96,1.04,grain),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Off
            HLSLPROGRAM
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float3 _LightDirection;
            struct A {float4 vertex:POSITION;float3 normal:NORMAL;UNITY_VERTEX_INPUT_INSTANCE_ID};
            float4 vertShadow(A i):SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 world=TransformObjectToWorld(i.vertex.xyz);
                float4 p=TransformWorldToHClip(ApplyShadowBias(world,TransformObjectToWorldNormal(i.normal),_LightDirection));
                #if UNITY_REVERSED_Z
                p.z=min(p.z,UNITY_NEAR_CLIP_VALUE);
                #else
                p.z=max(p.z,UNITY_NEAR_CLIP_VALUE);
                #endif
                return p;
            }
            half4 fragShadow():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
