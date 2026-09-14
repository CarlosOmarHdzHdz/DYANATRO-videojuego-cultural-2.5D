Shader "Xunjuu/UI/MazahuaHealth"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Health ("Health", Range(0,1)) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 world : TEXCOORD1; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _Health;
            v2f vert(appdata v)
            {
                v2f o;
                o.world = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 full = tex2D(_MainTex, i.uv) + _TextureSampleAdd;
                // The supplied empty frame has transparent cells and the original segment outlines.
                float2 emptyUV = i.uv + float2(300, -600) / _MainTex_TexelSize.zw;
                fixed4 empty = tex2D(_MainTex, emptyUV) + _TextureSampleAdd;
                fixed4 c = empty * i.color;
                float2 pixel = float2(i.uv.x, 1-i.uv.y) * _MainTex_TexelSize.zw;
                // Exclude the ornament from the preceding row of the source sheet.
                if (pixel.x < 70 && pixel.y < 100) c.a = 0;
                bool interior = pixel.x >= 14 && pixel.x < 279 && pixel.y >= 152 && pixel.y < 194;
                bool skull = pixel.x >= 110 && pixel.y < 167 && pixel.x < 166 - (pixel.y - 152) * 1.5;
                bool medallion = distance(pixel, float2(268, 153)) < 16;
                float edge = 14 + floor(265 * saturate(_Health));
                if (interior && !skull && !medallion && pixel.x < edge && _Health > 0)
                    c = full * i.color;
                // At maximum health use the complete original artwork, without mixing frames.
                if (_Health >= 0.9999) c = full * i.color;
                if (pixel.x < 70 && pixel.y < 100) c.a = 0;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.world.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(c.a - 0.001);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
