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
        private const float ArcPeakFactor = 4f;
        private const float SquashAmplitude = 0.35f;
        private const float SquashDamping = 2f;
        private const float SquashFrequency = 10f;
        private const float WobbleAmplitude = 0.08f;
        private const float WobbleFrequency = 12f;
        private const float RippleAmplitude = 0.04f;
        private const float RippleFrequency = 6f * Mathf.PI;
        private const float RippleSpeed = 1.5f;

        [MenuItem("VATyakov/Dev/Regenerate Drift Content")]
        private static void Regenerate()
        {
            VatAlembicFixtures.Record(Path, Frames, true, (mesh, frameIndex) => Jelly(mesh, frameIndex / VatAlembicFixtures.Fps));
            AssetDatabase.Refresh();
        }

        private static Mesh Jelly(Mesh mesh, float time)
        {
            var center = Center(time);
            var landed = Mathf.Max(0f, time - Flight);
            var vertices = new Vector3[(Segments + 1) * (Rings + 1)];
            var uv = new Vector2[vertices.Length];
            for (var ring = 0; ring <= Rings; ring++)
            {
                for (var segment = 0; segment <= Segments; segment++)
                {
                    var u = segment / (float)Segments;
                    var v = ring / (float)Rings;
                    var normal = Sphere(u, v);
                    vertices[ring * (Segments + 1) + segment] = center + Deform(normal, u, time, landed);
                    uv[ring * (Segments + 1) + segment] = new Vector2(u, v);
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

        private static Vector3 Center(float time)
        {
            var progress = Mathf.Clamp01(time / Flight);
            return new Vector3(Distance * progress, Radius + ArcPeakFactor * Height * progress * (1f - progress), 0f);
        }

        private static Vector3 Deform(Vector3 normal, float u, float time, float landed)
        {
            var ripple = RippleAmplitude * Mathf.Sin(RippleFrequency * u + RippleSpeed * time) * (1f - normal.y * normal.y);
            var stretch = 1f + Squash(time, landed);
            var squeeze = 1f / Mathf.Sqrt(stretch);
            return new Vector3(normal.x * squeeze, normal.y * stretch, normal.z * squeeze) * (Radius * (1f + ripple));
        }

        private static float Squash(float time, float landed)
        {
            return landed > 0f
                ? SquashAmplitude * Mathf.Exp(-SquashDamping * landed) * Mathf.Sin(SquashFrequency * landed)
                : WobbleAmplitude * Mathf.Sin(WobbleFrequency * time);
        }

        private static Vector3 Sphere(float u, float v)
        {
            var theta = 2f * Mathf.PI * u;
            var phi = Mathf.PI * v;
            return new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
        }
    }
}
