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
            #include "../Fog.cginc"
            #include "../Bloom.cginc"

            #pragma shader_feature _ EDGE_DECTECTION
            #pragma shader_feature _ POSTPROCESSING_FOG_LINEAR POSTPROCESSING_FOG_EXP POSTPROCESSING_FOG_EXP2
            #pragma shader_feature _ POSTPROCESSING_BLOOM
            #pragma shader_feature _ POSTPROCESSING_VIGNETTE_CLASSIC POSTPROCESSING_VIGNETTE_MASK

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

            sampler2D _BloomTex;
            float4 _BloomTex_TexelSize;

            half3 _Vignette_Color;
            half2 _Vignette_Center;
            half4 _Vignette_Settings;
            sampler2D _Vignette_Mask;
            half _Vignette_Opacity;

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            float4 _EdgeColor;

            float4 frag (v2f i) : SV_Target
            {
                half2 uv = 0.0;
#ifdef EDGE_DECTECTION
                uv = i.uv[4];
                float edge = Sobel(_MainTex, i.uv);
                float4 col = tex2D(_MainTex, uv);
                col.rgb = lerp(col.rgb, _EdgeColor.rgb, edge);
#else
                uv = i.uv;
                float4 col = tex2D(_MainTex, uv);
#endif

#ifdef POSTPROCESSING_BLOOM
                float3 bloomColor = BoxFilter(_BloomTex, uv, _BloomTex_TexelSize.xy);
                col.rgb += bloomColor;
#endif

#if (POSTPROCESSING_FOG_LINEAR) || (POSTPROCESSING_FOG_EXP) || (POSTPROCESSING_FOG_EXP2)
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
                depth = Linear01Depth(depth);
                float dist = ComputeFogDistance(depth);
                half fog = 1.0 - ComputeFog(dist);
                col.rgb = lerp(col.rgb, _FogColor.rgb, fog);
#endif

#if POSTPROCESSING_VIGNETTE_CLASSIC
                half2 d = abs(uv - _Vignette_Center) * _Vignette_Settings.x;
                d.x *= lerp(1.0, _ScreenParams.x / _ScreenParams.y, _Vignette_Settings.w);
                d = pow(d, _Vignette_Settings.z);
                half vfactor = pow(saturate(1.0 - dot(d, d)), _Vignette_Settings.y);
                col.rgb *= lerp(_Vignette_Color, (1.0).xxx, vfactor);
#elif POSTPROCESSING_VIGNETTE_MASK
                half vfactor = tex2D(_Vignette_Mask, i.uv);
                half3 blendColor = col.rgb * lerp(_Vignette_Color, (1.0).xxx, vfactor);
                col.rgb = lerp(col.rgb, blendColor, _Vignette_Opacity);
#endif
                return col;
            }
            ENDCG
        }
    }
}
