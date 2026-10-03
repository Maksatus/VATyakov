#ifndef VATYAKOV_SHADERGRAPH_INCLUDED
#define VATYAKOV_SHADERGRAPH_INCLUDED

#include "Packages/com.vatyakov/Shaders/VatCore.hlsl"

float3 VatClipOffset(uint id, UnityTexture2D PosTex, float4 Layout, float4 Frame)
{
    float3 delta0 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0).xyz;
    float3 delta1 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0).xyz;
    return lerp(delta0, delta1, Frame.z);
}

float4 VatClipRotation(uint id, UnityTexture2D RotTex, float4 Layout, float4 Frame)
{
    float4 texel0 = LOAD_TEXTURE2D_LOD(RotTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0);
    float4 texel1 = LOAD_TEXTURE2D_LOD(RotTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0);
    return VatDecodeRotation(lerp(texel0, texel1, Frame.z));
}

void VatVertexPosition_float(float VertexId, float3 RestPosition,
    UnityTexture2D PosTex, float4 Layout, float4 Frame, float4 Drift, float4 PosScale,
    out float3 Position)
{
    Position = RestPosition + Drift.xyz + VatClipOffset((uint)VertexId, PosTex, Layout, Frame) * PosScale.xyz;
}

void VatVertexNormalTangent_float(float VertexId,
    UnityTexture2D RotTex, float4 Layout, float4 Frame,
    out float3 Normal, out float3 Tangent)
{
    float4 q = VatClipRotation((uint)VertexId, RotTex, Layout, Frame);
    Normal = VatFrameNormal(q);
    Tangent = VatFrameTangent(q);
}

void VatVertexPositionBlend_float(float VertexId, float3 RestPosition,
    UnityTexture2D PosTex, float4 Layout, float4 Frame, float4 FrameB, float4 Drift, float4 PosScale,
    out float3 Position)
{
    uint id = (uint)VertexId;
    float3 offsetA = VatClipOffset(id, PosTex, Layout, Frame);
    float3 offsetB = VatClipOffset(id, PosTex, Layout, FrameB);
    Position = RestPosition + Drift.xyz + lerp(offsetA, offsetB, FrameB.w) * PosScale.xyz;
}

void VatVertexNormalTangentBlend_float(float VertexId,
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
