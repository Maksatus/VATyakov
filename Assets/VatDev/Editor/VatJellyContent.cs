using UnityEditor;
using UnityEngine;

namespace VATyakov.Dev
{
    // Drift content (1.5): a jelly ball flies 40 m in 1.5 s, lands and keeps wobbling slowly.
    // Without drift its offsets sit in the 32–64 m half range (31 mm step), so the slow wobble at the end jitters.
    static class VatJellyContent
    {
        const string Path = "Assets/VatDev/Content/Alembic/Jelly.abc";
        const int Frames = 121; // 4 s at 30 fps
        const float Flight = 1.5f;
        const float Distance = 40f;
        const float Height = 6f;
        const float Radius = 0.5f;
        const int Segments = 48;
        const int Rings = 24;

        [MenuItem("VATyakov/Dev/Regenerate Drift Content")]
        static void Regenerate()
        {
            VatAlembicFixtures.Record(Path, Frames, true, (mesh, k) => Jelly(mesh, k / VatAlembicFixtures.Fps));
            AssetDatabase.Refresh();
        }

        static Mesh Jelly(Mesh mesh, float t)
        {
            var center = Center(t);
            float landed = Mathf.Max(0f, t - Flight);
            var vertices = new Vector3[(Segments + 1) * (Rings + 1)];
            var uv = new Vector2[vertices.Length];
            for (int r = 0; r <= Rings; r++)
            for (int s = 0; s <= Segments; s++)
            {
                float u = s / (float)Segments, v = r / (float)Rings;
                var n = Sphere(u, v);
                vertices[r * (Segments + 1) + s] = center + Deform(n, u, t, landed);
                uv[r * (Segments + 1) + s] = new Vector2(u, v);
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = VatAlembicFixtures.Triangles(Segments + 1, Rings + 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Ballistic arc, then rest on the ground at Distance.
        static Vector3 Center(float t)
        {
            float a = Mathf.Clamp01(t / Flight);
            return new Vector3(Distance * a, Radius + 4f * Height * a * (1f - a), 0f);
        }

        // Squash and stretch that decays after landing, plus a slow ripple that never stops.
        static Vector3 Deform(Vector3 n, float u, float t, float landed)
        {
            float squash = landed > 0f ? 0.35f * Mathf.Exp(-2f * landed) * Mathf.Sin(10f * landed) : 0.08f * Mathf.Sin(12f * t);
            float ripple = 0.04f * Mathf.Sin(6f * Mathf.PI * u + 1.5f * t) * (1f - n.y * n.y);
            float y = 1f + squash;
            float xz = 1f / Mathf.Sqrt(y);
            return new Vector3(n.x * xz, n.y * y, n.z * xz) * (Radius * (1f + ripple));
        }

        static Vector3 Sphere(float u, float v)
        {
            float theta = 2f * Mathf.PI * u, phi = Mathf.PI * v;
            return new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
        }
    }
}
