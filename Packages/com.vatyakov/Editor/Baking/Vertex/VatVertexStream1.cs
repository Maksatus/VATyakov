using System.Runtime.InteropServices;
using UnityEngine;

namespace VATyakov.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VatVertexStream1
    {
        public ushort NormalX;
        public ushort NormalY;
        public ushort NormalZ;
        public ushort NormalW;
        public ushort TangentX;
        public ushort TangentY;
        public ushort TangentZ;
        public ushort TangentW;
        public ushort U;
        public ushort V;

        public static VatVertexStream1[] Build(VatRestPose rest, Vector2[] uv)
        {
            var data = new VatVertexStream1[rest.Positions.Length];
            for (var vertex = 0; vertex < data.Length; vertex++)
            {
                data[vertex] = From(rest.Normals[vertex], rest.Tangents[vertex], uv != null ? uv[vertex] : Vector2.zero);
            }

            return data;
        }

        private static VatVertexStream1 From(Vector3 normal, Vector4 tangent, Vector2 uv)
        {
            return new VatVertexStream1
            {
                NormalX = Half(normal.x),
                NormalY = Half(normal.y),
                NormalZ = Half(normal.z),
                TangentX = Half(tangent.x),
                TangentY = Half(tangent.y),
                TangentZ = Half(tangent.z),
                TangentW = Half(tangent.w < 0f ? -1f : 1f),
                U = Half(uv.x),
                V = Half(uv.y),
            };
        }

        private static ushort Half(float value)
        {
            return Mathf.FloatToHalf(value);
        }
    }
}
