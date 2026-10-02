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
    public class VatAlembicTests
    {
        private const string Fixtures = "Packages/com.vatyakov/Tests/Editor/Fixtures/";
        private const float Fps = 30f;
        private const float Tolerance = 5e-4f;
        private const float AngleTolerance = 0.3f;

        private Scene _scene;
        private VatInMemoryBake _bake;
        private VatBakeProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            _bake?.Destroy();
            _bake = null;
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _profile = null;
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void ClothBake_MatchesAlembicFrames_WithinQuantization([Values(0f, 0.2f)] float start)
        {
            var cloth = Instance("VatCloth");
            cloth.GetComponent<AlembicStreamPlayer>().StartTime = start;
            _bake = new VatInMemoryBake(VatAlembic.Open(cloth), Fps, loop: false);
            var clip = _bake.Layout.Clips[0];
            Assert.AreEqual(Mathf.RoundToInt((1f - start) * Fps) + 1, clip.FrameCount, "round(Duration·fps) + 1");
            CollectionAssert.IsEmpty(_bake.Warnings);

            var positions = VatTestUtil.ReadGpu(_bake.Position);
            var rotations = VatTestUtil.ReadGpu(_bake.Rotation);
            var drift = VatTestUtil.ReadGpu(_bake.Drift);
            var rest = _bake.Mesh.vertices;
            var reference = new AlembicReference(cloth, _scene);
            var maxError = 0f;
            var maxAngle = 0f;
            for (var k = 0; k < clip.FrameCount; k++)
            {
                var mesh = reference.Sample(clip.FrameTime(k));
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var d = VatTestUtil.DecodeDrift(drift, clip.StartRow + k);
                for (var v = 0; v < rest.Length; v++)
                {
                    var decoded = rest[v] + d + VatTestUtil.DecodeOffset(positions, _bake.Layout.Info, v, clip.StartRow + k);
                    var q = VatTestUtil.DecodeRotation(rotations, _bake.Layout.Info, v, clip.StartRow + k);
                    maxError = Mathf.Max(maxError, (decoded - vertices[v]).magnitude);
                    maxAngle = Mathf.Max(maxAngle, Vector3.Angle(normals[v], VatMath.FrameNormal(q)));
                }
            }

            Assert.Less(maxError, Tolerance, $"max error {maxError * 1000f:0.###} mm");
            Assert.Less(maxAngle, AngleTolerance, "normal, degrees");
        }

        [Test]
        public void ShuffledPoints_GiveAWarning()
        {
            _bake = new VatInMemoryBake(VatAlembic.Open(Load("VatShuffled")), Fps, loop: false);
            Assert.IsTrue(_bake.Warnings.Any(w => w.Contains("point order")), string.Join("\n", _bake.Warnings));
            Assert.IsTrue(_bake.Warnings.Any(w => w.Contains(VatBaker.TriplanarShaderName)), "no UV — triplanar hint");
        }

        [Test]
        public void ZeroDuration_FailsTheBake()
        {
            var instance = Instance("VatCloth");
            var player = instance.GetComponent<AlembicStreamPlayer>();
            player.EndTime = player.StartTime;
            Assert.LessOrEqual(player.Duration, 0f);
            _profile = AlembicProfile(instance);

            var error = Assert.Throws<VatBakeException>(() => VatBaker.Bake(_profile, "Assets/__VatTestTemp/Zero.asset"));
            StringAssert.Contains("duration", error.Message);
            Assert.IsNull(_profile.Asset);
        }

        [Test]
        public void ChangingTopology_FailsTheBake_WithPatch2Message()
        {
            var error = Assert.Throws<VatBakeException>(() => _bake = new VatInMemoryBake(VatAlembic.Open(Load("VatTopology")), Fps, loop: false));
            StringAssert.Contains("vertices 81 → 100", error.Message);
            StringAssert.Contains("patch 2", error.Message);
            Assert.IsNotEmpty(VatBaker.Validate(AlembicProfile(Load("VatTopology"))), "the inspector sees it too");
        }

        [Test]
        public void LoopHint_SeesTheClosedCloth()
        {
            var probe = VatAlembicProbe.For(Load("VatCloth"));
            Assert.IsNull(probe.Problem);
            Assert.AreEqual(81, probe.VertexCount);
            Assert.AreEqual(1f, probe.Clip.Length, 1e-5f);
            Assert.Less(probe.LoopGap, VatLoopGap.MaxLoopGap);
        }

        private VatBakeProfile AlembicProfile(GameObject alembic)
        {
            _profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            _profile.Kind = VatSourceKind.Alembic;
            _profile.Alembic = alembic;
            _profile.Fps = Fps;
            _profile.Shader = Shader.Find(VatBaker.DefaultShaderName);
            return _profile;
        }

        private GameObject Instance(string name)
        {
            var instance = Object.Instantiate(Load(name));
            SceneManager.MoveGameObjectToScene(instance, _scene);
            return instance;
        }

        private static GameObject Load(string name)
        {
            var alembic = AssetDatabase.LoadAssetAtPath<GameObject>(Fixtures + name + ".abc");
            Assert.IsNotNull(alembic, name);
            return alembic;
        }

        private sealed class AlembicReference
        {
            private readonly AlembicStreamPlayer _player;
            private readonly MeshFilter _mesh;

            public AlembicReference(GameObject alembic, Scene scene)
            {
                var instance = Object.Instantiate(alembic);
                SceneManager.MoveGameObjectToScene(instance, scene);
                _player = instance.GetComponent<AlembicStreamPlayer>();
                _player.UpdateImmediately(0f);
                _mesh = instance.GetComponentInChildren<MeshFilter>();
            }

            public Mesh Sample(double time)
            {
                _player.UpdateImmediately((float)time);
                return _mesh.sharedMesh;
            }
        }
    }
}
#endif
