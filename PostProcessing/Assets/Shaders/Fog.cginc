#ifndef _FOG_H_
#define _FOG_H_

float3 _FogParams;
half4 _FogColor;

half ComputeFog(float z)
{
    half fog = 0.0;

#ifdef POSTPROCESSING_FOG_LINEAR
    fog = (_FogParams.y - z) / (_FogParams.y - _FogParams.x);
#elif POSTPROCESSING_FOG_EXP
    fog = exp2(-_FogParams.z * z);
#elif POSTPROCESSING_FOG_EXP2
    fog = _FogParams.z * z;
    fog = exp2(-fog * fog);
#endif

    return saturate(fog);
}

float ComputeFogDistance(float depth)
{
    float dist = depth * _ProjectionParams.z;
    dist -= _ProjectionParams.y;
    return dist;
}

#endif