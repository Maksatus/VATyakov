using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: orthonormal N and T of every frame. T is the skinned source tangent after Gram–Schmidt; a degenerate one
    // (degenerate UVs in the source) is carried over from the previous frame of the same clip, frame 0 gets a basis
    // from N alone. Clips never see each other, so the result does not depend on the clip order.
    sealed class VatTangentFrames
    {
        const float MinSqrLength = 1e-6f; // T at least 1e-3 rad away from N

        public readonly Vector3[] Normals;
        public readonly Vector3[] Tangents;

        public VatTangentFrames(int vertexCount)
        {
            Normals = new Vector3[vertexCount];
            Tangents = new Vector3[vertexCount];
        }

        // Returns how many tangents of the frame were degenerate.
        public int Build(int frame, VatFrame data)
        {
            int degenerate = 0;
            for (int v = 0; v < Normals.Length; v++)
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

        // (T, N×T, N) as (x, y, z, w).
        public static Vector4 Rotation(Vector3 normal, Vector3 tangent)
        {
            var q = Quaternion.LookRotation(normal, Vector3.Cross(normal, tangent));
            return new Vector4(q.x, q.y, q.z, q.w);
        }

        // BakeMesh does not keep normals unit under scaled bones; a zero normal falls back like a missing one.
        public static Vector3 Normal(Vector3 normal)
        {
            var n = normal.normalized;
            return n.sqrMagnitude > 0.5f ? n : Vector3.forward;
        }

        // T_k = normalize(T_{k−1} − N_k·dot(N_k, T_{k−1})).
        public static Vector3 Repair(Vector3 normal, bool hasPrevious, Vector3 previous) =>
            hasPrevious && TryOrthogonalize(normal, previous, out var carried) ? carried : Basis(normal);

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
