using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatDriftTests
    {
        private const int VertexCount = 600;
        private const float Fps = 30f;
        private const float Travel = 40f;
        private const float Radius = 0.5f;
        private const float GoldenAngle = 2.39996323f;
        private const int SampleCount = 10000;

        [Test]
        public void Codec_HiLoPair_KeepsAbout22Bits([Values(0.001f, 1.7f, 23.4567f, 777.7f, 30000f)] float scale)
        {
            var random = new Random(1);
            for (var i = 0; i < SampleCount; i++)
            {
                var drift = VatTestUtil.NextSignedVector(random) * scale;
                var (hi, lo) = VatDriftCodec.Encode(drift);
                Assert.AreEqual(new VatHalf3(drift).X, hi.X, "hi = half(d)");
                var decoded = VatDriftCodec.Decode(hi, lo);
                for (var c = 0; c < 3; c++)
                {
                    Assert.LessOrEqual(Mathf.Abs(decoded[c] - drift[c]), Mathf.Abs(drift[c]) / (1 << 21) + 3e-8f, $"component {c} of {drift}");
                }
            }
        }

        [Test]
        public void Codec_Zero_IsZero()
        {
            var (hi, lo) = VatDriftCodec.Encode(Vector3.zero);
            Assert.AreEqual(Vector3.zero, VatDriftCodec.Decode(hi, lo), "zero decodes exactly");
        }

        [Test]
        public void FlyingBody_DecodesWithinATenthOfAMillimeter()
        {
            var run = new Run(Travel);
            try
            {
                Assert.Greater(run.Precision.MaxDrift, Travel - 1f, "the drift covers the travel");
                Assert.Less(run.Precision.Error, 1e-4f, "without drift half of 40 m would jitter by millimeters");
                AssertDriftRows(run);
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Precision.Error + 1e-6f), "decode error within the reported precision");
            }
            finally
            {
                run.Destroy();
            }
        }

        [Test]
        public void InPlaceBody_StoresTheCentroidWobble()
        {
            var run = new Run(0f);
            try
            {
                Assert.Less(run.Precision.MaxDrift, 0.1f, "the centroid only wobbles");
                Assert.Less(run.Precision.Error, 1e-4f, "precision of the in-place body");
                AssertDriftRows(run);
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Precision.Error + 1e-6f), "decode error within the reported precision");
            }
            finally
            {
                run.Destroy();
            }
        }

        private static void AssertDriftRows(Run run)
        {
            var restCentroid = VatCentroid.Of(run.Frames[0].Positions);
            for (var k = 0; k < run.Frames.Length; k++)
            {
                var expected = VatCentroid.Of(run.Frames[k].Positions) - restCentroid;
                var decoded = VatTestUtil.DecodeDrift(run.DriftTexels, k);
                Assert.Less((decoded - expected).magnitude, 1e-5f, $"drift of frame {k}");
            }
        }

        private static VatFrame Frame(float progress, float travel)
        {
            var frame = new VatFrame(VertexCount);
            var center = new Vector3(travel * progress, 0.4f * travel * progress * (1f - progress), 0f);
            for (var v = 0; v < VertexCount; v++)
            {
                var normal = SpherePoint(v);
                var wobble = 1f + 0.1f * Mathf.Sin(9f * progress + 3f * normal.y) * Mathf.Sin(5f * normal.x);
                frame.Positions[v] = center + normal * (Radius * wobble);
                frame.Normals[v] = normal;
                var tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                frame.Tangents[v] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
            }

            return frame;
        }

        private static Vector3 SpherePoint(int index)
        {
            var height = 1f - 2f * (index + 0.5f) / VertexCount;
            var ringRadius = Mathf.Sqrt(1f - height * height);
            var phi = index * GoldenAngle;
            return new Vector3(ringRadius * Mathf.Cos(phi), height, ringRadius * Mathf.Sin(phi));
        }

        private sealed class Run
        {
            public readonly VatFrame[] Frames;
            public readonly VatPrecision Precision;
            public readonly byte[] DriftTexels;
            private readonly VatLayout _layout;
            private readonly Mesh _mesh;
            private readonly VatBakeTextures _textures;
            private readonly byte[] _positionTexels;

            public Run(float travel)
            {
                var layout = VatLayout.ForVertex(VertexCount, new[] { new VatClipRequest("Fly", 2f, Fps, isLooping: false) });
                var source = new VatSourceMesh("Body", VertexCount, null,
                    new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
                var encoder = new VatVertexEncoder(layout, source);
                Frames = new VatFrame[layout.Clips[0].FrameCount];
                for (var k = 0; k < Frames.Length; k++)
                {
                    Frames[k] = Frame(k / (Frames.Length - 1f), travel);
                    encoder.AddFrame(0, k, Frames[k]);
                }

                _layout = encoder.Layout;
                Precision = encoder.Precision;
                _mesh = encoder.BuildMesh("Body");
                _textures = VatBakeTextures.Build(encoder, "Body");
                Assert.AreEqual(VatMath.DriftWidth, _textures.Drift.width, "drift texture width");
                Assert.AreEqual(Frames.Length, _textures.Drift.height, "one drift row per frame");
                _positionTexels = VatTestUtil.ReadGpu(_textures.Position);
                DriftTexels = VatTestUtil.ReadGpu(_textures.Drift);
            }

            public float MaxDecodeError()
            {
                var rest = _mesh.vertices;
                var bounds = _mesh.bounds;
                bounds.Expand(1e-5f);
                var max = 0f;
                for (var k = 0; k < Frames.Length; k++)
                {
                    var drift = VatTestUtil.DecodeDrift(DriftTexels, k);
                    for (var v = 0; v < VertexCount; v++)
                    {
                        var decoded = rest[v] + drift + VatTestUtil.DecodeOffset(_positionTexels, _layout.Info, v, k);
                        Assert.IsTrue(bounds.Contains(decoded), $"vertex {v} of frame {k} outside bounds");
                        max = Mathf.Max(max, (decoded - Frames[k].Positions[v]).magnitude);
                    }
                }

                return max;
            }

            public void Destroy()
            {
                Object.DestroyImmediate(_mesh);
                _textures.Destroy();
            }
        }
    }
}
