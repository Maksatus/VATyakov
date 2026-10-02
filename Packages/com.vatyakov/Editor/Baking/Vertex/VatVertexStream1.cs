using System.Runtime.InteropServices;
using UnityEngine;

namespace VATyakov.Editor
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VatVertexStream1
    {
        public ushort NormalX, NormalY, NormalZ, NormalW;
        public ushort TangentX, TangentY, TangentZ, TangentW;
        public ushort U, V;

        public static VatVertexStream1[] Build(VatRestPose rest, Vector2[] uv)
        {
            var data = new VatVertexStream1[rest.Positions.Length];
            for (var v = 0; v < data.Length; v++)
            {
                data[v] = From(rest.Normals[v], rest.Tangents[v], uv != null ? uv[v] : Vector2.zero);
            }

            return data;
        }

        private static VatVertexStream1 From(Vector3 n, Vector4 t, Vector2 uv)
        {
            return new VatVertexStream1
            {
                NormalX = Half(n.x), NormalY = Half(n.y), NormalZ = Half(n.z),
                TangentX = Half(t.x), TangentY = Half(t.y), TangentZ = Half(t.z), TangentW = Half(t.w < 0f ? -1f : 1f),
                U = Half(uv.x), V = Half(uv.y),
            };
        }

        private static ushort Half(float value)
        {
            return Mathf.FloatToHalf(value);
        }
    }
}
