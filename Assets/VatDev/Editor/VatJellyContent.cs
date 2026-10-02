using UnityEditor;
using UnityEngine;

namespace VATyakov.Dev
{
    internal static class VatJellyContent
    {
        private const string Path = "Assets/VatDev/Content/Alembic/jelly.abc";
        private const int Frames = 121;
        private const float Flight = 1.5f;
        private const float Distance = 40f;
        private const float Height = 6f;
        private const float Radius = 0.5f;
        private const int Segments = 48;
        private const int Rings = 24;

        [MenuItem("VATyakov/Dev/Regenerate Drift Content")]
        private static void Regenerate()
        {
            VatAlembicFixtures.Record(Path, Frames, true, (mesh, k) => Jelly(mesh, k / VatAlembicFixtures.Fps));
            AssetDatabase.Refresh();
        }

        private static Mesh Jelly(Mesh mesh, float t)
        {
            var center = Center(t);
            var landed = Mathf.Max(0f, t - Flight);
            var vertices = new Vector3[(Segments + 1) * (Rings + 1)];
            var uv = new Vector2[vertices.Length];
            for (var r = 0; r <= Rings; r++)
            {
                for (var s = 0; s <= Segments; s++)
                {
                    var u = s / (float)Segments;
                    var v = r / (float)Rings;
                    var n = Sphere(u, v);
                    vertices[r * (Segments + 1) + s] = center + Deform(n, u, t, landed);
                    uv[r * (Segments + 1) + s] = new Vector2(u, v);
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = VatAlembicFixtures.Triangles(Segments + 1, Rings + 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Center(float t)
        {
            var a = Mathf.Clamp01(t / Flight);
            return new Vector3(Distance * a, Radius + 4f * Height * a * (1f - a), 0f);
        }

        private static Vector3 Deform(Vector3 n, float u, float t, float landed)
        {
            var squash = landed > 0f ? 0.35f * Mathf.Exp(-2f * landed) * Mathf.Sin(10f * landed) : 0.08f * Mathf.Sin(12f * t);
            var ripple = 0.04f * Mathf.Sin(6f * Mathf.PI * u + 1.5f * t) * (1f - n.y * n.y);
            var y = 1f + squash;
            var xz = 1f / Mathf.Sqrt(y);
            return new Vector3(n.x * xz, n.y * y, n.z * xz) * (Radius * (1f + ripple));
        }

        private static Vector3 Sphere(float u, float v)
        {
            var theta = 2f * Mathf.PI * u;
            var phi = Mathf.PI * v;
            return new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
        }
    }
}
