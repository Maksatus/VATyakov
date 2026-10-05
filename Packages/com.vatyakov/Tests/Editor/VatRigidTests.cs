using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatRigidTests
    {
        private const float Fps = 30f;
        private const float Tolerance = 1e-3f;
        private const int CubeVertices = 8;
        private const float LateShow = 0.5f;
        private const float LateHide = 0.8f;
        private const int LateFirstFrame = 15;
        private const int LateLastFrame = 23;

        private static readonly Vector3 _stillPoint = new(0.7f, 1.3f, -0.4f);
        private static readonly Vector3 _drift = new(0.5f, 0.2f, 0f);

        private VatBakeResult _result;

        [TearDown]
        public void TearDown()
        {
            if (_result != null)
            {
                Object.DestroyImmediate(_result.Mesh);
                _result.Textures.Destroy();
            }

            _result = null;
        }

        [Test]
        public void NeverVisiblePiece_IsLeftOut_WithAWarning()
        {
            var shown = new VatSyntheticPiece("shown", time => VatSyntheticRigidSource.Trs(Vector3.right * (float)time, Quaternion.identity));
            var never = new VatSyntheticPiece("never", _ => Matrix4x4.identity, _ => false);
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(1f, null, shown, never), Fps, false, "Synthetic");

            Assert.AreEqual(VatMode.Rigid, _result.Mode);
            Assert.AreEqual(CubeVertices, _result.Mesh.vertexCount, "only the shown piece is in the mesh");
            Assert.AreEqual(VatMath.TexelsPerBone, _result.Layout.Info.Elements, "one piece in the texture");
            Assert.IsTrue(_result.Warnings.Any(warning => warning.Contains("hidden on every baked frame") && warning.Contains("'never'")),
                string.Join("\n", _result.Warnings));
        }

        [Test]
        public void PieceHiddenOnFrame0_HoldsItsFirstVisiblePose_WithScaleZero()
        {
            var late = new VatSyntheticPiece("late", LatePose, time => time >= LateShow - 1e-6 && time < LateHide - 1e-6);
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(1f, null, late), Fps, false, "Synthetic");
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var clip = _result.Layout.Clips[0];

            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                var source = frame < LateFirstFrame ? LateFirstFrame : Math.Min(frame, LateLastFrame);
                var isVisible = frame >= LateFirstFrame && frame <= LateLastFrame;
                var offsetScale = decoder.Texel(0, clip.StartRow + frame);
                Assert.AreEqual((Vector3)decoder.Texel(0, clip.StartRow + source), (Vector3)offsetScale, $"frame {frame}: offset of frame {source}");
                Assert.AreEqual(decoder.Texel(1, clip.StartRow + source), decoder.Texel(1, clip.StartRow + frame), $"frame {frame}: rotation of frame {source}");
                Assert.AreEqual(isVisible ? 1f : 0f, offsetScale.w, Tolerance, $"frame {frame}: scale is a step");
                Assert.AreNotEqual(Vector4.zero, decoder.Texel(1, clip.StartRow + frame), "q = 0 is never written");
            }

            AssertMatchesPose(decoder, clip, LatePose, LateFirstFrame, LateLastFrame);
        }

        [Test]
        public void LeastSquaresPivot_IsTheStillPointOfATumblingPiece()
        {
            var tumbling = new VatSyntheticPiece("tumbling", Tumbling);
            Assert.Less(Vector3.Distance(_stillPoint, Pivot(tumbling)), Tolerance, "the point with zero acceleration");

            var falling = new VatSyntheticPiece("falling", time => VatSyntheticRigidSource.Trs(Falling(time), Quaternion.identity));
            Assert.AreEqual(Vector3.zero, Pivot(falling), "no rotation: the center of the bounds");

            var hinge = new VatSyntheticPiece("hinge", time => Hinge(time));
            Assert.AreEqual(Vector3.zero, Pivot(hinge), "rotation about one fixed axis is ill-conditioned: the center of the bounds");
        }

        [Test]
        public void FastSpin_WarnsAboutFps_BelowTheSourceRate()
        {
            var times = VatSyntheticRigidSource.Uniform(0.5f, 120f);
            var spin = new VatSyntheticPiece("spin", time => VatSyntheticRigidSource.Trs(Vector3.right * 3f, Quaternion.AngleAxis(3600f * (float)time, Vector3.up)));
            var slide = new VatSyntheticPiece("slide", time => VatSyntheticRigidSource.Trs(Vector3.forward * (float)time, Quaternion.identity));

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.5f, times, spin, slide), Fps, false, "Synthetic");
            var warning = _result.Warnings.SingleOrDefault(text => text.Contains("too fast"));
            Assert.IsNotNull(warning, string.Join("\n", _result.Warnings));
            StringAssert.Contains("1 piece moves", warning);
            StringAssert.Contains("'spin'", warning);
            TearDown();

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.5f, times, spin, slide), 120f, false, "Synthetic");
            Assert.IsFalse(_result.Warnings.Any(text => text.Contains("too fast")), "every source sample is a baked frame");
        }

        [Test]
        public void FiveHundredPieces_OneHundredFiftyFrames_TakeAbout1_2MB()
        {
            var layout = VatLayout.ForRigid(500, new[] { new VatClipRequest("Destruction", 149f / Fps, Fps, false) });
            Assert.AreEqual(150, layout.Clips[0].FrameCount);
            Assert.AreEqual(1000, layout.Info.Width, "two texels per piece");
            Assert.AreEqual(151, layout.Info.Height, "frames and the pivot row");
            Assert.AreEqual(1208000L, VatMemory.BoneTextureBytes(layout.Info), "16 B per piece and frame");
        }

        [Test]
        public void PieceIndex_Above256_SurvivesTheMesh_AndTheLimitIs2048()
        {
            const int pieceCount = 300;
            var pieces = Enumerable.Range(0, pieceCount)
                .Select(index => new VatSyntheticPiece($"piece_{index}", time => VatSyntheticRigidSource.Trs(new Vector3(index, (float)time, 0f), Quaternion.identity)))
                .ToArray();
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.2f, null, pieces), Fps, false, "Synthetic");
            Assert.AreEqual(1, _result.Mesh.subMeshCount, "pieces share their material slots: one submesh, not one per piece");

            var uvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            for (var vertex = 0; vertex < uvs.Length; vertex++)
            {
                Assert.AreEqual(vertex / CubeVertices * VatMath.TexelsPerBone, VatBoneDecoder.PieceTexel(uvs[vertex]), $"vertex {vertex}");
            }

            var error = Assert.Throws<VatBakeException>(() => VatLayout.ForRigid(VatRigidFormat.MaxPieces + 1, new[] { new VatClipRequest("Clip", 1f, Fps) }));
            StringAssert.Contains("up to 2048", error.Message);
        }

        [Test]
        public void ModeMustFitTheSource()
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            try
            {
                profile.Kind = VatSourceKind.Skinned;
                profile.Mode = VatMode.Rigid;
                Assert.IsTrue(VatBaker.Validate(profile).Any(problem => problem.StartsWith("Mode = Rigid takes an Alembic")));
                Assert.IsFalse(profile.IsRigid);

                profile.Kind = VatSourceKind.Alembic;
                profile.Mode = VatMode.Bone;
                Assert.IsTrue(VatBaker.Validate(profile).Any(problem => problem.StartsWith("Mode = Bone takes a Skinned Mesh Renderer")));

                profile.Mode = VatMode.Rigid;
                Assert.IsTrue(profile.IsRigid);
                Assert.IsFalse(VatBaker.Validate(profile).Any(problem => problem.StartsWith("Mode =")));
            }
            finally
            {
                Object.DestroyImmediate(profile);
            }
        }

        private void AssertMatchesPose(VatBoneDecoder decoder, VatClip clip, Func<double, Matrix4x4> pose, int firstFrame, int lastFrame)
        {
            var rest = _result.Mesh.vertices;
            var uvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            var restInverse = pose(clip.FrameTime(firstFrame)).inverse;
            for (var frame = firstFrame; frame <= lastFrame; frame++)
            {
                var motion = pose(clip.FrameTime(frame)) * restInverse;
                for (var vertex = 0; vertex < rest.Length; vertex++)
                {
                    var decoded = decoder.PiecePosition(uvs[vertex], rest[vertex], VatTestUtil.Row(clip.StartRow + frame));
                    Assert.Less(Vector3.Distance(motion.MultiplyPoint3x4(rest[vertex]), decoded), Tolerance, $"frame {frame}, vertex {vertex}");
                }
            }
        }

        private static Vector3 Pivot(VatSyntheticPiece piece)
        {
            using var source = new VatSyntheticRigidSource(1f, null, piece);
            var clip = VatLayout.ForRigid(1, new[] { new VatClipRequest("Clip", 1f, Fps, false) }).Clips[0];
            var track = source.Extract(clip, VatRigidInnerTimes.Between(clip, null))[0];
            return new VatRigidPiece(track, new VatRigidVisibility(track.Visible)).Pivot;
        }

        private static Matrix4x4 LatePose(double time)
        {
            var t = (float)time;
            return VatSyntheticRigidSource.Trs(new Vector3(t, 2f - t * t, 0f), Quaternion.AngleAxis(90f * t, Vector3.up));
        }

        private static Matrix4x4 Tumbling(double time)
        {
            var t = (float)time;
            var rotation = Quaternion.AngleAxis(200f * t, new Vector3(1f, 2f, 0.5f)) * Quaternion.AngleAxis(130f * t, new Vector3(-0.3f, 0.2f, 1f));
            return Matrix4x4.Translate(_stillPoint + _drift * t) * Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(-_stillPoint);
        }

        private static Vector3 Falling(double time)
        {
            var t = (float)time;
            return new Vector3(t, 3f * t - 4.9f * t * t, 0f);
        }

        private static Matrix4x4 Hinge(double time)
        {
            return Matrix4x4.Translate(_stillPoint) * Matrix4x4.Rotate(Quaternion.AngleAxis(300f * (float)time, Vector3.up)) * Matrix4x4.Translate(-_stillPoint);
        }
    }
}
