#ifndef VATYAKOV_SHADERGRAPH_INCLUDED
#define VATYAKOV_SHADERGRAPH_INCLUDED

// Shader Graph wrappers for the VAT SubGraphs (Custom Function, File mode).
// Only _float variants exist on purpose (§1.2): the SubGraphs and their Custom Functions are Precision Single,
// and a half Vertex ID breaks above 2048. A missing _half makes such a mistake a compile error.

#include "Packages/com.vatyakov/Shaders/VatCore.hlsl"

// Vertex mode: pos = rest + lerp(Δ0, Δ1, frac). Frame = (row0, row1, frac, 0) from the CPU (§1.4).
void VatVertexPosition_float(float VertexId, float3 RestPosition,
    UnityTexture2D PosTex, float4 Layout, float4 Frame,
    out float3 Position)
{
    uint id = (uint)VertexId;
    float3 d0 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.x), 0).xyz;
    float3 d1 = LOAD_TEXTURE2D_LOD(PosTex.tex, VatTexel(id, (uint)Layout.x, (uint)Layout.y, (uint)Frame.y), 0).xyz;
    Position = RestPosition + lerp(d0, d1, Frame.z);
}

// Vertex mode: frame (T, N×T, N) from _VatRotTex, nlerp between the two rows (§2.2).
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

#endif // VATYAKOV_SHADERGRAPH_INCLUDED
