#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

#define VAT_BYTE_MAX 255.0
#define VAT_FIELD_MAX 1023.0
#define VAT_MAX_COMPONENT 0.70710678
#define VAT_FIELD_LOW_BITS 8u
#define VAT_HIGH_BITS_MASK 3u
#define VAT_HIGH_BITS_A_SHIFT 0u
#define VAT_HIGH_BITS_B_SHIFT 2u
#define VAT_HIGH_BITS_C_SHIFT 4u
#define VAT_INDEX_SHIFT 6u

int2 VatTexel(uint element, uint width, uint totalRows, uint row)
{
    uint block = element / width;
    return int2(element - block * width, block * totalRows + row);
}

uint4 VatRotationBytes(float4 texel)
{
    return (uint4)round(texel * VAT_BYTE_MAX);
}

uint VatRotationField(uint low, uint highBits, uint shift)
{
    return low | (highBits >> shift & VAT_HIGH_BITS_MASK) << VAT_FIELD_LOW_BITS;
}

uint4 VatRotationFields(uint4 bytes)
{
    return uint4(VatRotationField(bytes.r, bytes.a, VAT_HIGH_BITS_A_SHIFT), VatRotationField(bytes.g, bytes.a, VAT_HIGH_BITS_B_SHIFT),
        VatRotationField(bytes.b, bytes.a, VAT_HIGH_BITS_C_SHIFT), bytes.a >> VAT_INDEX_SHIFT);
}

float4 VatDecodeRotation(float4 texel)
{
    uint4 fields = VatRotationFields(VatRotationBytes(texel));
    float3 smallest = ((float3)fields.xyz * (2.0 / VAT_FIELD_MAX) - 1.0) * VAT_MAX_COMPONENT;
    float largest = sqrt(1.0 - dot(smallest, smallest));
    return fields.w == 0u ? float4(largest, smallest)
        : fields.w == 1u ? float4(smallest.x, largest, smallest.yz)
        : fields.w == 2u ? float4(smallest.xy, largest, smallest.z)
        : float4(smallest, largest);
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
