using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatHlslProbeTests
    {
        private const float Fps = 30f;
        private const int TwoBlockVertexCount = 5000;
        private const int HalfWeightVertexCount = 66;
        private const double PositionA = 2.3;
        private const double PositionB = 5.7;
        private const float Weight = 0.4f;
        private const float Tolerance = 1e-4f;
        private const float RigidLength = 1f;

        private static readonly Vector3 _stillPoint = new(0.1f, 0.15f, -0.05f);
        private static readonly Vector3 _throw = new(2f, 3f, 0f);

        private Scene _scene;
        private VatTestRig _rig;
        private VatBakeProfile _profile;
        private VatBakeResult _result;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_result != null)
            {
                Object.DestroyImmediate(_result.Mesh);
                _result.Textures.Destroy();
            }

            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _rig?.Destroy();
            _result = null;
            _profile = null;
            _rig = null;
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void VertexShader_MatchesTheCpuDecode_BetweenFrames([Values] bool isTransition)
        {
            _rig = new VatTestRig(_scene, TwoBlockVertexCount, isLegacy: true, hasTwist: true);
            var bake = new VatInMemoryBake(_rig, Fps);
            try
            {
                var clip = bake.Layout.Clips[0];
                var frame = clip.Frame(PositionA);
                var frameB = clip.Frame(PositionB);
                var weight = isTransition ? Weight : 0f;
                frameB.w = weight;
                var drift = Vector4.LerpUnclamped(Drift(bake, frame), Drift(bake, frameB), weight);
                var rest = bake.Mesh.vertices;
                using var probe = new VatHlslProbe(rest.Length);
                probe.SetTexture("_PosTex", bake.Position);
                probe.SetTexture("_RotTex", bake.Rotation);
                probe.SetVector("_Layout", bake.Layout.Info.ShaderLayout);
                probe.SetVector("_Frame", frame);
                probe.SetVector("_FrameB", frameB);
                probe.SetVector("_Drift", drift);
                probe.SetVector("_PosScale", bake.PositionRange.ShaderScale);
                probe.SetInput("_RestPosition", rest);

                probe.Dispatch(isTransition ? "VertexBlendProbe" : "VertexProbe");

                var positions = VatTestUtil.ReadGpu(bake.Position);
                var rotations = VatTestUtil.ReadGpu(bake.Rotation);
                for (var v = 0; v < rest.Length; v++)
                {
                    var sample = Vector3.LerpUnclamped(Sample(bake, positions, v, frame), Sample(bake, positions, v, frameB), weight);
                    var qA = Rotation(bake, rotations, v, frame);
                    var qB = Rotation(bake, rotations, v, frameB);
                    var normal = Vector3.LerpUnclamped(VatMath.FrameNormal(qA), VatMath.FrameNormal(qB), weight);
                    var tangent = Vector3.LerpUnclamped(VatMath.FrameTangent(qA), VatMath.FrameTangent(qB), weight);
                    AssertClose(VatTestUtil.ShaderPosition(rest[v], drift, sample, bake.PositionRange), probe.Positions[v], $"position of vertex {v}");
                    AssertClose(normal, probe.Normals[v], $"normal of vertex {v}");
                    AssertClose(tangent, probe.Tangents[v], $"tangent of vertex {v}");
                }
            }
            finally
            {
                bake.Destroy();
            }
        }

        [Test]
        public void BoneShader_MatchesTheCpuDecode_BetweenFramesInATransition()
        {
            _rig = new VatTestRig(_scene, HalfWeightVertexCount, isLegacy: true, hasTwist: true);
            _profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            _profile.Source = _rig.Renderer;
            _profile.SetClips(_rig.Clip);
            _profile.Fps = Fps;
            _profile.Mode = VatMode.Bone;
            _result = VatBakePipeline.Run(_profile, "Probe");
            Assert.AreEqual(VatMode.Bone, _result.Mode, _result.Fallback);
            var clip = _result.Layout.Clips[0];
            var frame = clip.Frame(PositionA);
            var frameB = clip.Frame(PositionB);
            frameB.w = Weight;
            var mesh = _result.Mesh;
            var uvs = VatBoneDecoder.BoneUvs(mesh);
            using var probe = Probe(mesh, uvs, _result);
            probe.SetVector("_Frame", frame);
            probe.SetVector("_FrameB", frameB);

            probe.Dispatch("BoneBlendProbe");

            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var rest = mesh.vertices;
            for (var v = 0; v < rest.Length; v++)
            {
                var expected = Vector3.LerpUnclamped(decoder.Position(uvs[v], rest[v], frame), decoder.Position(uvs[v], rest[v], frameB), Weight);
                AssertClose(expected, probe.Positions[v], $"position of vertex {v}");
            }
        }

        [Test]
        public void RigidShader_MatchesTheCpuDecode_BetweenFrames()
        {
            var still = new VatSyntheticPiece("still", _ => Matrix4x4.identity);
            var thrown = new VatSyntheticPiece("thrown", Thrown);
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(RigidLength, null, still, thrown), Fps, "Probe");
            var frame = _result.Layout.Clips[0].Frame(PositionA);
            var mesh = _result.Mesh;
            var uvs = VatBoneDecoder.BoneUvs(mesh);
            using var probe = Probe(mesh, uvs, _result);
            probe.SetVector("_Frame", frame);

            probe.Dispatch("RigidProbe");

            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var rest = mesh.vertices;
            for (var v = 0; v < rest.Length; v++)
            {
                AssertClose(decoder.PiecePosition(uvs[v], rest[v], frame), probe.Positions[v], $"position of vertex {v}");
            }
        }

        private static VatHlslProbe Probe(Mesh mesh, Vector4[] uvs, VatBakeResult result)
        {
            var probe = new VatHlslProbe(mesh.vertexCount);
            probe.SetTexture("_PosTex", result.Textures.Bone);
            probe.SetVector("_Layout", result.Layout.Info.ShaderLayout);
            probe.SetInput("_Uv", uvs);
            probe.SetInput("_RestPosition", mesh.vertices);
            probe.SetInput("_RestNormal", mesh.normals);
            probe.SetInput("_RestTangent", Array.ConvertAll(mesh.tangents, tangent => (Vector3)tangent));
            return probe;
        }

        private static Matrix4x4 Thrown(double time)
        {
            var t = (float)time;
            var rotation = Quaternion.AngleAxis(200f * t, new Vector3(1f, 2f, 0.5f));
            return Matrix4x4.Translate(_stillPoint + _throw * t) * Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(-_stillPoint);
        }

        private static Vector4 Drift(VatInMemoryBake bake, Vector4 frame)
        {
            return Vector3.LerpUnclamped(bake.Drift[(int)frame.x], bake.Drift[(int)frame.y], frame.z) + bake.PositionRange.Min;
        }

        private static Vector3 Sample(VatInMemoryBake bake, byte[] texels, int vertex, Vector4 frame)
        {
            var sample0 = VatTestUtil.PositionSample(texels, bake.Layout.Info, bake.PositionFormat, vertex, (int)frame.x);
            var sample1 = VatTestUtil.PositionSample(texels, bake.Layout.Info, bake.PositionFormat, vertex, (int)frame.y);
            return Vector3.LerpUnclamped(sample0, sample1, frame.z);
        }

        private static Vector4 Rotation(VatInMemoryBake bake, byte[] texels, int vertex, Vector4 frame)
        {
            var q0 = VatTestUtil.DecodeRotation(texels, bake.Layout.Info, vertex, (int)frame.x);
            var q1 = VatTestUtil.DecodeRotation(texels, bake.Layout.Info, vertex, (int)frame.y);
            return Vector4.LerpUnclamped(q0, q1, frame.z);
        }

        private static void AssertClose(Vector3 expected, Vector3 actual, string message)
        {
            Assert.Less(Vector3.Distance(expected, actual), Tolerance, $"{message}: {expected} on the CPU, {actual} on the GPU");
        }
    }
}
