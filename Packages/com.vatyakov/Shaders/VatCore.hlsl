#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

// VATyakov core: addressing (§1.1), clip state (§1.4), frame index (§1.3).
// CPU mirror: Runtime/VatMath.cs — keep both in sync. All math is float/uint, never half (§1.2).

#define VAT_ROW_SHIFT  12u   // packed clip = startRow·4096 + F
#define VAT_FRAME_MASK 4095u

struct VatClip
{
    uint  startRow;    // first row of the clip inside a block
    uint  frameCount;  // F
    bool  loop;        // sign of the packed value: + loop, − one-shot
    float rate;        // fps_eff · speed
    float t0;          // time anchor, seconds
    float offset;      // frame offset
};

// _VatClip* = (±(startRow·4096 + F), rate, t0, offset). |x| ∈ [1, 2^24] is exact in float32.
VatClip VatUnpackClip(float4 state)
{
    VatClip clip;
    uint p = (uint)abs(state.x) - 1u;
    clip.startRow   = p >> VAT_ROW_SHIFT;
    clip.frameCount = (p & VAT_FRAME_MASK) + 1u;
    clip.loop   = state.x > 0.0;
    clip.rate   = state.y;
    clip.t0     = state.z;
    clip.offset = state.w;
    return clip;
}

// f = (t − t0)·rate + offset
float VatFramePosition(VatClip clip, float time)
{
    return (time - clip.t0) * clip.rate + clip.offset;
}

// Loop clamp, floor semantics: f0 ∈ [0, F−1], frac ∈ [0, 1]. Converted to uint only after the clamp.
// The lower bound matters: when f·(1/F) rounds up to an integer, u lands a few ulp below zero.
uint VatLoopFrame(float f, uint frameCount, out float frac)
{
    float count = (float)frameCount;
    float u = f - count * floor(f * (1.0 / count));
    float f0 = clamp(floor(u), 0.0, count - 1.0);
    frac = saturate(u - f0);
    return (uint)f0;
}

// Texel of an element in a row counted inside its block: b = id / W, x = id − b·W, y = b·totalRows + row.
int2 VatTexel(uint element, uint width, uint totalRows, uint row)
{
    width = max(width, 1u); // uninitialized materials: avoid a division by zero
    uint block = element / width;
    return int2(element - block * width, block * totalRows + row);
}

#endif // VATYAKOV_CORE_INCLUDED
