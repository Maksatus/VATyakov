using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatPositionEncodingTests
    {
        private const int VertexCount = 64;
        private const int GridWidth = 8;
        private const int FrameCount = 12;
        private const float Fps = 30f;
        private const float Spacing = 0.1f;
        private const float Swing = 0.2f;
        private const float Flight = 10f;
        private const float Epsilon = 1e-5f;

        [Test]
        public void SmallMotion_BakesBytes_WithinTheTolerance()
        {
            var run = new Run(Wave, VatPositionEncoding.ByteTolerance);
            try
            {
                Assert.AreEqual(VatPositionFormat.Byte, run.Positions.Format, "position format");
                Assert.AreEqual(VatVertexFormat.BytePosition, run.Texture.graphicsFormat, "position texture format");
                Assert.Less(run.Positions.MaxError, VatPositionEncoding.ByteTolerance, "error within the tolerance");
                Assert.AreEqual(run.Positions.ByteError, run.Positions.MaxError, Epsilon, "the reported error is the 8-bit error");
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Positions.MaxError + Epsilon), "decode error within the reported precision");
            }
            finally
            {
                run.Destroy();
            }
        }

        [Test]
        public void VertexFlyingTenMeters_KeepsHalf_AndReportsTheByteError()
        {
            var run = new Run(WaveWithFlyingVertex, VatPositionEncoding.ByteTolerance);
            try
            {
                Assert.AreEqual(VatPositionFormat.Half, run.Positions.Format, "position format");
                Assert.AreEqual(VatVertexFormat.HalfPosition, run.Texture.graphicsFormat, "position texture format");
                Assert.AreEqual(VatPositionRange.Identity, run.Positions.Range, "half floats need no range");
                Assert.Greater(run.Positions.ByteError, VatPositionEncoding.ByteTolerance, "8-bit error over the tolerance");
                Assert.Less(run.Positions.MaxError, run.Positions.ByteError, "half floats are more precise here");
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Positions.MaxError + Epsilon), "decode error within the reported precision");
            }
            finally
            {
                run.Destroy();
            }
        }

        [Test]
        public void AxisWithoutMotion_HasZeroSize_AndDecodesWithoutNaN()
        {
            var run = new Run(SwayAlongX, VatPositionEncoding.ByteTolerance);
            try
            {
                Assert.AreEqual(VatPositionFormat.Byte, run.Positions.Format, "position format");
                Assert.Greater(run.Positions.Range.Size.x, 0f, "the moving axis has a size");
                Assert.AreEqual(0f, run.Positions.Range.Size.y, "no motion along y");
                Assert.AreEqual(0f, run.Positions.Range.Size.z, "no motion along z");
                Assert.That(run.MaxDecodeError(), Is.LessThanOrEqualTo(run.Positions.MaxError + Epsilon), "decode error within the reported precision");
            }
            finally
            {
                run.Destroy();
            }
        }

        [Test]
        public void ZeroTolerance_KeepsHalf_EvenWithoutError()
        {
            var run = new Run(Still, 0f);
            try
            {
                Assert.AreEqual(0f, run.Positions.ByteError, "a still mesh loses nothing in bytes");
                Assert.AreEqual(VatPositionFormat.Half, run.Positions.Format, "zero tolerance always keeps half floats");
            }
            finally
            {
                run.Destroy();
            }
        }

        private static Vector3 Still(int vertex, int frame)
        {
            return new Vector3(vertex % GridWidth, 0f, vertex / GridWidth) * Spacing;
        }

        private static Vector3 Wave(int vertex, int frame)
        {
            var phase = frame * 0.5f + vertex * 0.3f;
            return Still(vertex, frame) + new Vector3(0f, Swing * Mathf.Sin(phase), 0.5f * Swing * Mathf.Cos(phase));
        }

        private static Vector3 WaveWithFlyingVertex(int vertex, int frame)
        {
            var flight = vertex == 0 ? Flight * frame / (FrameCount - 1f) : 0f;
            return Wave(vertex, frame) + new Vector3(flight, 0f, 0f);
        }

        private static Vector3 SwayAlongX(int vertex, int frame)
        {
            return Still(vertex, frame) + new Vector3(Swing * Mathf.Sin(frame * 0.5f + vertex * 0.3f), 0f, 0f);
        }

        private sealed class Run
        {
            public readonly VatPositionEncoding Positions;
            public readonly Texture2D Texture;

            private readonly VatFrame[] _frames;
            private readonly Vector3[] _drift;
            private readonly VatLayout _layout;
            private readonly Mesh _mesh;

            public Run(Func<int, int, Vector3> position, float tolerance)
            {
                _layout = VatLayout.ForVertex(VertexCount, new[] { new VatClipRequest("Clip", (FrameCount - 1) / Fps, Fps, isLooping: false) });
                var source = new VatSourceMesh("Body", VertexCount, null,
                    new[] { new VatSourceSubMesh(Enumerable.Range(0, VertexCount).ToArray(), MeshTopology.Points) });
                var encoder = new VatVertexEncoder(_layout, source, tolerance);
                _frames = new VatFrame[FrameCount];
                for (var k = 0; k < FrameCount; k++)
                {
                    _frames[k] = Frame(position, k);
                    encoder.AddFrame(0, k, _frames[k]);
                }

                Positions = encoder.Positions;
                _drift = encoder.Drift;
                _mesh = encoder.BuildMesh("Body");
                Texture = encoder.BuildPositionTexture("Body");
            }

            public float MaxDecodeError()
            {
                var texels = VatTestUtil.ReadGpu(Texture);
                var rest = _mesh.vertices;
                var max = 0f;
                for (var k = 0; k < FrameCount; k++)
                {
                    for (var v = 0; v < VertexCount; v++)
                    {
                        var sample = VatTestUtil.PositionSample(texels, _layout.Info, Positions.Format, v, k);
                        var decoded = VatTestUtil.ShaderPosition(rest[v], _drift[k] + Positions.Range.Min, sample, Positions.Range);
                        Assert.IsFalse(float.IsNaN(decoded.x) || float.IsNaN(decoded.y) || float.IsNaN(decoded.z), $"vertex {v} of frame {k} is NaN");
                        max = Mathf.Max(max, (decoded - _frames[k].Positions[v]).magnitude);
                    }
                }

                return max;
            }

            public void Destroy()
            {
                Object.DestroyImmediate(_mesh);
                Object.DestroyImmediate(Texture);
            }

            private static VatFrame Frame(Func<int, int, Vector3> position, int frame)
            {
                var data = new VatFrame(VertexCount);
                for (var v = 0; v < VertexCount; v++)
                {
                    data.Positions[v] = position(v, frame);
                    data.Normals[v] = Vector3.up;
                    data.Tangents[v] = new Vector4(1f, 0f, 0f, 1f);
                }

                return data;
            }
        }
    }
}
