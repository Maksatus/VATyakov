using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatTangentFrames
    {
        private const float MinSqrLength = 1e-6f;
        private const float MinNormalSqrLength = 0.5f;
        private const float MaxRightAxisAlignment = 0.9f;

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
            for (var vertex = 0; vertex < Normals.Length; vertex++)
            {
                var normal = Normal(data.Normals[vertex]);
                if (!TryOrthogonalize(normal, data.Tangents[vertex], out var tangent))
                {
                    tangent = Repair(normal, frame > 0, Tangents[vertex]);
                    degenerate++;
                }

                Normals[vertex] = normal;
                Tangents[vertex] = tangent;
            }

            return degenerate;
        }

        public static Vector4 Rotation(Vector3 normal, Vector3 tangent)
        {
            var rotation = Quaternion.LookRotation(normal, Vector3.Cross(normal, tangent));
            return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        public static Vector3 Normal(Vector3 normal)
        {
            var unit = normal.normalized;
            return unit.sqrMagnitude > MinNormalSqrLength ? unit : VatFrame.MissingNormal;
        }

        public static Vector3 Repair(Vector3 normal, bool hasPrevious, Vector3 previous)
        {
            return hasPrevious && TryOrthogonalize(normal, previous, out var carried) ? carried : Basis(normal);
        }

        public static Vector3 Basis(Vector3 normal)
        {
            TryOrthogonalize(normal, Mathf.Abs(normal.x) < MaxRightAxisAlignment ? Vector3.right : Vector3.up, out var tangent);
            return tangent;
        }

        public static bool TryOrthogonalize(Vector3 normal, Vector3 tangent, out Vector3 result)
        {
            var unit = tangent.normalized;
            var projected = unit - normal * Vector3.Dot(normal, unit);
            result = projected.normalized;
            return projected.sqrMagnitude > MinSqrLength;
        }
    }
}
