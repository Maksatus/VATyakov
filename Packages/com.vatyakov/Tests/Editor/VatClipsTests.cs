using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatClipsTests
    {
        private const string TempFolder = "Assets/__VatClipsTemp";
        private const float Fps = 30f;
        private const float Tolerance = 5e-4f;

        private Scene _scene;
        private VatTestRig _rig;
        private VatBakeProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            if (!AssetDatabase.IsValidFolder(TempFolder))
            {
                AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
            }
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _profile = null;
            _rig?.Destroy();
            _rig = null;
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void Source_ClipsInTwoOrders_SampleTheSameFrames([Values] bool legacy)
        {
            _rig = new VatTestRig(_scene, 300, legacy, blendShape: true);
            var partial = _rig.BuildPartialClip("Partial");
            var forward = SampleAll(_rig.Clip, partial);
            var backward = SampleAll(partial, _rig.Clip);

            for (var k = 0; k < forward[0].Length; k++)
            {
                AssertSameFrame(forward[0][k], backward[1][k], $"{_rig.Clip.name}, frame {k}");
                AssertSameFrame(forward[1][k], backward[0][k], $"Partial, frame {k}");
            }

            var top = _rig.Renderer.sharedMesh.vertexCount - 1;
            Assert.Greater((forward[0][forward[0].Length - 1].Positions[top] - forward[1][0].Positions[top]).magnitude, 0.1f,
                "the end of Clip is far from the start of Partial: a missing reset would show");
        }

        [Test]
        public void Bake_ClipsInTwoOrders_DecodeToTheSamePositions()
        {
            _rig = new VatTestRig(_scene, 5000, legacy: true, blendShape: true);
            var partial = _rig.BuildPartialClip("Partial");
            var forward = new VatInMemoryBake(_rig, Fps, _rig.Clip, partial);
            var backward = new VatInMemoryBake(_rig, Fps, partial, _rig.Clip);
            try
            {
                Assert.AreEqual(2, forward.Layout.Info.Blocks);
                Assert.AreEqual(60, forward.Layout.Info.TotalRows, "two clips of 30 rows, no padding");
                CollectionAssert.AreEqual(new[] { 0, 30 }, forward.Layout.Clips.Select(c => c.StartRow));
                CollectionAssert.AreEqual(new[] { _rig.Clip.name, "Partial" }, forward.Layout.Clips.Select(c => c.Name));
                CollectionAssert.AreEqual(new[] { "Partial", _rig.Clip.name }, backward.Layout.Clips.Select(c => c.Name));

                var error = Mathf.Max(MaxDifference(forward, 0, backward, 1), MaxDifference(forward, 1, backward, 0));
                Assert.Less(error, Tolerance, $"max difference {error * 1000f:0.###} mm");
            }
            finally
            {
                forward.Destroy();
                backward.Destroy();
            }
        }

        [Test]
        public void Bake_ClipsTallerThan4096Rows_FailsWithAClearMessage()
        {
            _rig = new VatTestRig(_scene, 5000, legacy: true);
            _profile = CreateProfile(_rig.Clip, _rig.BuildPartialClip("Partial"));
            var asset = VatBaker.Bake(_profile, TempFolder + "/Tall.asset");

            _profile.Fps = 1100f;
            var error = Assert.Throws<VatBakeException>(() => VatBaker.Bake(_profile));

            StringAssert.Contains("4096", error.Message);
            StringAssert.Contains("2 blocks × 2200 frames of 2 clips = 4400 rows", error.Message);
            StringAssert.Contains("split the clips into several VatAssets", error.Message);
            Assert.AreEqual(error.Message, VatBakeEstimate.For(_profile).Error, "the inspector shows it before a bake");
            Assert.AreEqual(2 * 60, asset.PositionTexture.height, "the old bake is untouched");
        }

        [Test]
        public void Rebake_KeepsTheTemplateClipByName()
        {
            _rig = new VatTestRig(_scene, 300, legacy: true);
            var partial = _rig.BuildPartialClip("Partial");
            _profile = CreateProfile(_rig.Clip, partial);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Template.asset");
            AssertTemplateShows(asset, 0);

            asset.ApplyTo(_profile.Material, 1);
            VatBaker.Bake(_profile);
            AssertTemplateShows(asset, 1);

            asset.ApplyTo(_profile.Material, 0);
            _profile.SetClips(partial, _rig.Clip);
            VatBaker.Bake(_profile);
            Assert.AreEqual(_rig.Clip.name, asset.Clips[1].Name);
            AssertTemplateShows(asset, 1);

            _profile.SetClips(partial);
            VatBaker.Bake(_profile);
            AssertTemplateShows(asset, 0);
        }

        [Test]
        public void Validator_ReportsEmptyMissingRepeatedAndSameNamedClips()
        {
            _rig = new VatTestRig(_scene, 300, legacy: true);
            _profile = CreateProfile();
            StringAssert.Contains("Clips is empty", Problems());

            var twin = _rig.BuildPartialClip(_rig.Clip.name);
            _profile.SetClips(_rig.Clip, null, _rig.Clip, twin);
            var problems = Problems();
            StringAssert.Contains("Clip 2 is not set.", problems);
            StringAssert.Contains($"Clip '{_rig.Clip.name}' is listed twice.", problems);
            StringAssert.Contains($"Two clips are named '{_rig.Clip.name}'", problems);

            _profile.SetClips(_rig.Clip, _rig.BuildPartialClip("Partial"));
            Assert.IsEmpty(VatBaker.Validate(_profile));
        }

        private VatFrame[][] SampleAll(params AnimationClip[] clips)
        {
            using var source = new SkinnedFrameSource(_rig.Renderer, clips);
            var frames = new VatFrame[clips.Length][];
            for (var c = 0; c < clips.Length; c++)
            {
                frames[c] = new VatFrame[Mathf.RoundToInt(VatTestRig.Length * Fps)];
                for (var k = 0; k < frames[c].Length; k++)
                {
                    frames[c][k] = new VatFrame(source.Mesh.VertexCount);
                    source.Sample(c, k / (double)Fps, frames[c][k]);
                }
            }

            return frames;
        }

        private static void AssertSameFrame(VatFrame expected, VatFrame actual, string message)
        {
            CollectionAssert.AreEqual(expected.Positions, actual.Positions, message + ": positions");
            CollectionAssert.AreEqual(expected.Normals, actual.Normals, message + ": normals");
            CollectionAssert.AreEqual(expected.Tangents, actual.Tangents, message + ": tangents");
        }

        private static float MaxDifference(VatInMemoryBake a, int clipA, VatInMemoryBake b, int clipB)
        {
            var positionsA = VatTestUtil.ReadGpu(a.Position);
            var positionsB = VatTestUtil.ReadGpu(b.Position);
            var driftA = VatTestUtil.ReadGpu(a.Drift);
            var driftB = VatTestUtil.ReadGpu(b.Drift);
            var restA = a.Mesh.vertices;
            var restB = b.Mesh.vertices;
            var rowA = a.Layout.Clips[clipA].StartRow;
            var rowB = b.Layout.Clips[clipB].StartRow;
            var max = 0f;
            for (var k = 0; k < a.Layout.Clips[clipA].FrameCount; k++)
            {
                var dA = VatTestUtil.DecodeDrift(driftA, rowA + k);
                var dB = VatTestUtil.DecodeDrift(driftB, rowB + k);
                for (var v = 0; v < restA.Length; v++)
                {
                    var pA = restA[v] + dA + VatTestUtil.DecodeOffset(positionsA, a.Layout.Info, v, rowA + k);
                    var pB = restB[v] + dB + VatTestUtil.DecodeOffset(positionsB, b.Layout.Info, v, rowB + k);
                    max = Mathf.Max(max, (pA - pB).magnitude);
                }
            }

            return max;
        }

        private void AssertTemplateShows(VatAsset asset, int clip)
        {
            Assert.AreEqual(asset.Clips[clip].Frame(0.0), _profile.Material.GetVector(VatShaderIds.Frame));
            var binding = VatMaterialBinding.Read(_profile.Material);
            Assert.AreEqual(clip, binding.ClipIndex, "the material inspector finds the clip by its rows");
            Assert.IsFalse(binding.IsStale);
        }

        private string Problems()
        {
            return string.Join("\n", VatBaker.Validate(_profile));
        }

        private VatBakeProfile CreateProfile(params AnimationClip[] clips)
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            profile.Source = _rig.Renderer;
            profile.SetClips(clips);
            profile.Fps = Fps;
            profile.Shader = Shader.Find(VatBaker.DefaultShaderName);
            return profile;
        }
    }
}
