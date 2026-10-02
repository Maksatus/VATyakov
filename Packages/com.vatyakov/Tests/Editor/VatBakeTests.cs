using System;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatBakeTests
    {
        private const string TempFolder = "Assets/__VatTestTemp";
        private const float Fps = 30f;
        private const float Tolerance = 5e-4f;
        private const float AngleTolerance = 0.3f;

        private Scene _scene;
        private VatTestRig _rig;
        private VatBakeProfile _profile;

        private static Vector3 RestNormal => Quaternion.Euler(-90f, 0f, 0f) * Vector3.back;

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
            _rig?.Destroy();
            _rig = null;
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _profile = null;
            EditorSceneManager.ClosePreviewScene(_scene);
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void MeshLayout_MatchesVatLayout_WhileReadable()
        {
            _rig = new VatTestRig(_scene, 5000, legacy: false);
            var bake = new VatInMemoryBake(_rig, Fps);
            try
            {
                Assert.AreEqual(2, bake.Layout.Info.Blocks, "5000 vertices need two blocks");
                AssertFormat(bake);
                AssertVertices(bake, _rig.Renderer.sharedMesh.uv);
                AssertSubMeshes(bake.Mesh, _rig.Renderer.sharedMesh);
            }
            finally
            {
                bake.Destroy();
            }
        }

        [Test]
        public void Bake_Reload_TextureBytesAreIdentical_AndDecodeToReference([Values] bool legacy)
        {
            _rig = new VatTestRig(_scene, 5000, legacy);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Reload.asset");
            var before = VatTestUtil.ReadGpu(asset.PositionTexture);
            var rotationBefore = VatTestUtil.ReadGpu(asset.RotationTexture);

            var reloaded = Reload(asset);
            var after = VatTestUtil.ReadGpu(reloaded.PositionTexture);

            CollectionAssert.AreEqual(before, after, "texture bytes after reload");
            CollectionAssert.AreEqual(rotationBefore, VatTestUtil.ReadGpu(reloaded.RotationTexture), "rotation bytes after reload");
            Assert.IsFalse(reloaded.PositionTexture.isReadable);
            Assert.IsFalse(reloaded.RotationTexture.isReadable);
            Assert.IsFalse(reloaded.Mesh.isReadable);
            Assert.IsFalse(reloaded.DriftTexture.isReadable);
            Assert.AreEqual(reloaded.DriftTexture, _profile.Material.GetTexture(VatShaderIds.DriftTex), "template gets _VatDriftTex");
            Assert.AreEqual(VatAsset.CurrentFormatVersion, reloaded.FormatVersion);
            AssertDecodesToReference(reloaded, after);
        }

        [Test]
        public void TwistedStrip_RotationTextureDecodesToSkinnedNormalsAndTangents()
        {
            _rig = new VatTestRig(_scene, 300, legacy: true, twist: true);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Twist.asset");
            var texels = VatTestUtil.ReadGpu(asset.RotationTexture);

            var clip = asset.Clips[0];
            var maxNormal = 0f;
            var maxTangent = 0f;
            for (var k = 0; k < clip.FrameCount; k++)
            {
                var normals = _rig.ReferenceNormals(clip.FrameTime(k));
                var tangents = _rig.ReferenceTangents(clip.FrameTime(k));
                for (var v = 0; v < normals.Length; v++)
                {
                    var q = VatTestUtil.DecodeRotation(texels, asset.Layout, v, clip.StartRow + k);
                    var tangent = (tangents[v] - normals[v] * Vector3.Dot(normals[v], tangents[v])).normalized;
                    maxNormal = Mathf.Max(maxNormal, Vector3.Angle(normals[v], VatMath.FrameNormal(q)));
                    maxTangent = Mathf.Max(maxTangent, Vector3.Angle(tangent, VatMath.FrameTangent(q)));
                }
            }

            Assert.Less(maxNormal, AngleTolerance, "normal, degrees");
            Assert.Less(maxTangent, AngleTolerance, "tangent, degrees");
            Assert.Greater(Vector3.Angle(_rig.ReferenceNormals(0.5)[_rig.Renderer.sharedMesh.vertexCount - 1], RestNormal), 30f,
                "the twist turns the top of the strip");
            Assert.IsTrue(Array.TrueForAll(asset.Mesh.tangents, t => t.w == 1f), "bitangent sign from the rest tangent");
        }

        [Test]
        public void OneShotBake_EndsOnTheClipEnd([Values] bool legacy, [Values] bool loopingClip)
        {
            _rig = new VatTestRig(_scene, 300, legacy, loopingClip);
            _profile = CreateProfile(_rig);
            _profile.Loop = false;
            var asset = VatBaker.Bake(_profile, TempFolder + "/OneShot.asset");

            var clip = asset.Clips[0];
            Assert.IsFalse(clip.Loop);
            Assert.AreEqual(Mathf.RoundToInt(VatTestRig.Length * Fps) + 1, clip.FrameCount);
            Assert.AreEqual(Fps, clip.FrameRate, 1e-4f, "fps_eff = (F − 1) / L");
            Assert.AreEqual(VatTestRig.Length, clip.FrameTime(clip.FrameCount - 1), 1e-9);
            Assert.AreEqual(clip.Frame(0.0), _profile.Material.GetVector(VatShaderIds.Frame), "template shows frame 0");
            AssertDecodesToReference(asset, VatTestUtil.ReadGpu(asset.PositionTexture));
        }

        [Test]
        public void Rebake_WithOtherFps_KeepsFileIdsAndReferences()
        {
            _rig = new VatTestRig(_scene, 300, legacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, TempFolder + "/Rebake.asset");
            Assert.IsNull(_profile.Prefab, "a bake never creates a prefab");
            VatBaker.CreatePrefab(_profile);
            var ids = Ids(asset);
            var materialPath = AssetDatabase.GetAssetPath(_profile.Material);
            var prefabPath = AssetDatabase.GetAssetPath(_profile.Prefab);
            Assert.AreEqual(30, asset.PositionTexture.height);

            _profile.Fps = 10f;
            var rebaked = VatBaker.Bake(_profile);

            Assert.AreSame(asset, rebaked);
            CollectionAssert.AreEqual(ids, Ids(rebaked));
            Assert.AreEqual(10, rebaked.PositionTexture.height, "10 frames after the rebake");
            AssertReferencesSurviveReload(rebaked, materialPath, prefabPath);
        }

        [Test]
        public void Layout_TallerThan4096Rows_FailsWithAClearMessage()
        {
            var clips = new[] { new VatClipRequest("Long", 100f, 30f) };
            var error = Assert.Throws<VatBakeException>(() => VatLayout.ForVertex(5000, clips));
            StringAssert.Contains("4096", error.Message);
            StringAssert.Contains("fps", error.Message);
        }

        private static void AssertFormat(VatInMemoryBake bake)
        {
            CollectionAssert.AreEqual(VatVertexFormat.Attributes, bake.Mesh.GetVertexAttributes());
            Assert.AreEqual(VatVertexFormat.Strides[0], bake.Mesh.GetVertexBufferStride(0));
            Assert.AreEqual(VatVertexFormat.Strides[1], bake.Mesh.GetVertexBufferStride(1));
            Assert.AreEqual(VatVertexFormat.Position, bake.Position.graphicsFormat);
            Assert.AreEqual(bake.Layout.Info.Width, bake.Position.width);
            Assert.AreEqual(bake.Layout.Info.Height, bake.Position.height);
            Assert.AreEqual(1, bake.Position.mipmapCount);
            Assert.AreEqual(VatVertexFormat.Rotation, bake.Rotation.graphicsFormat);
            Assert.AreEqual(bake.Position.width, bake.Rotation.width);
            Assert.AreEqual(bake.Position.height, bake.Rotation.height);
        }

        private static void AssertVertices(VatInMemoryBake bake, Vector2[] sourceUv)
        {
            var mesh = bake.Mesh;
            Assert.IsTrue(mesh.isReadable);
            CollectionAssert.AreEqual(bake.Rest, mesh.vertices, "positions are Float32 rest positions");
            var uv = mesh.uv;
            var normals = mesh.normals;
            for (var v = 0; v < mesh.vertexCount; v++)
            {
                Assert.That((uv[v] - sourceUv[v]).magnitude, Is.LessThan(1e-3f), "uv, Float16");
                Assert.That((normals[v] - bake.RestNormals[v]).magnitude, Is.LessThan(1e-3f), "normal, Float16");
                Assert.That((bake.RestNormals[v] - RestNormal).magnitude, Is.LessThan(1e-5f), "normal in root space");
            }
        }

        private static void AssertSubMeshes(Mesh mesh, Mesh source)
        {
            Assert.AreEqual(source.subMeshCount, mesh.subMeshCount);
            for (var s = 0; s < mesh.subMeshCount; s++)
            {
                Assert.AreEqual(0, mesh.GetSubMesh(s).baseVertex, "baseVertex = 0 on every sub-mesh");
                CollectionAssert.AreEqual(source.GetIndices(s, true), mesh.GetIndices(s, true));
            }
        }

        private void AssertDecodesToReference(VatAsset asset, byte[] texels)
        {
            var clip = asset.Clips[0];
            var rest = asset.Mesh.vertices;
            var drift = VatTestUtil.ReadGpu(asset.DriftTexture);
            var maxError = 0f;
            for (var k = 0; k < clip.FrameCount; k++)
            {
                var reference = _rig.ReferencePositions(clip.FrameTime(k));
                var d = VatTestUtil.DecodeDrift(drift, clip.StartRow + k);
                for (var v = 0; v < rest.Length; v++)
                {
                    var decoded = rest[v] + d + VatTestUtil.DecodeOffset(texels, asset.Layout, v, clip.StartRow + k);
                    maxError = Mathf.Max(maxError, (decoded - reference[v]).magnitude);
                    Assert.IsTrue(Contains(asset.Mesh.bounds, decoded), $"vertex {v} of frame {k} outside bounds");
                }
            }

            Assert.Less(maxError, Tolerance, $"max error {maxError * 1000f:0.###} mm");
        }

        private static void AssertReferencesSurviveReload(VatAsset asset, string materialPath, string prefabPath)
        {
            var reloaded = Reload(asset);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.AreEqual(reloaded.PositionTexture, material.GetTexture(VatShaderIds.PosTex));
            Assert.AreEqual(reloaded.RotationTexture, material.GetTexture(VatShaderIds.RotTex));
            Assert.AreEqual(reloaded.Mesh, prefab.GetComponent<MeshFilter>().sharedMesh);
            Assert.AreEqual(10, material.GetTexture(VatShaderIds.PosTex).height);
            Assert.AreEqual(reloaded.Layout.ShaderLayout, material.GetVector(VatShaderIds.Layout));
            Assert.AreEqual(reloaded.Clips[0].Frame(0.0), material.GetVector(VatShaderIds.Frame));
            Assert.IsFalse(material.enableInstancing);
        }

        private static VatBakeProfile CreateProfile(VatTestRig rig)
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            profile.Source = rig.Renderer;
            profile.SetClips(rig.Clip);
            profile.Fps = Fps;
            profile.Shader = Shader.Find(VatBaker.DefaultShaderName);
            return profile;
        }

        private static VatAsset Reload(VatAsset asset)
        {
            Resources.UnloadAsset(asset.PositionTexture);
            Resources.UnloadAsset(asset.RotationTexture);
            Resources.UnloadAsset(asset.Mesh);
            return AssetDatabase.LoadAssetAtPath<VatAsset>(AssetDatabase.GetAssetPath(asset));
        }

        private static long[] Ids(VatAsset asset)
        {
            return new[] { Id(asset), Id(asset.Mesh), Id(asset.PositionTexture), Id(asset.RotationTexture) };
        }

        private static long Id(Object target)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string _, out var id), $"{target} is not an asset");
            return id;
        }

        private static bool Contains(Bounds bounds, Vector3 point)
        {
            return bounds.Contains(point) || bounds.SqrDistance(point) < 1e-10f;
        }
    }
}
