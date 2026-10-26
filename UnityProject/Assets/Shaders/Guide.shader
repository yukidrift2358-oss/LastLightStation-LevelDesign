Shader "LastLight/Guide"
{
    Properties {_Color("Color",Color)=(1,1,1,1)}
    SubShader
    {
        Tags {"Queue"="Geometry+10" "RenderType"="Opaque"}
        Pass
        {
            Cull Off ZWrite Off
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;};
            struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;};
            fixed4 _Color;
            v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;return o;}
            fixed4 frag(v2f i):SV_Target{return i.color*_Color;}
            ENDCG
        }
    }
}
