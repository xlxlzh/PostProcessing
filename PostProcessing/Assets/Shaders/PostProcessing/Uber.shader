Shader "XlXlZh/Uber"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "../EdgeDectection.cginc"
            #include "../CommonFunc.cginc"

            #pragma shader_feature _ EDGE_DECTECTION

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
#ifdef EDGE_DECTECTION
                float2 uv[9] : TEXCOORD0;
#else
                float2 uv : TEXCOORD0;
#endif
                float4 vertex : SV_POSITION;
            };

            uniform half4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                half2 uv = v.uv;

#ifdef EDGE_DECTECTION
                o.uv[0] = uv + _MainTex_TexelSize.xy * half2(-1, -1);
				o.uv[1] = uv + _MainTex_TexelSize.xy * half2(0, -1);
				o.uv[2] = uv + _MainTex_TexelSize.xy * half2(1, -1);
				o.uv[3] = uv + _MainTex_TexelSize.xy * half2(-1, 0);
				o.uv[4] = uv + _MainTex_TexelSize.xy * half2(0, 0);
				o.uv[5] = uv + _MainTex_TexelSize.xy * half2(1, 0);
				o.uv[6] = uv + _MainTex_TexelSize.xy * half2(-1, 1);
				o.uv[7] = uv + _MainTex_TexelSize.xy * half2(0, 1);
				o.uv[8] = uv + _MainTex_TexelSize.xy * half2(1, 1);
#else
                o.uv = uv;
#endif

                return o;
            }

            sampler2D _MainTex;
            float4 _EdgeColor;

            float4 frag (v2f i) : SV_Target
            {
#ifdef EDGE_DECTECTION
                float edge = Sobel(_MainTex, i.uv);
                float4 col = tex2D(_MainTex, i.uv[4]);
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge);
#else
                float4 col = tex2D(_MainTex, i.uv);
#endif
                return col;
            }
            ENDCG
        }
    }
}
