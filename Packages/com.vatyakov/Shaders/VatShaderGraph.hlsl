#ifndef VATYAKOV_SHADERGRAPH_INCLUDED
#define VATYAKOV_SHADERGRAPH_INCLUDED

#include "Packages/com.vatyakov/Shaders/VatCore.hlsl"

float3 VatLoadDrift(UnityTexture2D DriftTex, uint row)
{
    return VatDrift(LOAD_TEXTURE2D_LOD(DriftTex.tex, VatDriftTexel(0u, row), 0).xyz,
        LOAD_TEXTURE2D_LOD(DriftTex.tex, VatDriftTexel(1u, row), 0).xyz);
}

float3 VatClipOffset(uint id, UnityTexture2D PosTex, float4 Layout, float4 Frame, UnityTexture2D DriftTex)
{
    float3 delta0 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0).xyz;
    float3 delta1 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0).xyz;
    float3 drift = lerp(VatLoadDrift(DriftTex, (uint)Frame.x), VatLoadDrift(DriftTex, (uint)Frame.y), Frame.z);
    return drift + lerp(delta0, delta1, Frame.z);
}

float4 VatClipRotation(uint id, UnityTexture2D RotTex, float4 Layout, float4 Frame)
{
    float4 q0 = VatDecodeRotation(LOAD_TEXTURE2D_LOD(RotTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0));
    float4 q1 = VatDecodeRotation(LOAD_TEXTURE2D_LOD(RotTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0));
    return VatNlerp(q0, q1, Frame.z);
}

void VatVertexPosition_float(float VertexId, float3 RestPosition,
    UnityTexture2D PosTex, float4 Layout, float4 Frame, UnityTexture2D DriftTex, float4 FrameB,
    out float3 Position)
{
    uint id = (uint)VertexId;
    float3 offsetA = VatClipOffset(id, PosTex, Layout, Frame, DriftTex);
    float3 offsetB = VatClipOffset(id, PosTex, Layout, FrameB, DriftTex);
    Position = RestPosition + lerp(offsetA, offsetB, FrameB.w);
}

void VatVertexNormalTangent_float(float VertexId,
    UnityTexture2D RotTex, float4 Layout, float4 Frame, float4 FrameB,
    out float3 Normal, out float3 Tangent)
{
    uint id = (uint)VertexId;
    float4 qA = VatClipRotation(id, RotTex, Layout, Frame);
    float4 qB = VatClipRotation(id, RotTex, Layout, FrameB);
    Normal = lerp(VatFrameNormal(qA), VatFrameNormal(qB), FrameB.w);
    Tangent = lerp(VatFrameTangent(qA), VatFrameTangent(qB), FrameB.w);
}

#endif
