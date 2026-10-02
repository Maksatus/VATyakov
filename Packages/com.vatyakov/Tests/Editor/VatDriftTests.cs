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

        [Test]
        public void Codec_HiLoPair_KeepsAbout22Bits([Values(0.001f, 1.7f, 23.4567f, 777.7f, 30000f)] float scale)
        {
            var random = new Random(1);
            for (var i = 0; i < 10000; i++)
            {
                var d = new Vector3(Next(random), Next(random), Next(random)) * scale;
                var (hi, lo) = VatDriftCodec.Encode(d);
                Assert.AreEqual(new VatHalf3(d).X, hi.X, "hi = half(d)");
                var decoded = VatDriftCodec.Decode(hi, lo);
                for (var c = 0; c < 3; c++)
                {
                    Assert.LessOrEqual(Mathf.Abs(decoded[c] - d[c]), Mathf.Abs(d[c]) / (1 << 21) + 3e-8f, $"component {c} of {d}");
                }
            }
        }

        [Test]
        public void Codec_Zero_IsZero()
        {
            var (hi, lo) = VatDriftCodec.Encode(Vector3.zero);
            Assert.AreEqual(Vector3.zero, VatDriftCodec.Decode(hi, lo));
        }

        [Test]
        public void FlyingBody_DecodesWithinATenthOfAMillimeter()
        {
            var run = new Run(Travel);
            try
            {
                Assert.Greater(run.Precision.MaxDrift, Travel - 1f);
                Assert.Less(run.Precision.Error, 1e-4f, "without drift half of 40 m would jitter by millimeters");
                AssertDriftRows(run);
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Precision.Error + 1e-6f));
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
                Assert.Less(run.Precision.MaxDrift, 0.1f);
                Assert.Less(run.Precision.Error, 1e-4f);
                AssertDriftRows(run);
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Precision.Error + 1e-6f));
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

        private static VatFrame Frame(float t, float travel)
        {
            var frame = new VatFrame(VertexCount);
            var center = new Vector3(travel * t, 0.4f * travel * t * (1f - t), 0f);
            for (var v = 0; v < VertexCount; v++)
            {
                var n = SpherePoint(v);
                var wobble = 1f + 0.1f * Mathf.Sin(9f * t + 3f * n.y) * Mathf.Sin(5f * n.x);
                frame.Positions[v] = center + n * (Radius * wobble);
                frame.Normals[v] = n;
                var tangent = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                frame.Tangents[v] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
            }

            return frame;
        }

        private static Vector3 SpherePoint(int i)
        {
            var y = 1f - 2f * (i + 0.5f) / VertexCount;
            var r = Mathf.Sqrt(1f - y * y);
            var phi = i * 2.39996323f;
            return new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi));
        }

        private static float Next(Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }

        private sealed class Run
        {
            public readonly VertexEncoder Encoder;
            public readonly VatFrame[] Frames;
            public readonly VatPrecision Precision;
            public readonly byte[] DriftTexels;
            private readonly VatLayout _layout;
            private readonly Mesh _mesh;
            private readonly VatBakeTextures _textures;
            private readonly byte[] _positionTexels;

            public Run(float travel)
            {
                var layout = VatLayout.ForVertex(VertexCount, new[] { new VatClipRequest("Fly", 2f, Fps, loop: false) });
                var source = new VatSourceMesh("Body", VertexCount, null,
                    new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
                Encoder = new VertexEncoder(layout, source);
                Frames = new VatFrame[layout.Clips[0].FrameCount];
                for (var k = 0; k < Frames.Length; k++)
                {
                    Encoder.AddFrame(0, k, Frames[k] = Frame(k / (Frames.Length - 1f), travel));
                }

                _layout = Encoder.Layout;
                Precision = Encoder.Precision;
                _mesh = Encoder.BuildMesh("Body");
                _textures = VatBakeTextures.Build(Encoder, "Body");
                Assert.AreEqual(VatMath.DriftWidth, _textures.Drift.width);
                Assert.AreEqual(Frames.Length, _textures.Drift.height);
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
                    var d = VatTestUtil.DecodeDrift(DriftTexels, k);
                    for (var v = 0; v < VertexCount; v++)
                    {
                        var decoded = rest[v] + d + VatTestUtil.DecodeOffset(_positionTexels, _layout.Info, v, k);
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
