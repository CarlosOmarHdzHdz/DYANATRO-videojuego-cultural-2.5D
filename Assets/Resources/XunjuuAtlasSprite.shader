Shader "Xunjuu/AtlasSprite"
{
    Properties { [PerRendererData] _MainTex("Sprite",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _KeyMode("Magenta key",Float)=0 }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
        Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _KeyMode;
            CBUFFER_END
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            V vert(A i){SetUpSpriteInstanceProperties();i.pos.xyz=UnityFlipSprite(i.pos.xyz,unity_SpriteProps.xy);V o;o.pos=TransformObjectToHClip(i.pos.xyz);o.uv=i.uv;o.color=i.color*_Color*unity_SpriteColor;return o;}
            half4 frag(V i):SV_Target
            {
                half4 t=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                // Generator returned a neutral checker matte. Key only light achromatic pixels,
                // keeping warm cream clothing, embroidered colours and dark outlines opaque.
                half high=max(t.r,max(t.g,t.b)),low=min(t.r,min(t.g,t.b));
                if(_KeyMode>.5 && _KeyMode<1.5)
                {
                    // Remove dark magenta fringe too; the previous brightness threshold
                    // left a purple silhouette around hair against the green environment.
                    if(t.r>.015 && t.b>.015 && t.g<min(t.r,t.b)*.70 && min(t.r,t.b)>max(t.r,t.b)*.40)discard;
                }
                else if(_KeyMode<.5 && low>.36 && high-low<.055)discard;
                // Straight alpha throughout sprites. Opaque pixel-art interiors, one
                // cutoff for imported edge noise; intentional renderer fades stay intact.
                clip(t.a-.125);
                // Preserve authored coverage on plants; only keyed pixel-art atlases
                // need opaque interiors. Forcing every edge to one made grass bases solid.
                if(_KeyMode<1.5)t.a=1;
                return t*i.color;
            }
            ENDHLSL
        }
    }
}
