using System.Linq;
using Kefir.Vat.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kefir.Vat.Tests
{
    public class VatBakeTests
    {
        const string TempFolder = "Assets/__VatTestTemp";
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
            _rig?.Destroy();
            _rig = null; // NUnit reuses the fixture instance between tests
            if (_profile != null)
                Object.DestroyImmediate(_profile);
            _profile = null;
            EditorSceneManager.ClosePreviewScene(_scene);
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void MeshLayout_MatchesVatLayout_WhileReadable()
        {
            _rig = new VatTestRig(_scene, 5000, legacy: false);
            using var source = new SkinnedFrameSource(_rig.Renderer, new[] { _rig.Clip });
            var layout = VatLayout.ForVertex(source.Mesh.VertexCount, new[] { new VatLayout.ClipRequest(_rig.Clip.name, _rig.Clip.length, Fps) });
            var encoder = new VertexEncoder(layout, source.Mesh);
            var frame = new VatFrame(source.Mesh.VertexCount);
            Vector3[] rest = null, restNormals = null;
            var clip = layout.Clips[0];
            for (int k = 0; k < clip.FrameCount; k++)
            {
                source.Sample(0, VatMath.LoopFrameTime(k, clip.FrameCount, clip.Length), frame);
                rest ??= (Vector3[])frame.Positions.Clone();
                restNormals ??= (Vector3[])frame.Normals.Clone();
                encoder.AddFrame(0, k, frame);
            }

            var mesh = encoder.BuildMesh("MeshLayoutTest");
            var texture = encoder.BuildPositionTexture("MeshLayoutTest");
            try
            {
                Assert.AreEqual(2, layout.Info.Blocks, "5000 vertices need two blocks");
                CollectionAssert.AreEqual(VatLayout.VertexModeAttributes, mesh.GetVertexAttributes());
                Assert.AreEqual(VatLayout.VertexModeStrides[0], mesh.GetVertexBufferStride(0));
                Assert.AreEqual(VatLayout.VertexModeStrides[1], mesh.GetVertexBufferStride(1));
                Assert.DoesNotThrow(() => layout.Verify(mesh, texture));

                Assert.IsTrue(mesh.isReadable);
                CollectionAssert.AreEqual(rest, mesh.vertices, "positions are Float32 rest positions");
                var sourceUv = _rig.Renderer.sharedMesh.uv;
                var uv = mesh.uv;
                var normals = mesh.normals;
                for (int v = 0; v < mesh.vertexCount; v++)
                {
                    Assert.That((uv[v] - sourceUv[v]).magnitude, Is.LessThan(1e-3f), "uv, Float16");
                    Assert.That((normals[v] - restNormals[v]).magnitude, Is.LessThan(1e-3f), "normal, Float16");
                    Assert.That((restNormals[v] - RestNormal).magnitude, Is.LessThan(1e-5f), "normal in root space");
                }

                var sourceMesh = _rig.Renderer.sharedMesh;
                Assert.AreEqual(sourceMesh.subMeshCount, mesh.subMeshCount);
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    Assert.AreEqual(0, mesh.GetSubMesh(s).baseVertex, "baseVertex = 0 on every sub-mesh");
                    CollectionAssert.AreEqual(sourceMesh.GetIndices(s, true), mesh.GetIndices(s, true));
                }
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(texture);
            }

        }

        // Root-space normal of the strip: Model rotates the mesh −90° around X.
        static Vector3 RestNormal => Quaternion.Euler(-90f, 0f, 0f) * Vector3.back;

        [Test]
        public void Bake_Reload_TextureBytesAreIdentical_AndDecodeToReference([Values] bool legacy)
        {
            _rig = new VatTestRig(_scene, 5000, legacy);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Reload.asset");
            string path = AssetDatabase.GetAssetPath(asset);
            byte[] before = VatTestUtil.ReadGpu(asset.PositionTexture);

            Resources.UnloadAsset(asset.PositionTexture);
            Resources.UnloadAsset(asset.Mesh);
            var reloaded = AssetDatabase.LoadAssetAtPath<VatAsset>(path);
            byte[] after = VatTestUtil.ReadGpu(reloaded.PositionTexture);

            CollectionAssert.AreEqual(before, after, "texture bytes after reload");
            Assert.IsFalse(reloaded.PositionTexture.isReadable);
            Assert.IsFalse(reloaded.Mesh.isReadable);
            Assert.AreEqual(VatAsset.CurrentFormatVersion, reloaded.FormatVersion);

            var layout = reloaded.Layout;
            var clip = reloaded.Clips[0];
            var rest = reloaded.Mesh.vertices;
            float maxError = 0f;
            for (int k = 0; k < clip.FrameCount; k++)
            {
                var reference = _rig.ReferencePositions(VatMath.LoopFrameTime(k, clip.FrameCount, clip.Length));
                for (int v = 0; v < rest.Length; v++)
                {
                    var decoded = rest[v] + VatTestUtil.DecodeOffset(after, layout, v, clip.StartRow + k);
                    maxError = Mathf.Max(maxError, (decoded - reference[v]).magnitude);
                    Assert.IsTrue(reloaded.Mesh.bounds.Contains(decoded) || Near(reloaded.Mesh.bounds, decoded), $"vertex {v} of frame {k} outside bounds");
                }
            }

            Assert.Less(maxError, Tolerance, $"max error {maxError * 1000f:0.###} mm");
        }

        [Test]
        public void Rebake_WithOtherFps_KeepsFileIdsAndReferences()
        {
            _rig = new VatTestRig(_scene, 300, legacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Rebake.asset");
            Assert.IsNull(_profile.Prefab, "a bake never creates a prefab");
            VatBaker.CreatePrefab(_profile);
            var ids = new[] { Id(asset), Id(asset.Mesh), Id(asset.PositionTexture) };
            int heightBefore = asset.PositionTexture.height;
            string materialPath = AssetDatabase.GetAssetPath(_profile.Material);
            string prefabPath = AssetDatabase.GetAssetPath(_profile.Prefab);

            _profile.Fps = 10f;
            var rebaked = VatBaker.Bake(_profile);

            Assert.AreSame(asset, rebaked);
            CollectionAssert.AreEqual(ids, new[] { Id(rebaked), Id(rebaked.Mesh), Id(rebaked.PositionTexture) });
            Assert.AreEqual(30, heightBefore);
            Assert.AreEqual(10, rebaked.PositionTexture.height, "10 frames after the rebake");

            // References survive a reload from disk.
            Resources.UnloadAsset(rebaked.PositionTexture);
            Resources.UnloadAsset(rebaked.Mesh);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var reloaded = AssetDatabase.LoadAssetAtPath<VatAsset>(AssetDatabase.GetAssetPath(rebaked));
            Assert.AreEqual(reloaded.PositionTexture, material.GetTexture(VatShaderIds.PosTex));
            Assert.AreEqual(reloaded.Mesh, prefab.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(10, material.GetTexture(VatShaderIds.PosTex).height);
            Assert.AreEqual(reloaded.Layout.ShaderLayout, material.GetVector(VatShaderIds.Layout));
            Assert.AreEqual(reloaded.Clips[0].State(0f), material.GetVector(VatShaderIds.ClipA));
            Assert.IsFalse(material.enableInstancing);
        }

        [Test]
        public void Layout_TallerThan4096Rows_FailsWithAClearMessage()
        {
            var clips = new[] { new VatLayout.ClipRequest("Long", 100f, 30f) }; // 3000 frames × 2 blocks
            var error = Assert.Throws<VatBakeException>(() => VatLayout.ForVertex(5000, clips));
            StringAssert.Contains("4096", error.Message);
            StringAssert.Contains("fps", error.Message);
        }

        VatBakeProfile CreateProfile(VatTestRig rig)
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            profile.Source = rig.Renderer;
            profile.Clip = rig.Clip;
            profile.Fps = Fps;
            profile.Shader = Shader.Find(VatBaker.DefaultShaderName);
            return profile;
        }

        static long Id(Object target)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string _, out long id), $"{target} is not an asset");
            return id;
        }

        static bool Near(Bounds bounds, Vector3 point) => bounds.SqrDistance(point) < 1e-10f;
    }
}
