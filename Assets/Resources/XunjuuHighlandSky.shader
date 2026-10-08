Shader "Xunjuu/HighlandSky"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f {float4 vertex:SV_POSITION;float3 direction:TEXCOORD0;};
            v2f vert(float4 vertex:POSITION){v2f o;o.vertex=UnityObjectToClipPos(vertex);o.direction=vertex.xyz;return o;}
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(v2f i):SV_Target
            {
                float3 d=normalize(i.direction);float y=d.y;
                float3 color=lerp(float3(.64,.85,.86),float3(.28,.69,.84),smoothstep(-.12,.65,y));
                float2 cloudUV=d.xz/max(.06,y)*.6+float2(_Time.y*.001,0);
                float clouds=noise(cloudUV)*.65+noise(cloudUV*2.1)*.25+noise(cloudUV*4.2)*.1;
                float mask=smoothstep(.54,.72,clouds)*smoothstep(.035,.16,y)*(1-smoothstep(.55,.80,y));
                color=lerp(color,float3(1,.94,.81),mask*.74);
                float a=atan2(d.x,d.z);
                float ridge1=.032+sin(a*5+1)*.021+sin(a*11)*.009+sin(a*23)*.003;
                float ridge2=.003+sin(a*7+2)*.018+sin(a*15+2)*.007;
                float ridge3=-.025+sin(a*9)*.014+sin(a*19)*.006;
                color=lerp(color,float3(.55,.69,.73),1-smoothstep(ridge1-.002,ridge1+.002,y));
                color=lerp(color,float3(.43,.59,.59),1-smoothstep(ridge2-.002,ridge2+.002,y));
                color=lerp(color,float3(.30,.44,.34),1-smoothstep(ridge3-.002,ridge3+.002,y));
                float sun=pow(saturate(dot(d,normalize(float3(-.45,.48,.74)))),500);
                color+=float3(1,.76,.42)*sun*.6;
                return half4(GammaToLinearSpace(color),1);
            }
            ENDHLSL
        }
    }
}
