Shader "Custom/Outline"
{
    Properties
    {
        _Color ("Color", Color) = (1,0,0,1)
        _Thickness ("Thickness", Range(0, 0.1)) = 0.02
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            float4 _Color;
            float _Thickness;
            
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float minDist = min(min(uv.x, 1-uv.x), min(uv.y, 1-uv.y));
                if (minDist > _Thickness) discard;
                return _Color;
            }
            ENDCG
        }
    }
}