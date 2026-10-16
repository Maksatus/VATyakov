using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatMemory
    {
        private const int ShortIndexBytes = 2;
        private const int IntIndexBytes = 4;

        public static long TextureBytes(VatLayoutInfo info, VatPositionFormat format)
        {
            return Bytes(info.Width, info.Height, VatVertexFormat.Position(format)) + Bytes(info.Width, info.Height, VatVertexFormat.Rotation);
        }

        public static long BoneTextureBytes(VatLayoutInfo info)
        {
            return Bytes(info.Width, info.Height, VatBoneFormat.Texture);
        }

        public static long Bytes(Texture2D texture)
        {
            return Bytes(texture.width, texture.height, texture.graphicsFormat);
        }

        public static long Bytes(Mesh mesh)
        {
            var bytes = 0L;
            for (var stream = 0; stream < mesh.vertexBufferCount; stream++)
            {
                bytes += (long)mesh.vertexCount * mesh.GetVertexBufferStride(stream);
            }

            var indexBytes = mesh.indexFormat == IndexFormat.UInt16 ? ShortIndexBytes : IntIndexBytes;
            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                bytes += (long)mesh.GetIndexCount(subMesh) * indexBytes;
            }

            return bytes;
        }

        public static long TexelBytes(Texture2D texture)
        {
            return GraphicsFormatUtility.GetBlockSize(texture.graphicsFormat);
        }

        private static long Bytes(int width, int height, GraphicsFormat format)
        {
            return (long)width * height * GraphicsFormatUtility.GetBlockSize(format);
        }
    }
}
