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
        private const float Fps = 30f;
        private const float MaxAngle = 0.25f;
        private const int FlipPeriod = 1000;
        private const int FlippedCount = VertexCount / FlipPeriod;
        private const int NegativeSignPeriod = 3;
        private const int DegeneratePeriod = 7;

        [Test]
        public void RotationTexture_DecodesToTheFrameOfEveryVertex()
        {
            var layout = VatLayout.ForVertex(VertexCount, new[]
            {
                new VatClipRequest("A", 0.1f, Fps),
                new VatClipRequest("B", 2f / Fps, Fps),
            });
            var source = new VatSourceMesh("Synthetic", VertexCount, null,
                new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
            var encoder = new VatVertexEncoder(layout, source);
            var frames = new VatFrame[layout.Clips.Sum(clip => clip.FrameCount)];
            var frameIndex = 0;
            for (var c = 0; c < layout.Clips.Length; c++)
            {
                for (var k = 0; k < layout.Clips[c].FrameCount; k++, frameIndex++)
                {
                    frames[frameIndex] = Frame(frameIndex);
                    encoder.AddFrame(c, k, frames[frameIndex]);
                }
            }

            var mesh = encoder.BuildMesh("Synthetic");
            var texture = encoder.BuildRotationTexture("Synthetic");
            try
            {
                Assert.AreEqual(VatVertexFormat.Rotation, texture.graphicsFormat, "rotation texture format");
                AssertRows(layout, texture.GetPixelData<Color32>(0).ToArray(), frames);
                AssertRestFrame(mesh, frames[0]);
                Assert.AreEqual(frames.Sum(DegenerateCount), encoder.Stats.DegenerateTangents, "degenerate tangents are counted");
                Assert.Less(encoder.Stats.MaxRotationError, MaxAngle, "max rotation error, degrees");
                Assert.AreEqual(FlippedCount, encoder.Chirality.Count, "vertices whose bitangent sign flips");
                StringAssert.Contains("vertex 1,", encoder.Chirality.First, "the first flipped vertex is reported");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(texture);
            }
        }

        private static void AssertRows(VatLayout layout, Color32[] texels, VatFrame[] frames)
        {
            var frameIndex = 0;
            foreach (var clip in layout.Clips)
            {
                for (var k = 0; k < clip.FrameCount; k++, frameIndex++)
                {
                    for (var v = 0; v < VertexCount; v++)
                    {
                        var texel = VatMath.Texel(v, layout.Info.Width, layout.Info.TotalRows, clip.StartRow + k);
                        var rotation = VatMath.DecodeRotation((Color)texels[texel.y * layout.Info.Width + texel.x]);
                        AssertFrame(frames[frameIndex], v, VatMath.FrameNormal(rotation), VatMath.FrameTangent(rotation));
                    }
                }
            }
        }

        private static void AssertFrame(VatFrame frame, int vertex, Vector3 normal, Vector3 tangent)
        {
            var expected = frame.Normals[vertex].normalized;
            Assert.Less(Vector3.Angle(expected, normal), MaxAngle, $"normal of vertex {vertex}");
            Assert.AreEqual(0f, Vector3.Dot(normal, tangent), 1e-5f, "orthogonal frame");
            if (VatTangentFrames.TryOrthogonalize(expected, frame.Tangents[vertex], out var expectedTangent))
            {
                Assert.Less(Vector3.Angle(expectedTangent, tangent), MaxAngle, $"tangent of vertex {vertex}");
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
                Assert.AreEqual(frame.Tangents[v].w < 0f ? -1f : 1f, tangents[v].w, $"bitangent sign of {v}");
            }
        }

        private static VatFrame Frame(int frameIndex)
        {
            var random = new Random(frameIndex);
            var frame = new VatFrame(VertexCount);
            for (var v = 0; v < VertexCount; v++)
            {
                frame.Normals[v] = VatTestUtil.RandomDirection(random) * (0.5f + (float)random.NextDouble());
                var tangent = VatTestUtil.RandomDirection(random);
                var sign = v % NegativeSignPeriod == 0 ? -1f : 1f;
                if (frameIndex > 0 && v % FlipPeriod == 1)
                {
                    sign = -sign;
                }

                frame.Tangents[v] = IsDegenerate(frameIndex, v) ? Vector4.zero : new Vector4(tangent.x, tangent.y, tangent.z, sign);
                if (IsDegenerate(frameIndex, v))
                {
                    frame.Tangents[v].w = sign;
                }
            }

            return frame;
        }

        private static bool IsDegenerate(int frameIndex, int vertex)
        {
            return frameIndex % 2 == 1 && vertex % DegeneratePeriod == 0;
        }

        private static int DegenerateCount(VatFrame frame)
        {
            return Enumerable.Range(0, VertexCount)
                .Count(vertex => !VatTangentFrames.TryOrthogonalize(frame.Normals[vertex].normalized, frame.Tangents[vertex], out _));
        }
    }
}
