Shader "Xunjuu/FoliageDither"
{
    Properties
    {
        _BaseColor("Color", Color) = (0.25,0.5,0.2,1)
        _Visibility("Visibility", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="AlphaTest" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Visibility)
            UNITY_INSTANCING_BUFFER_END(Props)
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float fog:TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                const float thresholds[16] = {0,8,2,10,12,4,14,6,3,11,1,9,15,7,13,5};
                uint2 pixel = (uint2)input.positionCS.xy % 4;
                clip(UNITY_ACCESS_INSTANCED_PROP(Props, _Visibility) - (thresholds[pixel.y*4+pixel.x]+0.5)/16.0);
                Light sun = GetMainLight();
                float3 normal = normalize(input.normalWS);
                half3 light = max(SampleSH(normal), half3(0.25,0.25,0.25)) + sun.color * saturate(dot(normal,sun.direction));
                return half4(MixFog(_BaseColor.rgb * light, input.fog), 1);
            }
            ENDHLSL
        }
    }
}
