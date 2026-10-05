#if VAT_ALEMBIC
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using UnityEngine.SceneManagement;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatRigidAlembicTests
    {
        private const string Fixtures = "Packages/com.vatyakov/Tests/Editor/Fixtures/";
        private const string TempFolder = "Assets/__VatRigidTemp";
        private const string Rigid = "vat_rigid";
        private const string Deform = "vat_rigid_deform";
        private const float SourceFps = 120f;
        private const float LowFps = 30f;
        private const float Tolerance = 1e-3f;
        private const int LateSourceFrame = 30;

        private Scene _scene;
        private VatBakeResult _result;
        private VatBakeProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyResult();
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _profile = null;
            EditorSceneManager.ClosePreviewScene(_scene);
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void RigidBake_MatchesAlembicStreamPlayer_OnVisibleFrames()
        {
            _result = Bake(Rigid, SourceFps);
            Assert.AreEqual(VatMode.Rigid, _result.Mode);
            Assert.AreEqual(3 * VatMath.TexelsPerBone, _result.Layout.Info.Elements, "three pieces");
            AssertMatchesPlayer(Rigid);
        }

        [Test]
        public void DeformGeometry_SplitsIntoTheSamePieces_AsTransformGeometry()
        {
            _result = Bake(Deform, SourceFps);
            Assert.AreEqual(VatMode.Rigid, _result.Mode);
            Assert.AreEqual(3 * VatMath.TexelsPerBone, _result.Layout.Info.Elements, "the islands of three cubes merge into three pieces");
            Assert.AreEqual(Load(Rigid).GetComponentsInChildren<MeshFilter>(true).Sum(node => node.sharedMesh.vertexCount), _result.Mesh.vertexCount);
            AssertMatchesPlayer(Deform);
        }

        [Test]
        public void PieceHiddenOnFrame0_IsScaledToZero_UntilItShows()
        {
            _result = Bake(Rigid, LowFps);
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var clip = _result.Layout.Clips[0];
            var late = PieceTexel("late");
            var firstVisible = Mathf.CeilToInt(LateSourceFrame / SourceFps * LowFps);
            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                var offsetScale = decoder.Texel(late, clip.StartRow + frame);
                Assert.AreEqual(frame < firstVisible ? 0f : 1f, offsetScale.w, Tolerance, $"frame {frame}");
                if (frame < firstVisible)
                {
                    Assert.AreEqual((Vector3)decoder.Texel(late, clip.StartRow + firstVisible), (Vector3)offsetScale, $"frame {frame}");
                    Assert.AreEqual(decoder.Texel(late + 1, clip.StartRow + firstVisible), decoder.Texel(late + 1, clip.StartRow + frame), $"frame {frame}");
                }
            }
        }

        [Test]
        public void FastSpin_WarnsAboutFps_FromTheSourceSamples()
        {
            _result = Bake(Rigid, LowFps);
            var warning = _result.Warnings.SingleOrDefault(text => text.Contains("too fast"));
            Assert.IsNotNull(warning, string.Join("\n", _result.Warnings));
            StringAssert.Contains("'spin'", warning);
            StringAssert.Contains("1 piece moves", warning, "the slide and the falling piece stay within 5 mm");
            DestroyResult();

            _result = Bake(Rigid, SourceFps);
            Assert.IsFalse(_result.Warnings.Any(text => text.Contains("too fast")), string.Join("\n", _result.Warnings));
        }

        [Test]
        public void Bake_WritesARigidAsset_OnTheRigidTemplate()
        {
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
            }

            _profile = Profile(Load(Rigid), VatMode.Rigid, SourceFps);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/rigid_vat.asset");

            Assert.AreEqual(VatMode.Rigid, asset.Mode);
            Assert.AreEqual(3, asset.BoneCount, "three pieces");
            Assert.AreEqual(VatBaker.RigidShaderName, _profile.Material.shader.name, "the template plays Rigid assets");
            Assert.AreEqual(asset.BoneTexture, _profile.Material.GetTexture(VatShaderIds.PieceTexture));
            StringAssert.EndsWith(VatAssetPath.PieceSuffix, asset.BoneTexture.name);
            Assert.IsFalse(VatSourceHash.IsOutdated(_profile, asset), "fresh bake");

            var binding = VatMaterialBinding.Read(_profile.Material);
            Assert.AreEqual(VatMaterialStatus.Bound, binding.Status, "the material inspector finds the asset by _VatPieceTex");
            Assert.AreEqual(asset, binding.Asset);
            Assert.IsFalse(binding.IsStale, "the template is fresh");

            _profile.Mode = VatMode.Vertex;
            Assert.IsTrue(VatSourceHash.IsOutdated(_profile, asset), "the mode is in the hash");
        }

        [Test]
        public void DeformingAlembic_FailsTheRigidBake()
        {
            using var source = VatAlembic.OpenRigid(Load("vat_cloth"));
            var error = Assert.Throws<VatBakeException>(() => VatRigidPipeline.Run(source, LowFps, false, "Cloth"));
            StringAssert.Contains("islands of deforming meshes don't move rigidly", error.Message);
            StringAssert.Contains("' island 0: ", error.Message, "the report lists the islands");
            StringAssert.Contains("Mode = Vertex", error.Message);
        }

        private void AssertMatchesPlayer(string fixture)
        {
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var reference = new Reference(Instance(fixture), _scene);
            var rest = _result.Mesh.vertices;
            var uvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            var clip = _result.Layout.Clips[0];
            var maxError = 0f;
            for (var frame = 0; frame < clip.FrameCount; frame++)
            {
                var positions = reference.Sample(clip.FrameTime(frame), out var visible);
                for (var vertex = 0; vertex < rest.Length; vertex++)
                {
                    if (visible[vertex])
                    {
                        var decoded = decoder.PiecePosition(uvs[vertex], rest[vertex], VatTestUtil.Row(clip.StartRow + frame));
                        maxError = Mathf.Max(maxError, Vector3.Distance(positions[vertex], decoded));
                    }
                }
            }

            Assert.Less(maxError, _result.Precision.Error + Tolerance, $"max error {maxError * 1000f:0.###} mm");
            Assert.Less(_result.Precision.Error, Tolerance, "half precision of the piece texture");
        }

        private VatBakeResult Bake(string fixture, float fps)
        {
            using var source = VatAlembic.OpenRigid(Instance(fixture));
            return VatRigidPipeline.Run(source, fps, false, "Rigid");
        }

        private int PieceTexel(string piece)
        {
            using var source = VatAlembic.OpenRigid(Instance(Rigid));
            var clip = VatLayout.ForRigid(source.PieceCount, new[] { new VatClipRequest("Clip", source.Clip.Length, LowFps, false) }).Clips[0];
            var tracks = source.Extract(clip, VatRigidInnerTimes.Between(clip, null));
            return tracks.ToList().FindIndex(track => track.Name == piece) * VatMath.TexelsPerBone;
        }

        private void DestroyResult()
        {
            if (_result != null)
            {
                Object.DestroyImmediate(_result.Mesh);
                _result.Textures.Destroy();
            }

            _result = null;
        }

        private static VatBakeProfile Profile(GameObject alembic, VatMode mode, float fps)
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            profile.Kind = VatSourceKind.Alembic;
            profile.Mode = mode;
            profile.Alembic = alembic;
            profile.Fps = fps;
            profile.IsLooping = false;
            profile.Shader = Shader.Find(VatBaker.DefaultShaderName);
            return profile;
        }

        private GameObject Instance(string fixture)
        {
            var instance = Object.Instantiate(Load(fixture));
            SceneManager.MoveGameObjectToScene(instance, _scene);
            return instance;
        }

        private static GameObject Load(string name)
        {
            var alembic = AssetDatabase.LoadAssetAtPath<GameObject>($"{Fixtures}{name}.abc");
            Assert.IsNotNull(alembic, name);
            return alembic;
        }

        private sealed class Reference
        {
            private readonly AlembicStreamPlayer _player;
            private readonly MeshFilter[] _pieces;

            public Reference(GameObject alembic, Scene scene)
            {
                var instance = Object.Instantiate(alembic);
                SceneManager.MoveGameObjectToScene(instance, scene);
                _player = instance.GetComponent<AlembicStreamPlayer>();
                _player.UpdateImmediately(0f);
                _pieces = instance.GetComponentsInChildren<MeshFilter>(true);
            }

            public Vector3[] Sample(double time, out bool[] visible)
            {
                _player.UpdateImmediately((float)time);
                var positions = _pieces.SelectMany(piece => piece.sharedMesh.vertices.Select(piece.transform.localToWorldMatrix.MultiplyPoint3x4)).ToArray();
                visible = _pieces.SelectMany(piece => Enumerable.Repeat(piece.gameObject.activeInHierarchy, piece.sharedMesh.vertexCount)).ToArray();
                return positions;
            }
        }
    }
}
#endif
