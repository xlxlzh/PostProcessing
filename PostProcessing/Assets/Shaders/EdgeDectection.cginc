#ifndef _EDGE_DECTECTION_H_
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
// Upgrade NOTE: excluded shader from DX11 because it uses wrong array syntax (type[size] name)
#pragma exclude_renderers d3d11
#define _EDGE_DECTECTION_H_

float Sobel(sampler2D tex, float2 uvs[9])
{
    const half gx[9] = 
    {
        -1, -2, -1,
        0, 0, 0,
        1, 2, 1
    };

    const half gy[9] = 
    {
        -1, 0, 1,
        -2, 0, 2,
        -1, 0, 1
    };

    half texCol = 0.0;
    half edgeX = 0.0;
    half edgeY = 0.0;

    [UNITYUNROLL]
    for (int i = 0; i < 9; ++i)
    {
        texCol = tex2D(tex, uvs[i]);
        edgeX += texCol * gx[i];
        edgeY += texCol * gy[i];
    }

    return abs(edgeX) + abs(edgeY);
}

#endif