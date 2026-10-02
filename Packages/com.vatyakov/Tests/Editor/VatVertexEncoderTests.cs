using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatVertexEncoderTests
    {
        private const int VertexCount = 5000;
        private const float MaxAngle = 0.25f;

        private static int Flipped => VertexCount / 1000;

        [Test]
        public void RotationTexture_DecodesToTheFrameOfEveryVertex()
        {
            var layout = VatLayout.ForVertex(VertexCount, new[]
            {
                new VatClipRequest("A", 0.1f, 30f),
                new VatClipRequest("B", 2f / 30f, 30f),
            });
            var source = new VatSourceMesh("Synthetic", VertexCount, null,
                new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
            var encoder = new VertexEncoder(layout, source);
            var frames = new VatFrame[5];
            var f = 0;
            for (var c = 0; c < layout.Clips.Length; c++)
            {
                for (var k = 0; k < layout.Clips[c].FrameCount; k++, f++)
                {
                    encoder.AddFrame(c, k, frames[f] = Frame(f));
                }
            }

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
                StringAssert.Contains("vertex 1,", encoder.Chirality.First);
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(texture);
            }
        }

        private static void AssertRows(VatLayout layout, Color32[] texels, VatFrame[] frames)
        {
            var f = 0;
            foreach (var clip in layout.Clips)
            {
                for (var k = 0; k < clip.FrameCount; k++, f++)
                {
                    for (var v = 0; v < VertexCount; v++)
                    {
                        var texel = VatMath.Texel(v, layout.Info.Width, layout.Info.TotalRows, clip.StartRow + k);
                        var q = VatMath.DecodeRotation((Color)texels[texel.y * layout.Info.Width + texel.x]);
                        AssertFrame(frames[f], v, VatMath.FrameNormal(q), VatMath.FrameTangent(q));
                    }
                }
            }
        }

        private static void AssertFrame(VatFrame frame, int v, Vector3 n, Vector3 t)
        {
            var expected = frame.Normals[v].normalized;
            Assert.Less(Vector3.Angle(expected, n), MaxAngle, $"normal of vertex {v}");
            Assert.AreEqual(0f, Vector3.Dot(n, t), 1e-5f, "orthogonal frame");
            if (VatTangentFrames.TryOrthogonalize(expected, frame.Tangents[v], out var tangent))
            {
                Assert.Less(Vector3.Angle(tangent, t), MaxAngle, $"tangent of vertex {v}");
            }
        }

        private static void AssertRestFrame(Mesh mesh, VatFrame frame)
        {
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            for (var v = 0; v < VertexCount; v++)
            {
                Assert.Less((normals[v] - frame.Normals[v].normalized).magnitude, 2e-3f, $"rest normal of {v}");
                Assert.AreEqual(0f, Vector3.Dot(normals[v], tangents[v]), 2e-3f, $"rest tangent of {v}");
                Assert.AreEqual(frame.Tangents[v].w < 0f ? -1f : 1f, tangents[v].w);
            }
        }

        private static VatFrame Frame(int f)
        {
            var random = new Random(f);
            var frame = new VatFrame(VertexCount);
            for (var v = 0; v < VertexCount; v++)
            {
                frame.Normals[v] = RandomVector(random) * (0.5f + (float)random.NextDouble());
                var t = RandomVector(random);
                var w = v % 3 == 0 ? -1f : 1f;
                if (f > 0 && v % 1000 == 1)
                {
                    w = -w;
                }

                frame.Tangents[v] = IsDegenerate(f, v) ? Vector4.zero : new Vector4(t.x, t.y, t.z, w);
                if (IsDegenerate(f, v))
                {
                    frame.Tangents[v].w = w;
                }
            }

            return frame;
        }

        private static bool IsDegenerate(int f, int v)
        {
            return f % 2 == 1 && v % 7 == 0;
        }

        private static int DegenerateCount(VatFrame frame)
        {
            return Enumerable.Range(0, VertexCount).Count(v => !VatTangentFrames.TryOrthogonalize(frame.Normals[v].normalized, frame.Tangents[v], out _));
        }

        private static Vector3 RandomVector(Random random)
        {
            Vector3 v;
            do
            {
                v = new Vector3(Next(random), Next(random), Next(random));
            } while (v.sqrMagnitude < 1e-2f || v.sqrMagnitude > 1f);
            return v.normalized;
        }

        private static float Next(Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }
    }
}
