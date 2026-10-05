using UnityEngine;

namespace VATyakov
{
    public static class VatMath
    {
        public const int MaxTextureSize = 4096;
        public const float RotationUnit = 127f;
        public const float RotationZero = 128f;
        public const int TexelsPerBone = 2;
        public const int BonePivotRows = 1;
        public const float BoneWeightMax = 65535f;

        private const float ByteMax = 255f;
        private const float ByteSteps = 256f;

        public static int BlockCount(int elementCount)
        {
            return (elementCount + MaxTextureSize - 1) / MaxTextureSize;
        }

        public static int TextureWidth(int elementCount)
        {
            var blocks = BlockCount(elementCount);
            return (elementCount + blocks - 1) / blocks;
        }

        public static Vector2Int Texel(int element, int width, int totalRows, int row)
        {
            var block = element / width;
            return new Vector2Int(element - block * width, block * totalRows + row);
        }

        public static Vector4 DecodeRotation(Vector4 texel)
        {
            return texel * (ByteMax / RotationUnit) - Vector4.one * (RotationZero / RotationUnit);
        }

        public static Vector3 FrameNormal(Vector4 q)
        {
            return new Vector3(2f * (q.x * q.z + q.w * q.y), 2f * (q.y * q.z - q.w * q.x), q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
        }

        public static Vector3 FrameTangent(Vector4 q)
        {
            return new Vector3(q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z, 2f * (q.x * q.y + q.w * q.z), 2f * (q.x * q.z - q.w * q.y));
        }

        public static int BoneIndex(float boneUv)
        {
            return (int)(boneUv * ByteMax + 0.5f);
        }

        public static int PieceTexel(float high, float low)
        {
            return (int)(Mathf.Floor(high * ByteMax + 0.5f) * (ByteSteps * TexelsPerBone) + Mathf.Floor(low * ByteMax + 0.5f) * TexelsPerBone);
        }

        public static float BoneWeight(float high, float low)
        {
            return Mathf.Floor(high * ByteMax + 0.5f) * (ByteSteps / BoneWeightMax) + Mathf.Floor(low * ByteMax + 0.5f) * (1f / BoneWeightMax);
        }

        public static Vector3 Rotate(Vector4 q, Vector3 vector)
        {
            var axis = new Vector3(q.x, q.y, q.z);
            return (q.w * q.w - Vector3.Dot(axis, axis)) * vector + 2f * Vector3.Dot(axis, vector) * axis + 2f * q.w * Vector3.Cross(axis, vector);
        }

        public static Vector3 BonePoint(Vector4 offsetScale, Vector4 q, Vector3 pivot, Vector3 rest)
        {
            return Rotate(q, rest - pivot) * (offsetScale.w / Vector4.Dot(q, q)) + pivot + (Vector3)offsetScale;
        }

        public static Vector3 BoneDirection(Vector4 q, Vector3 direction)
        {
            return Rotate(q, direction) / Vector4.Dot(q, q);
        }
    }
}
