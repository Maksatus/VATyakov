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
        private const float SourceFps = 120f;
        private const float Tolerance = 1e-3f;
        private const int CubeVertices = VatSyntheticRigidSource.CubeVertices;
        private const float LateShow = 0.5f;
        private const float LateHide = 0.8f;
        private const int LateFirstFrame = 15;
        private const int LateLastFrame = 23;
        private const float Gravity = 9.81f;
        private const float SlowTurnRate = 12f;

        private static readonly Vector3 _stillPoint = new(0.1f, 0.15f, -0.05f);
        private static readonly Vector3 _drift = new(0.5f, 0.2f, 0f);
        private static readonly Vector3 _throw = new(2f, 3f, 0f);
        private static readonly Vector3 _flight = new(70f, 0f, 0f);
        private static readonly Vector3 _mirroredCenter = new(2f, 0f, 0f);

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
                var rotation = decoder.Texel(1, clip.StartRow + frame);
                Assert.AreEqual((Vector3)decoder.Texel(0, clip.StartRow + source), (Vector3)offsetScale, $"frame {frame}: offset of frame {source}");
                Assert.AreEqual(decoder.Texel(1, clip.StartRow + source), rotation, $"frame {frame}: rotation of frame {source}");
                Assert.AreEqual(isVisible ? 1f : 0f, offsetScale.w, Tolerance, $"frame {frame}: scale is a step");
                Assert.AreNotEqual(Vector4.zero, rotation, "q = 0 is never written");
            }

            AssertMatchesPose(decoder, clip, LatePose, LateFirstFrame, LateLastFrame);
        }

        [Test]
        public void HiddenPiece_CollapsesIntoItsCenter_NotIntoThePivot()
        {
            var tumbling = new VatSyntheticPiece("tumbling", Tumbling, time => time < LateHide - 1e-6);
            Assert.Greater(Pivot(tumbling).magnitude, 0.1f, "the pivot is the still point, away from the center");

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(1f, null, tumbling), Fps, false, "Synthetic");
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var clip = _result.Layout.Clips[0];
            var uvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            var rest = _result.Mesh.vertices;
            var lastVisible = Mathf.CeilToInt(LateHide * Fps) - 1;
            var center = Tumbling(clip.FrameTime(lastVisible)).MultiplyPoint3x4(Vector3.zero);
            for (var vertex = 0; vertex < rest.Length; vertex++)
            {
                var collapsed = decoder.PiecePosition(uvs[vertex], rest[vertex], VatTestUtil.Row(clip.StartRow + lastVisible + 1));
                Assert.Less(Vector3.Distance(center, collapsed), Tolerance, $"vertex {vertex}");
            }
        }

        [Test]
        public void LeastSquaresPivot_IsTheStillPointOfATumblingPiece()
        {
            var tumbling = new VatSyntheticPiece("tumbling", Tumbling);
            Assert.Less(Vector3.Distance(_stillPoint, Pivot(tumbling)), Tolerance, "the point with zero acceleration");

            var falling = new VatSyntheticPiece("falling", time => VatSyntheticRigidSource.Trs(Falling(time), Quaternion.identity));
            Assert.AreEqual(Vector3.zero, Pivot(falling), "no rotation: the center of the bounds");

            var hinge = new VatSyntheticPiece("hinge", Hinge);
            Assert.AreEqual(Vector3.zero, Pivot(hinge), "rotation about one fixed axis is ill-conditioned: the center of the bounds");
        }

        [Test]
        public void FallingPiece_ThatTumblesSlowly_KeepsThePivotInsideIt()
        {
            var debris = new VatSyntheticPiece("debris", SlowTumble);
            Assert.LessOrEqual(Pivot(debris).magnitude, VatSyntheticRigidSource.HalfSize * Mathf.Sqrt(3f), "gravity pulls the still point far away");

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(1f, null, debris), Fps, false, "Synthetic");
            Assert.Less(_result.Precision.Error, VatRigidFormat.Tolerance, "half rotations don't get a lever of meters");
            Assert.Less(_result.Mesh.bounds.size.magnitude, 10f, "the bounds follow the flight, not a far pivot");
        }

        [Test]
        public void FastSpin_WarnsAboutFps_BelowTheSourceRate()
        {
            var times = VatSyntheticRigidSource.Uniform(0.5f, SourceFps);
            var spin = new VatSyntheticPiece("spin", Spin);
            var slide = new VatSyntheticPiece("slide", time => VatSyntheticRigidSource.Trs(Vector3.forward * (float)time, Quaternion.identity));

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.5f, times, spin, slide), Fps, false, "Synthetic");
            var warning = _result.Warnings.SingleOrDefault(text => text.Contains("too fast"));
            Assert.IsNotNull(warning, string.Join("\n", _result.Warnings));
            StringAssert.Contains("1 piece moves", warning);
            StringAssert.Contains("'spin'", warning);
            TearDown();

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.5f, times, spin, slide), SourceFps, false, "Synthetic");
            Assert.IsFalse(_result.Warnings.Any(text => text.Contains("too fast")), "every source sample is a baked frame");
        }

        [Test]
        public void FarFlight_WithSteadyMotion_GetsNoFpsWarning_FromHalfPrecision()
        {
            var times = VatSyntheticRigidSource.Uniform(0.5f, SourceFps);
            var flight = new VatSyntheticPiece("flight", time => VatSyntheticRigidSource.Trs(_flight * (float)time, Quaternion.identity));

            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.5f, times, flight), Fps, false, "Synthetic");
            Assert.Greater(_result.Precision.Error, VatRigidFormat.Tolerance, "half offsets 35 m away lose millimeters");
            Assert.IsFalse(_result.Warnings.Any(text => text.Contains("too fast")), string.Join("\n", _result.Warnings));
        }

        [Test]
        public void MirroredPiece_KeepsItsFrontFacesOutside()
        {
            var plain = new VatSyntheticPiece("plain", _ => Matrix4x4.identity);
            var mirrored = new VatSyntheticPiece("mirrored", _ => Matrix4x4.TRS(_mirroredCenter, Quaternion.identity, new Vector3(-1f, 1f, 1f)));
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.2f, null, plain, mirrored), Fps, false, "Synthetic");

            var positions = _result.Mesh.vertices;
            var triangles = _result.Mesh.triangles;
            var expected = Math.Sign(Outward(positions, triangles, 0));
            for (var start = 0; start < triangles.Length; start += 3)
            {
                Assert.AreEqual(expected, Math.Sign(Outward(positions, triangles, start)), $"triangle {start / 3}");
            }
        }

        [Test]
        public void ZeroScaleOnAVisibleFrame_HidesThePiece()
        {
            var shrinking = new VatSyntheticPiece("shrinking", time => Matrix4x4.Scale(Vector3.one * (time < LateHide - 1e-6 ? 1f : 0f)));
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(1f, null, shrinking), Fps, false, "Synthetic");
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var clip = _result.Layout.Clips[0];

            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                var isShown = clip.FrameTime(frame) < LateHide - 1e-6;
                Assert.AreEqual(isShown ? 1f : 0f, decoder.Texel(0, clip.StartRow + frame).w, Tolerance, $"frame {frame}");
            }
        }

        [Test]
        public void NonUniformScale_FailsTheBake()
        {
            var squashed = new VatSyntheticPiece("squashed", time => Matrix4x4.Scale(new Vector3(1f, 1f + (float)time, 1f)));
            var source = new VatSyntheticRigidSource(1f, null, squashed);

            var error = Assert.Throws<VatBakeException>(() => VatRigidPipeline.Run(source, Fps, false, "Synthetic"));
            StringAssert.Contains("piece 'squashed' scales non-uniformly", error.Message);
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
            var pieces = Enumerable.Range(0, pieceCount).Select(index => new VatSyntheticPiece($"piece_{index}", time => Row(index, time))).ToArray();
            _result = VatRigidPipeline.Run(new VatSyntheticRigidSource(0.2f, null, pieces), Fps, false, "Synthetic");
            Assert.AreEqual(1, _result.Mesh.subMeshCount, "pieces share their material slots: one submesh, not one per piece");

            var uvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            for (var vertex = 0; vertex < uvs.Length; vertex++)
            {
                Assert.AreEqual(vertex / CubeVertices * VatMath.TexelsPerBone, VatBoneDecoder.PieceTexel(uvs[vertex]), $"vertex {vertex}");
            }

            var largest = VatLayout.ForRigid(VatRigidFormat.MaxPieces, new[] { new VatClipRequest("Clip", 1f, Fps) });
            Assert.AreEqual(VatMath.MaxTextureSize, largest.Info.Width, "2048 pieces fill the width");
            var error = Assert.Throws<VatBakeException>(() => VatLayout.ForRigid(VatRigidFormat.MaxPieces + 1, new[] { new VatClipRequest("Clip", 1f, Fps) }));
            StringAssert.Contains("up to 2048", error.Message);
        }

        [Test]
        public void Validate_ModeThatDoesNotFitTheSource_IsAProblem()
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            try
            {
                profile.Kind = VatSourceKind.Skinned;
                profile.Mode = VatMode.Rigid;
                Assert.IsTrue(VatBaker.Validate(profile).Any(problem => problem.StartsWith("Mode = Rigid takes an Alembic with rigid pieces")));
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

        private static float Outward(Vector3[] positions, int[] triangles, int start)
        {
            var a = positions[triangles[start]];
            var b = positions[triangles[start + 1]];
            var c = positions[triangles[start + 2]];
            var center = triangles[start] < CubeVertices ? Vector3.zero : _mirroredCenter;
            return Vector3.Dot(Vector3.Cross(b - a, c - a), (a + b + c) / 3f - center);
        }

        private static Vector3 Pivot(VatSyntheticPiece piece)
        {
            using var source = new VatSyntheticRigidSource(1f, null, piece);
            var clip = VatLayout.ForRigid(1, new[] { new VatClipRequest("Clip", 1f, Fps, false) }).Clips[0];
            var track = source.Extract(clip, VatRigidInnerTimes.Between(clip, null))[0];
            return new VatRigidPiece(track, VatRigidVisibility.Of(track)).Pivot;
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

        private static Matrix4x4 SlowTumble(double time)
        {
            var t = (float)time;
            var rotation = Quaternion.AngleAxis(SlowTurnRate * t, new Vector3(1f, 2f, 0.5f)) * Quaternion.AngleAxis(0.65f * SlowTurnRate * t, Vector3.forward);
            return VatSyntheticRigidSource.Trs(_throw * t + 0.5f * Gravity * t * t * Vector3.down, rotation);
        }

        private static Vector3 Falling(double time)
        {
            var t = (float)time;
            return new Vector3(t, 3f * t - 4.9f * t * t, 0f);
        }

        private static Matrix4x4 Hinge(double time)
        {
            var rotation = Quaternion.AngleAxis(300f * (float)time, Vector3.up);
            return Matrix4x4.Translate(_stillPoint) * Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(-_stillPoint);
        }

        private static Matrix4x4 Spin(double time)
        {
            return VatSyntheticRigidSource.Trs(Vector3.right * 3f, Quaternion.AngleAxis(3600f * (float)time, Vector3.up));
        }

        private static Matrix4x4 Row(int index, double time)
        {
            return VatSyntheticRigidSource.Trs(new Vector3(index, (float)time, 0f), Quaternion.identity);
        }
    }
}
