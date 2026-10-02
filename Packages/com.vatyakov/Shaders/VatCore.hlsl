#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

int2 VatTexel(uint element, uint width, uint totalRows, uint row)
{
    uint block = element / width;
    return int2(element - block * width, block * totalRows + row);
}

int2 VatDriftTexel(uint part, uint row)
{
    return int2(part, row);
}

float3 VatDrift(float3 hi, float3 lo)
{
    return hi + lo;
}

uint4 VatRotationBytes(float4 texel)
{
    return (uint4)round(texel * 255.0);
}

uint4 VatRotationFields(uint4 b)
{
    return uint4(b.r | (b.a & 3u) << 8, b.g | (b.a >> 2 & 3u) << 8, b.b | (b.a >> 4 & 3u) << 8, b.a >> 6);
}

float4 VatDecodeRotation(float4 texel)
{
    uint4 f = VatRotationFields(VatRotationBytes(texel));
    float3 abc = ((float3)f.xyz * (2.0 / 1023.0) - 1.0) * 0.70710678;
    float m = sqrt(saturate(1.0 - dot(abc, abc)));
    return f.w == 0u ? float4(m, abc) : f.w == 1u ? float4(abc.x, m, abc.yz) : f.w == 2u ? float4(abc.xy, m, abc.z) : float4(abc, m);
}

float4 VatNlerp(float4 q0, float4 q1, float t)
{
    q1 = dot(q0, q1) < 0.0 ? -q1 : q1;
    return normalize(lerp(q0, q1, t));
}

float3 VatFrameNormal(float4 q)
{
    return float3(2.0 * (q.x * q.z + q.w * q.y), 2.0 * (q.y * q.z - q.w * q.x), 1.0 - 2.0 * (q.x * q.x + q.y * q.y));
}

float3 VatFrameTangent(float4 q)
{
    return float3(1.0 - 2.0 * (q.y * q.y + q.z * q.z), 2.0 * (q.x * q.y + q.w * q.z), 2.0 * (q.x * q.z - q.w * q.y));
}

#endif
