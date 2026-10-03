using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatDriftTests
    {
        private const int VertexCount = 600;
        private const float Fps = 30f;
        private const float Travel = 40f;
        private const float Radius = 0.5f;
        private const float GoldenAngle = 2.39996323f;
        private const float Tolerance = 1e-5f;

        private static readonly Vector3[] _rows = { Vector3.zero, new(10f, 0f, 0f), new(10f, 4f, 0f), new(-2f, 0f, 6f) };

        [Test]
        public void Drift_LerpsTheTwoRowsByFrac()
        {
            var asset = DriftAsset();
            try
            {
                AssertDrift(new Vector3(5f, 0f, 0f), asset.Drift(new Vector4(0f, 1f, 0.5f, 0f)), "half way from row 0 to row 1");
                AssertDrift(new Vector3(10f, 1f, 0f), asset.Drift(new Vector4(1f, 2f, 0.25f, 0f)), "a quarter from row 1 to row 2");
                AssertDrift(_rows[3], asset.Drift(VatTestUtil.Row(3)), "a whole row");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Drift_BlendsTheTransitionByWeight()
        {
            var asset = DriftAsset();
            try
            {
                var frame = new Vector4(0f, 1f, 0.5f, 0f);
                var frameB = new Vector4(2f, 3f, 0.5f, 0.25f);
                AssertDrift(new Vector3(4.75f, 0.5f, 0.75f), asset.Drift(frame, frameB), "a quarter from clip A (5, 0, 0) to clip B (4, 2, 3)");
                frameB.w = 1f;
                AssertDrift(asset.Drift(frameB), asset.Drift(frame, frameB), "the finished transition is clip B");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void Drift_WithoutTransition_IsTheDriftOfClipA()
        {
            var asset = DriftAsset();
            try
            {
                var frame = new Vector4(1f, 2f, 0.75f, 0f);
                AssertDrift(asset.Drift(frame), asset.Drift(frame, Vector4.zero), "_VatFrameB = 0");
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ApplyFrame_WritesTheFrameAndItsDrift()
        {
            var asset = DriftAsset();
            var material = new Material(Shader.Find(VatBaker.DefaultShaderName));
            try
            {
                var frame = new Vector4(2f, 3f, 0.5f, 0f);
                asset.ApplyFrame(material, frame);
                Assert.AreEqual(frame, material.GetVector(VatShaderIds.Frame), "_VatFrame");
                AssertDrift(new Vector3(4f, 2f, 3f), material.GetVector(VatShaderIds.Drift), "_VatDrift");
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(asset);
            }
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

        private static VatAsset DriftAsset()
        {
            var asset = ScriptableObject.CreateInstance<VatAsset>();
            var clips = new[] { new VatClip("A", 0, 2, 1f, 2f, true), new VatClip("B", 2, 2, 1f, 2f, true) };
            asset.SetData(new VatLayoutInfo(1, 1, 1, _rows.Length), null, null, null, (Vector3[])_rows.Clone(), clips, default, string.Empty);
            return asset;
        }

        private static void AssertDrift(Vector3 expected, Vector4 actual, string message)
        {
            Assert.Less((expected - (Vector3)actual).magnitude, Tolerance, $"{message}: {actual}");
            Assert.AreEqual(0f, actual.w, $"{message}: w");
        }

        private static void AssertDriftRows(Run run)
        {
            Assert.AreEqual(run.Frames.Length, run.Drift.Length, "one drift row per frame");
            var restCentroid = VatCentroid.Of(run.Frames[0].Positions);
            for (var k = 0; k < run.Frames.Length; k++)
            {
                var expected = VatCentroid.Of(run.Frames[k].Positions) - restCentroid;
                Assert.Less((run.Drift[k] - expected).magnitude, 1e-6f, $"drift of frame {k}");
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
            public readonly Vector3[] Drift;
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
                Drift = encoder.Drift;
                _mesh = encoder.BuildMesh("Body");
                _textures = VatBakeTextures.Build(encoder, "Body");
                _positionTexels = VatTestUtil.ReadGpu(_textures.Position);
            }

            public float MaxDecodeError()
            {
                var rest = _mesh.vertices;
                var bounds = _mesh.bounds;
                bounds.Expand(1e-5f);
                var max = 0f;
                for (var k = 0; k < Frames.Length; k++)
                {
                    for (var v = 0; v < VertexCount; v++)
                    {
                        var decoded = rest[v] + Drift[k] + VatTestUtil.DecodeOffset(_positionTexels, _layout.Info, v, k);
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
