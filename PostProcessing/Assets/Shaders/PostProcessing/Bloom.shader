Shader "XlXlZh/Bloom"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    
    #include "UnityCG.cginc"
    #include "../Bloom.cginc"

    sampler2D _MainTex;
    sampler2D _SourceTex;
    float4 _MainTex_TexelSize;

    half4 _Filter;
    half _Intensity;

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
    };

    v2f vert (appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    half3 BoxFilter(float2 uv, float delta)
    {
        float4 o = _MainTex_TexelSize.xyxy * float2(-delta, delta).xxyy;
        half3 s = tex2D(_MainTex, uv + o.xy).rgb + tex2D(_MainTex, uv + o.zy).rgb + tex2D(_MainTex, uv + o.xw).rgb + tex2D(_MainTex, uv + o.zw).rgb;
        return s * 0.25;
    }

    half3 Prefilter(half3 c)
    {
        half brightness = max(c.r, max(c.g, c.b));
        half soft = brightness - _Filter.y;
        soft = clamp(soft, 0, _Filter.z);
        soft = soft * soft * _Filter.w;
        half contribution = max(soft, brightness - _Filter.x);
        contribution /= max(brightness, 0.00001);
        return c * contribution;
    }

    ENDCG

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag (v2f i) : SV_Target
            {
                half4 c = tex2D(_MainTex, i.uv);
                return half4(Prefilter(c), 1.0);
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag (v2f i) : SV_Target
            {
                return half4(BoxFilter(_MainTex, i.uv, _MainTex_TexelSize.xy), 1.0);
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            half4 frag (v2f i) : SV_Target
            {
                half4 c = tex2D(_SourceTex, i.uv);
                c.rgb += _Intensity * BoxFilter(_MainTex, i.uv, _MainTex_TexelSize.xy);
                return c;
            }

            ENDCG
        }
    }
}
