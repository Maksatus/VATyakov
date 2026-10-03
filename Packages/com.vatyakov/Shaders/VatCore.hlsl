#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

#define VAT_ROTATION_SCALE (255.0 / 127.0)
#define VAT_ROTATION_BIAS (128.0 / 127.0)

int2 VatTexel(uint element, uint width, uint totalRows, uint row)
{
    uint block = element / width;
    return int2(element - block * width, block * totalRows + row);
}

float4 VatDecodeRotation(float4 texel)
{
    return texel * VAT_ROTATION_SCALE - VAT_ROTATION_BIAS;
}

float3 VatFrameNormal(float4 q)
{
    return float3(2.0 * (q.x * q.z + q.w * q.y), 2.0 * (q.y * q.z - q.w * q.x), q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
}

float3 VatFrameTangent(float4 q)
{
    return float3(q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z, 2.0 * (q.x * q.y + q.w * q.z), 2.0 * (q.x * q.z - q.w * q.y));
}

#endif
