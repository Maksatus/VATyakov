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

float4 VatBoneTexel(UnityTexture2D BoneTex, uint texel, float row)
{
    return LOAD_TEXTURE2D_LOD(BoneTex.tex, int2(texel, (uint)row), 0);
}

float4 VatBoneFrameTexel(UnityTexture2D BoneTex, uint texel, float4 Frame)
{
    return lerp(VatBoneTexel(BoneTex, texel, Frame.x), VatBoneTexel(BoneTex, texel, Frame.y), Frame.z);
}

void VatBoneSkin(uint texel, float3 RestPosition, float3 RestNormal, float3 RestTangent, UnityTexture2D BoneTex, float4 Layout, float4 Frame,
    out float3 Position, out float3 Normal, out float3 Tangent)
{
    float3 pivot = VatBoneTexel(BoneTex, texel, Layout.y - VAT_BONE_PIVOT_ROWS).xyz;
    float4 offsetScale = VatBoneFrameTexel(BoneTex, texel, Frame);
    float4 q = VatBoneFrameTexel(BoneTex, texel + 1, Frame);
    Position = VatBonePoint(offsetScale, q, pivot, RestPosition);
    Normal = VatBoneDirection(q, RestNormal);
    Tangent = VatBoneDirection(q, RestTangent);
}

void VatBonePose(float4 BoneUv, float3 RestPosition, float3 RestNormal, float3 RestTangent, UnityTexture2D BoneTex, float4 Layout, float4 Frame,
    out float3 Position, out float3 Normal, out float3 Tangent)
{
    uint2 texels = VatBoneTexels(BoneUv);
    float weight = VatBoneWeight(BoneUv);
    float3 position0;
    float3 normal0;
    float3 tangent0;
    float3 position1;
    float3 normal1;
    float3 tangent1;
    VatBoneSkin(texels.x, RestPosition, RestNormal, RestTangent, BoneTex, Layout, Frame, position0, normal0, tangent0);
    VatBoneSkin(texels.y, RestPosition, RestNormal, RestTangent, BoneTex, Layout, Frame, position1, normal1, tangent1);
    Position = lerp(position1, position0, weight);
    Normal = lerp(normal1, normal0, weight);
    Tangent = lerp(tangent1, tangent0, weight);
}

void VatBoneVertex_float(float4 BoneUv, float3 RestPosition, float3 RestNormal, float3 RestTangent,
    UnityTexture2D BoneTex, float4 Layout, float4 Frame,
    out float3 Position, out float3 Normal, out float3 Tangent)
{
    VatBonePose(BoneUv, RestPosition, RestNormal, RestTangent, BoneTex, Layout, Frame, Position, Normal, Tangent);
}

void VatBoneVertexBlend_float(float4 BoneUv, float3 RestPosition, float3 RestNormal, float3 RestTangent,
    UnityTexture2D BoneTex, float4 Layout, float4 Frame, float4 FrameB,
    out float3 Position, out float3 Normal, out float3 Tangent)
{
    float3 positionA;
    float3 normalA;
    float3 tangentA;
    float3 positionB;
    float3 normalB;
    float3 tangentB;
    VatBonePose(BoneUv, RestPosition, RestNormal, RestTangent, BoneTex, Layout, Frame, positionA, normalA, tangentA);
    VatBonePose(BoneUv, RestPosition, RestNormal, RestTangent, BoneTex, Layout, FrameB, positionB, normalB, tangentB);
    Position = lerp(positionA, positionB, FrameB.w);
    Normal = lerp(normalA, normalB, FrameB.w);
    Tangent = lerp(tangentA, tangentB, FrameB.w);
}

#endif
