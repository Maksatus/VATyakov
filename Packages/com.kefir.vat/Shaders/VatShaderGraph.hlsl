#ifndef KEFIR_VAT_SHADERGRAPH_INCLUDED
#define KEFIR_VAT_SHADERGRAPH_INCLUDED

// Shader Graph wrappers for the VAT SubGraphs (Custom Function, File mode).
// Only _float variants exist on purpose (§1.2): the SubGraphs and their Custom Functions are Precision Single,
// and a half Vertex ID breaks above 2048. A missing _half makes such a mistake a compile error.

#include "Packages/com.kefir.vat/Shaders/VatCore.hlsl"

// Vertex mode, subversion 1.1: positions only, one clip, nearest frame.
// pos = rest + Δ, Δ from _VatPosTex (RGBAHalf), read with an exact texel load, no filtering.
void VatVertexPosition_float(float VertexId, float3 RestPosition, float Time,
    UnityTexture2D PosTex, float4 Layout, float4 ClipA,
    out float3 Position)
{
    VatClip clip = VatUnpackClip(ClipA);
    float frac;
    uint frame = VatLoopFrame(VatFramePosition(clip, Time), clip.frameCount, frac);
    int2 texel = VatTexel((uint)VertexId, (uint)Layout.x, (uint)Layout.y, clip.startRow + frame);
    float3 delta = LOAD_TEXTURE2D_LOD(PosTex.tex, texel, 0).xyz;
    Position = RestPosition + delta;
}

#endif // KEFIR_VAT_SHADERGRAPH_INCLUDED
