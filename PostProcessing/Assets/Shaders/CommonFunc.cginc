#ifndef _COMMONFUNC_H_
#define _COMMONFUNC_H_

float Luminance(float4 color)
{
    return 0.2125 * color.r + 0.7154 * color.g + 0.0721 * color.b;
}

#endif