#ifndef VATYAKOV_CORE_INCLUDED
#define VATYAKOV_CORE_INCLUDED

#define VAT_ROTATION_SCALE (255.0 / 127.0)
#define VAT_ROTATION_BIAS (128.0 / 127.0)
#define VAT_TEXELS_PER_BONE 2
#define VAT_BONE_PIVOT_ROWS 1.0
#define VAT_BYTE_MAX 255.0
#define VAT_BYTE_STEPS 256.0
#define VAT_BONE_WEIGHT_MAX 65535.0

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

uint2 VatBoneTexels(float4 boneUv)
{
    return (uint2)(boneUv.xy * VAT_BYTE_MAX + 0.5) * VAT_TEXELS_PER_BONE;
}

uint VatPieceTexel(float4 pieceUv)
{
    float2 bytes = floor(pieceUv.xy * VAT_BYTE_MAX + 0.5);
    return (uint)dot(bytes, float2(VAT_BYTE_STEPS * VAT_TEXELS_PER_BONE, VAT_TEXELS_PER_BONE));
}

float VatBoneWeight(float4 boneUv)
{
    float2 bytes = floor(boneUv.zw * VAT_BYTE_MAX + 0.5);
    return dot(bytes, float2(VAT_BYTE_STEPS / VAT_BONE_WEIGHT_MAX, 1.0 / VAT_BONE_WEIGHT_MAX));
}

float3 VatRotate(float4 q, float3 v)
{
    return (q.w * q.w - dot(q.xyz, q.xyz)) * v + 2.0 * dot(q.xyz, v) * q.xyz + 2.0 * q.w * cross(q.xyz, v);
}

float3 VatBonePoint(float4 offsetScale, float4 q, float3 pivot, float3 rest)
{
    return VatRotate(q, rest - pivot) * (offsetScale.w / dot(q, q)) + pivot + offsetScale.xyz;
}

float3 VatBoneDirection(float4 q, float3 direction)
{
    return VatRotate(q, direction) / dot(q, q);
}

#endif
