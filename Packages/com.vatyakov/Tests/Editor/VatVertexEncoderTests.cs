using System.Linq;
using VATyakov.Editor;
using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    // VertexEncoder on synthetic frames: _VatRotTex rows, the rest frame in the mesh, degenerate tangents and chirality.
    public class VatVertexEncoderTests
    {
        const int VertexCount = 5000; // two blocks
        const float MaxAngle = 0.25f; // degrees

        [Test]
        public void RotationTexture_DecodesToTheFrameOfEveryVertex()
        {
            var layout = VatLayout.ForVertex(VertexCount, new[]
            {
                new VatClipRequest("A", 0.1f, 30f), // 3 frames
                new VatClipRequest("B", 2f / 30f, 30f), // 2 frames
            });
            var source = new VatSourceMesh("Synthetic", VertexCount, null,
                new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
            var encoder = new VertexEncoder(layout, source);
            var frames = new VatFrame[5];
            int f = 0;
            for (int c = 0; c < layout.Clips.Length; c++)
                for (int k = 0; k < layout.Clips[c].FrameCount; k++, f++)
                    encoder.AddFrame(c, k, frames[f] = Frame(f));

            var mesh = encoder.BuildMesh("Synthetic");
            var texture = encoder.BuildRotationTexture("Synthetic");
            try
            {
                Assert.AreEqual(VatVertexFormat.Rotation, texture.graphicsFormat);
                AssertRows(layout, texture.GetPixelData<Color32>(0).ToArray(), frames);
                AssertRestFrame(mesh, frames[0]);
                Assert.AreEqual(frames.Sum(DegenerateCount), encoder.Stats.DegenerateTangents);
                Assert.Less(encoder.Stats.MaxRotationError, MaxAngle);
                Assert.AreEqual(Flipped, encoder.Chirality.Count, "vertices whose bitangent sign flips");
                StringAssert.Contains("вертекс 1,", encoder.Chirality.First);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(texture);
            }
        }

        static void AssertRows(VatLayout layout, Color32[] texels, VatFrame[] frames)
        {
            int f = 0;
            foreach (var clip in layout.Clips)
            {
                for (int k = 0; k < clip.FrameCount; k++, f++)
                {
                    for (int v = 0; v < VertexCount; v++)
                    {
                        var texel = VatMath.Texel(v, layout.Info.Width, layout.Info.TotalRows, clip.StartRow + k);
                        var q = VatMath.DecodeRotation((Color)texels[texel.y * layout.Info.Width + texel.x]);
                        AssertFrame(frames[f], v, VatMath.FrameNormal(q), VatMath.FrameTangent(q));
                    }
                }
            }
        }

        static void AssertFrame(VatFrame frame, int v, Vector3 n, Vector3 t)
        {
            var expected = frame.Normals[v].normalized;
            Assert.Less(Vector3.Angle(expected, n), MaxAngle, $"normal of vertex {v}");
            Assert.AreEqual(0f, Vector3.Dot(n, t), 1e-5f, "orthogonal frame");
            if (VatTangentFrames.TryOrthogonalize(expected, frame.Tangents[v], out var tangent))
                Assert.Less(Vector3.Angle(tangent, t), MaxAngle, $"tangent of vertex {v}");
        }

        // Mesh normal and tangent are the orthonormal frame 0, tangent.w keeps the sign (Float16).
        static void AssertRestFrame(Mesh mesh, VatFrame frame)
        {
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            for (int v = 0; v < VertexCount; v++)
            {
                Assert.Less((normals[v] - frame.Normals[v].normalized).magnitude, 2e-3f, $"rest normal of {v}");
                Assert.AreEqual(0f, Vector3.Dot(normals[v], tangents[v]), 2e-3f, $"rest tangent of {v}");
                Assert.AreEqual(frame.Tangents[v].w < 0f ? -1f : 1f, tangents[v].w);
            }
        }

        // Normals of any length, tangents not orthogonal to them; every 7th vertex has a degenerate tangent
        // on odd frames, vertices 1, 1001, … flip the bitangent sign after frame 0.
        static VatFrame Frame(int f)
        {
            var random = new System.Random(f);
            var frame = new VatFrame(VertexCount);
            for (int v = 0; v < VertexCount; v++)
            {
                frame.Normals[v] = RandomVector(random) * (0.5f + (float)random.NextDouble());
                var t = RandomVector(random);
                float w = v % 3 == 0 ? -1f : 1f;
                if (f > 0 && v % 1000 == 1)
                    w = -w;
                frame.Tangents[v] = IsDegenerate(f, v) ? Vector4.zero : new Vector4(t.x, t.y, t.z, w);
                if (IsDegenerate(f, v))
                    frame.Tangents[v].w = w;
            }
            return frame;
        }

        static bool IsDegenerate(int f, int v) => f % 2 == 1 && v % 7 == 0;

        static int DegenerateCount(VatFrame frame) =>
            Enumerable.Range(0, VertexCount).Count(v => !VatTangentFrames.TryOrthogonalize(frame.Normals[v].normalized, frame.Tangents[v], out _));

        static int Flipped => VertexCount / 1000;

        static Vector3 RandomVector(System.Random random)
        {
            Vector3 v;
            do
            {
                v = new Vector3(Next(random), Next(random), Next(random));
            } while (v.sqrMagnitude < 1e-2f || v.sqrMagnitude > 1f);
            return v.normalized;
        }

        static float Next(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);
    }
}
