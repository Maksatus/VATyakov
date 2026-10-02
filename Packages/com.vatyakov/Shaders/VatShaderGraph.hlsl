#ifndef VATYAKOV_SHADERGRAPH_INCLUDED
#define VATYAKOV_SHADERGRAPH_INCLUDED

#include "Packages/com.vatyakov/Shaders/VatCore.hlsl"

float3 VatLoadDrift(UnityTexture2D DriftTex, uint row)
{
    return VatDrift(LOAD_TEXTURE2D_LOD(DriftTex.tex, VatDriftTexel(0u, row), 0).xyz,
        LOAD_TEXTURE2D_LOD(DriftTex.tex, VatDriftTexel(1u, row), 0).xyz);
}

void VatVertexPosition_float(float VertexId, float3 RestPosition,
    UnityTexture2D PosTex, float4 Layout, float4 Frame, UnityTexture2D DriftTex,
    out float3 Position)
{
    uint id = (uint)VertexId;
    float3 delta0 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0).xyz;
    float3 delta1 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0).xyz;
    float3 drift = lerp(VatLoadDrift(DriftTex, (uint)Frame.x), VatLoadDrift(DriftTex, (uint)Frame.y), Frame.z);
    Position = RestPosition + drift + lerp(delta0, delta1, Frame.z);
}

void VatVertexNormalTangent_float(float VertexId,
    UnityTexture2D RotTex, float4 Layout, float4 Frame,
    out float3 Normal, out float3 Tangent)
{
    uint id = (uint)VertexId;
    float4 q0 = VatDecodeRotation(LOAD_TEXTURE2D_LOD(RotTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0));
    float4 q1 = VatDecodeRotation(LOAD_TEXTURE2D_LOD(RotTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0));
    float4 q = VatNlerp(q0, q1, Frame.z);
    Normal = VatFrameNormal(q);
    Tangent = VatFrameTangent(q);
}

#endif
