using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatTangentFrames
    {
        private const float MinSqrLength = 1e-6f;

        public readonly Vector3[] Normals;
        public readonly Vector3[] Tangents;

        public VatTangentFrames(int vertexCount)
        {
            Normals = new Vector3[vertexCount];
            Tangents = new Vector3[vertexCount];
        }

        public int Build(int frame, VatFrame data)
        {
            var degenerate = 0;
            for (var v = 0; v < Normals.Length; v++)
            {
                var n = Normal(data.Normals[v]);
                if (!TryOrthogonalize(n, data.Tangents[v], out var t))
                {
                    t = Repair(n, frame > 0, Tangents[v]);
                    degenerate++;
                }

                Normals[v] = n;
                Tangents[v] = t;
            }

            return degenerate;
        }

        public static Vector4 Rotation(Vector3 normal, Vector3 tangent)
        {
            var q = Quaternion.LookRotation(normal, Vector3.Cross(normal, tangent));
            return new Vector4(q.x, q.y, q.z, q.w);
        }

        public static Vector3 Normal(Vector3 normal)
        {
            var n = normal.normalized;
            return n.sqrMagnitude > 0.5f ? n : Vector3.forward;
        }

        public static Vector3 Repair(Vector3 normal, bool hasPrevious, Vector3 previous)
        {
            return hasPrevious && TryOrthogonalize(normal, previous, out var carried) ? carried : Basis(normal);
        }

        public static Vector3 Basis(Vector3 normal)
        {
            TryOrthogonalize(normal, Mathf.Abs(normal.x) < 0.9f ? Vector3.right : Vector3.up, out var tangent);
            return tangent;
        }

        public static bool TryOrthogonalize(Vector3 normal, Vector3 tangent, out Vector3 result)
        {
            var t = tangent.normalized;
            var p = t - normal * Vector3.Dot(normal, t);
            result = p.normalized;
            return p.sqrMagnitude > MinSqrLength;
        }
    }
}
