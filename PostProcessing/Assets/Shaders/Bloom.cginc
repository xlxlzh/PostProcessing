#ifndef _BLOOM_H_
#define _BLOOM_H_

#include "CommonFunc.cginc"

half Brightness(half3 c)
{
    return Max3(c);
}

half3 BoxFilter(sampler2D tex, float2 uv, float2 texelSize)
{
    float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0);

    half3 s;
    s = tex2D(tex, uv + d.xy);
    s += tex2D(tex, uv + d.zy);
    s += tex2D(tex, uv + d.xw);
    s += tex2D(tex, uv + d.zw);

    return s * (1.0 / 4.0);
}

#endif