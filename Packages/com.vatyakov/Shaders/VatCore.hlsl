#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

// VATyakov core: texel addressing (§1.1) and the frame rotation codec (§1.9). CPU mirror: Runtime/VatMath.cs —
// keep both in sync. Frames, loops and time are computed on the CPU (§1.3); inputs are validated by the baker, not here.

// b = id / W, x = id − b·W, y = b·totalRows + row.
int2 VatTexel(uint element, uint width, uint totalRows, uint row)
{
    uint block = element / width;
    return int2(element - block * width, block * totalRows + row);
}

// RGBA8 is read as bytes: round(v·255) is exact even through a mediump sampler (§1.2).
uint4 VatRotationBytes(float4 texel)
{
    return (uint4)round(texel * 255.0);
}

// Smallest-three fields (n_a, n_b, n_c, idx), §1.9.
uint4 VatRotationFields(uint4 b)
{
    return uint4(b.r | (b.a & 3u) << 8, b.g | (b.a >> 2 & 3u) << 8, b.b | (b.a >> 4 & 3u) << 8, b.a >> 6);
}

// Unit quaternion (x, y, z, w); the dropped component is the largest one and is never negative.
float4 VatDecodeRotation(float4 texel)
{
    uint4 f = VatRotationFields(VatRotationBytes(texel));
    float3 abc = ((float3)f.xyz * (2.0 / 1023.0) - 1.0) * 0.70710678;
    float m = sqrt(saturate(1.0 - dot(abc, abc)));
    return f.w == 0u ? float4(m, abc) : f.w == 1u ? float4(abc.x, m, abc.yz) : f.w == 2u ? float4(abc.xy, m, abc.z) : float4(abc, m);
}

// q and −q are the same rotation: align the sign, then nlerp (§2.2). The aligned pair never sums to zero.
float4 VatNlerp(float4 q0, float4 q1, float t)
{
    q1 = dot(q0, q1) < 0.0 ? -q1 : q1;
    return normalize(lerp(q0, q1, t));
}

// Frame (T, N×T, N): N = rot(q, (0, 0, 1)).
float3 VatFrameNormal(float4 q)
{
    return float3(2.0 * (q.x * q.z + q.w * q.y), 2.0 * (q.y * q.z - q.w * q.x), 1.0 - 2.0 * (q.x * q.x + q.y * q.y));
}

// T = rot(q, (1, 0, 0)); the bitangent sign stays in the mesh tangent.w.
float3 VatFrameTangent(float4 q)
{
    return float3(1.0 - 2.0 * (q.y * q.y + q.z * q.z), 2.0 * (q.x * q.y + q.w * q.z), 2.0 * (q.x * q.z - q.w * q.y));
}

#endif // VATYAKOV_CORE_INCLUDED
