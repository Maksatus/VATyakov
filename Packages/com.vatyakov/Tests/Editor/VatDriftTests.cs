using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    // 1.5: the hi/lo codec of _VatDriftTex and the automatic drift of the encoder (§1.2, §1.9, §2.2).
    public class VatDriftTests
    {
        const int VertexCount = 600;
        const float Fps = 30f;
        const float Travel = 40f; // meters: |Δ| without drift lands in the 32–64 m half range, step 31 mm
        const float Radius = 0.5f;

        // hi = half(d), |d − (hi + lo)| ≤ |d|·2⁻²¹ per component; a tiny lo is subnormal, its step is 2⁻²⁴.
        [Test]
        public void Codec_HiLoPair_KeepsAbout22Bits([Values(0.001f, 1.7f, 23.4567f, 777.7f, 30000f)] float scale)
        {
            var random = new System.Random(1);
            for (int i = 0; i < 10000; i++)
            {
                var d = new Vector3(Next(random), Next(random), Next(random)) * scale;
                var (hi, lo) = VatDriftCodec.Encode(d);
                Assert.AreEqual(new VatHalf3(d).X, hi.X, "hi = half(d)");
                var decoded = VatDriftCodec.Decode(hi, lo);
                for (int c = 0; c < 3; c++)
                    Assert.LessOrEqual(Mathf.Abs(decoded[c] - d[c]), Mathf.Abs(d[c]) / (1 << 21) + 3e-8f, $"component {c} of {d}");
            }
        }

        [Test]
        public void Codec_Zero_IsZero()
        {
            var (hi, lo) = VatDriftCodec.Encode(Vector3.zero);
            Assert.AreEqual(Vector3.zero, VatDriftCodec.Decode(hi, lo));
        }

        [TestCase(2.5f, 0f, true)]
        [TestCase(1.5f, 0.002f, true)]
        [TestCase(1.5f, 0.0005f, false)]
        public void Policy_TurnsDriftOnByDistanceOrError(float maxDrift, float errorWithoutDrift, bool expected)
        {
            Assert.AreEqual(expected, VatDriftPolicy.IsNeeded(maxDrift, errorWithoutDrift));
        }

        [Test]
        public void FlyingBody_TurnsDriftOn_AndDecodesWithinATenthOfAMillimeter()
        {
            var run = new Run(Travel, allowDrift: true);
            try
            {
                Assert.IsTrue(run.Encoder.DriftOn);
                Assert.IsTrue(run.Encoder.Layout.Info.Drift);
                Assert.AreEqual(1f, run.Encoder.Layout.Info.ShaderLayout.z, "driftOn in _VatLayout");
                Assert.Greater(run.Precision.MaxDrift, Travel - 1f);
                Assert.Greater(run.Precision.ErrorWithoutDrift, 5e-3f, "half of 40 m jitters by millimeters");
                Assert.Less(run.Precision.ErrorWithDrift, 1e-4f);
                AssertDriftRows(run);
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Precision.ErrorWithDrift + 1e-6f));
            }
            finally
            {
                run.Destroy();
            }
        }

        [Test]
        public void InPlaceBody_KeepsDriftOff_AndTheTextureHoldsZeros()
        {
            var run = new Run(0f, allowDrift: true);
            try
            {
                Assert.IsFalse(run.Encoder.DriftOn);
                Assert.AreEqual(0f, run.Encoder.Layout.Info.ShaderLayout.z);
                Assert.Less(run.Precision.ErrorWithoutDrift, 1e-3f);
                Assert.IsTrue(run.DriftTexels.All(b => b == 0), "zeros: the shader adds d anyway");
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Precision.ErrorWithoutDrift + 1e-6f));
            }
            finally
            {
                run.Destroy();
            }
        }

        // The dev comparison of the Drift scene: same flight, no drift.
        [Test]
        public void DriftNotAllowed_KeepsItOff_WithTheErrorWithoutDrift()
        {
            var run = new Run(Travel, allowDrift: false);
            try
            {
                Assert.IsFalse(run.Encoder.DriftOn);
                Assert.IsTrue(run.DriftTexels.All(b => b == 0));
                float error = run.MaxDecodeError();
                Assert.Greater(error, 5e-3f);
                Assert.AreEqual(run.Precision.ErrorWithoutDrift, error, 1e-6f);
            }
            finally
            {
                run.Destroy();
            }
        }

        static void AssertDriftRows(Run run)
        {
            var restCentroid = VatCentroid.Of(run.Frames[0].Positions);
            for (int k = 0; k < run.Frames.Length; k++)
            {
                var expected = VatCentroid.Of(run.Frames[k].Positions) - restCentroid;
                var decoded = VatTestUtil.DecodeDrift(run.DriftTexels, k);
                Assert.Less((decoded - expected).magnitude, 1e-5f, $"drift of frame {k}");
            }
        }

        // Synthetic soft body: a wobbling sphere that flies `travel` meters along X over one clip.
        sealed class Run
        {
            public readonly VertexEncoder Encoder;
            public readonly VatFrame[] Frames;
            public readonly VatPrecision Precision;
            public readonly byte[] DriftTexels;
            readonly VatLayout _layout;
            readonly Mesh _mesh;
            readonly VatBakeTextures _textures;
            readonly byte[] _positionTexels;

            public Run(float travel, bool allowDrift)
            {
                var layout = VatLayout.ForVertex(VertexCount, new[] { new VatClipRequest("Fly", 2f, Fps, loop: false) });
                var source = new VatSourceMesh("Body", VertexCount, null,
                    new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
                Encoder = new VertexEncoder(layout, source, allowDrift);
                Frames = new VatFrame[layout.Clips[0].FrameCount];
                for (int k = 0; k < Frames.Length; k++)
                    Encoder.AddFrame(0, k, Frames[k] = Frame(k / (Frames.Length - 1f), travel));

                _layout = Encoder.Layout;
                Precision = Encoder.Precision;
                _mesh = Encoder.BuildMesh("Body");
                _textures = VatBakeTextures.Build(Encoder, "Body");
                Assert.DoesNotThrow(() => VatLayoutVerifier.Verify(_layout, _mesh, _textures));
                Assert.AreEqual(VatMath.DriftWidth, _textures.Drift.width);
                Assert.AreEqual(Frames.Length, _textures.Drift.height);
                _positionTexels = VatTestUtil.ReadGpu(_textures.Position);
                DriftTexels = VatTestUtil.ReadGpu(_textures.Drift);
            }

            // rest + d + Δ as the shader reads it, against the source; every decoded point is inside the mesh bounds.
            public float MaxDecodeError()
            {
                var rest = _mesh.vertices;
                var bounds = _mesh.bounds;
                bounds.Expand(1e-5f);
                float max = 0f;
                for (int k = 0; k < Frames.Length; k++)
                {
                    var d = VatTestUtil.DecodeDrift(DriftTexels, k);
                    for (int v = 0; v < VertexCount; v++)
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

        static VatFrame Frame(float t, float travel)
        {
            var frame = new VatFrame(VertexCount);
            var center = new Vector3(travel * t, 0.4f * travel * t * (1f - t), 0f); // an arc 0.1·travel high
            for (int v = 0; v < VertexCount; v++)
            {
                var n = SpherePoint(v);
                float wobble = 1f + 0.1f * Mathf.Sin(9f * t + 3f * n.y) * Mathf.Sin(5f * n.x);
                frame.Positions[v] = center + n * (Radius * wobble);
                frame.Normals[v] = n;
                var tangent = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                frame.Tangents[v] = new Vector4(tangent.x, tangent.y, tangent.z, 1f);
            }
            return frame;
        }

        // Fibonacci sphere.
        static Vector3 SpherePoint(int i)
        {
            float y = 1f - 2f * (i + 0.5f) / VertexCount;
            float r = Mathf.Sqrt(1f - y * y);
            float phi = i * 2.39996323f;
            return new Vector3(r * Mathf.Cos(phi), y, r * Mathf.Sin(phi));
        }

        static float Next(System.Random random) => (float)(random.NextDouble() * 2.0 - 1.0);
    }
}
