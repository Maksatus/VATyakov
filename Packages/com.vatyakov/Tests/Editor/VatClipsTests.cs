using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    // 1.6: several clips in one asset, the pose reset before each clip, the height limit and the default clip.
    public class VatClipsTests
    {
        const string TempFolder = "Assets/__VatClipsTemp";
        const float Fps = 30f;
        const float Tolerance = 5e-4f; // half quantization of offsets below 1 m, meters

        Scene _scene;
        VatTestRig _rig;
        VatBakeProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
            if (!AssetDatabase.IsValidFolder(TempFolder))
                AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
        }

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TempFolder);
            if (_profile != null)
                Object.DestroyImmediate(_profile);
            _profile = null;
            _rig?.Destroy();
            _rig = null;
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        // Partial keys Bone0 only: without the reset it would start from the last pose of Clip (Bone1 and the blend shape).
        [Test]
        public void Source_ClipsInTwoOrders_SampleTheSameFrames([Values] bool legacy)
        {
            _rig = new VatTestRig(_scene, 300, legacy, blendShape: true);
            var partial = _rig.BuildPartialClip("Partial");
            var forward = SampleAll(_rig.Clip, partial);
            var backward = SampleAll(partial, _rig.Clip);

            for (int k = 0; k < forward[0].Length; k++)
            {
                AssertSameFrame(forward[0][k], backward[1][k], $"{_rig.Clip.name}, frame {k}");
                AssertSameFrame(forward[1][k], backward[0][k], $"Partial, frame {k}");
            }
            int top = _rig.Renderer.sharedMesh.vertexCount - 1;
            Assert.Greater((forward[0][forward[0].Length - 1].Positions[top] - forward[1][0].Positions[top]).magnitude, 0.1f,
                "the end of Clip is far from the start of Partial: a missing reset would show");
        }

        VatFrame[][] SampleAll(params AnimationClip[] clips)
        {
            using var source = new SkinnedFrameSource(_rig.Renderer, clips);
            var frames = new VatFrame[clips.Length][];
            for (int c = 0; c < clips.Length; c++)
            {
                frames[c] = new VatFrame[Mathf.RoundToInt(VatTestRig.Length * Fps)];
                for (int k = 0; k < frames[c].Length; k++)
                {
                    frames[c][k] = new VatFrame(source.Mesh.VertexCount);
                    source.Sample(c, k / (double)Fps, frames[c][k]);
                }
            }
            return frames;
        }

        static void AssertSameFrame(VatFrame expected, VatFrame actual, string message)
        {
            CollectionAssert.AreEqual(expected.Positions, actual.Positions, message + ": positions");
            CollectionAssert.AreEqual(expected.Normals, actual.Normals, message + ": normals");
            CollectionAssert.AreEqual(expected.Tangents, actual.Tangents, message + ": tangents");
        }

        // Rest is frame 0 of the first clip (§2.2), so the bytes differ between the orders; decoded positions must not.
        // 5000 vertices: two blocks, every clip has to land in both.
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

                float error = Mathf.Max(MaxDifference(forward, 0, backward, 1), MaxDifference(forward, 1, backward, 0));
                Assert.Less(error, Tolerance, $"max difference {error * 1000f:0.###} mm");
            }
            finally
            {
                forward.Destroy();
                backward.Destroy();
            }
        }

        static float MaxDifference(VatInMemoryBake a, int clipA, VatInMemoryBake b, int clipB)
        {
            var positionsA = VatTestUtil.ReadGpu(a.Position);
            var positionsB = VatTestUtil.ReadGpu(b.Position);
            var driftA = VatTestUtil.ReadGpu(a.Drift);
            var driftB = VatTestUtil.ReadGpu(b.Drift);
            var restA = a.Mesh.vertices;
            var restB = b.Mesh.vertices;
            int rowA = a.Layout.Clips[clipA].StartRow, rowB = b.Layout.Clips[clipB].StartRow;
            float max = 0f;
            for (int k = 0; k < a.Layout.Clips[clipA].FrameCount; k++)
            {
                var dA = VatTestUtil.DecodeDrift(driftA, rowA + k);
                var dB = VatTestUtil.DecodeDrift(driftB, rowB + k);
                for (int v = 0; v < restA.Length; v++)
                {
                    var pA = restA[v] + dA + VatTestUtil.DecodeOffset(positionsA, a.Layout.Info, v, rowA + k);
                    var pB = restB[v] + dB + VatTestUtil.DecodeOffset(positionsB, b.Layout.Info, v, rowB + k);
                    max = Mathf.Max(max, (pA - pB).magnitude);
                }
            }
            return max;
        }

        // §1.1: 2 blocks × (1100 + 1100) rows. The bake stops at the layout, before sampling, and keeps the old asset.
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

        // §1.6: chosen in the VatAsset inspector, kept by name across rebakes, written into the template material.
        [Test]
        public void DefaultClip_GoesToTheTemplateAndSurvivesRebakes()
        {
            _rig = new VatTestRig(_scene, 300, legacy: true);
            var partial = _rig.BuildPartialClip("Partial");
            _profile = CreateProfile(_rig.Clip, partial);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Default.asset");
            Assert.AreEqual(0, asset.DefaultClipIndex, "the first clip until one is chosen");
            AssertTemplateShows(asset, 0);

            // The inspector finds the template through VatProfileLookup; a saved profile would lose the preview-scene renderer.
            VatDefaultClip.Set(asset, 1, _profile.Material);
            Assert.AreEqual(1, asset.DefaultClipIndex);
            AssertTemplateShows(asset, 1);

            _profile.SetClips(partial, _rig.Clip);
            VatBaker.Bake(_profile);
            Assert.AreEqual(0, asset.DefaultClipIndex, "Partial moved to the top and stays the default");
            Assert.AreEqual("Partial", asset.Clips[asset.DefaultClipIndex].Name);
            AssertTemplateShows(asset, 0);

            _profile.SetClips(_rig.Clip);
            VatBaker.Bake(_profile);
            Assert.AreEqual(0, asset.DefaultClipIndex, "a removed default falls back to the first clip");
            AssertTemplateShows(asset, 0);
        }

        void AssertTemplateShows(VatAsset asset, int clip)
        {
            Assert.AreEqual(asset.Clips[clip].Frame(0.0), _profile.Material.GetVector(VatShaderIds.Frame));
            var binding = VatMaterialBinding.Read(_profile.Material);
            Assert.AreEqual(clip, binding.ClipIndex, "the material inspector finds the clip by its rows");
            Assert.IsFalse(binding.IsStale);
        }

        [Test]
        public void Validator_ReportsEmptyMissingRepeatedAndSameNamedClips()
        {
            _rig = new VatTestRig(_scene, 300, legacy: true);
            _profile = CreateProfile();
            StringAssert.Contains("Clips is empty", Problems());

            var twin = _rig.BuildPartialClip(_rig.Clip.name);
            _profile.SetClips(_rig.Clip, null, _rig.Clip, twin);
            string problems = Problems();
            StringAssert.Contains("Clip 2 is not set.", problems);
            StringAssert.Contains($"Clip '{_rig.Clip.name}' is listed twice.", problems);
            StringAssert.Contains($"Two clips are named '{_rig.Clip.name}'", problems);

            _profile.SetClips(_rig.Clip, _rig.BuildPartialClip("Partial"));
            Assert.IsEmpty(VatBaker.Validate(_profile));
        }

        string Problems() => string.Join("\n", VatBaker.Validate(_profile));

        // Profiles before 1.6 had a single _clip.
        [Test]
        public void OldProfile_MovesItsClipIntoTheList()
        {
            _rig = new VatTestRig(_scene, 300, legacy: true);
            _profile = CreateProfile();
            using (var serialized = new SerializedObject(_profile))
            {
                serialized.FindProperty("_clip").objectReferenceValue = _rig.Clip;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            CollectionAssert.AreEqual(new[] { _rig.Clip }, _profile.Clips);
            using (var serialized = new SerializedObject(_profile))
                Assert.IsNull(serialized.FindProperty("_clip").objectReferenceValue);
        }

        VatBakeProfile CreateProfile(params AnimationClip[] clips)
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
