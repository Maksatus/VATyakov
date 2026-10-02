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
        private const float RebakeFps = 10f;
        private const float Tolerance = 5e-4f;
        private const float AngleTolerance = 0.3f;
        private const float MinTopTwist = 30f;
        private const int TwoBlockVertexCount = 5000;
        private const int OneBlockVertexCount = 300;

        private Scene _scene;
        private VatTestRig _rig;
        private VatBakeProfile _profile;

        private static Vector3 RestNormal => Quaternion.Euler(VatTestRig.ModelPitch, 0f, 0f) * Vector3.back;

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
            _rig = new VatTestRig(_scene, TwoBlockVertexCount, isLegacy: false);
            var bake = new VatInMemoryBake(_rig, Fps);
            try
            {
                Assert.AreEqual(2, bake.Layout.Info.Blocks, $"{TwoBlockVertexCount} vertices need two blocks");
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
        public void Bake_Reload_TextureBytesAreIdentical_AndDecodeToReference([Values] bool isLegacy)
        {
            _rig = new VatTestRig(_scene, TwoBlockVertexCount, isLegacy);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Reload.asset");
            var before = VatTestUtil.ReadGpu(asset.PositionTexture);
            var rotationBefore = VatTestUtil.ReadGpu(asset.RotationTexture);

            var reloaded = Reload(asset);
            var after = VatTestUtil.ReadGpu(reloaded.PositionTexture);

            CollectionAssert.AreEqual(before, after, "texture bytes after reload");
            CollectionAssert.AreEqual(rotationBefore, VatTestUtil.ReadGpu(reloaded.RotationTexture), "rotation bytes after reload");
            Assert.IsFalse(reloaded.PositionTexture.isReadable, "position texture is not readable");
            Assert.IsFalse(reloaded.RotationTexture.isReadable, "rotation texture is not readable");
            Assert.IsFalse(reloaded.Mesh.isReadable, "mesh is not readable");
            Assert.IsFalse(reloaded.DriftTexture.isReadable, "drift texture is not readable");
            Assert.AreEqual(reloaded.DriftTexture, _profile.Material.GetTexture(VatShaderIds.DriftTexture), "template gets _VatDriftTex");
            Assert.AreEqual(VatAsset.CurrentFormatVersion, reloaded.FormatVersion, "format version");
            AssertDecodesToReference(reloaded, after);
        }

        [Test]
        public void TwistedStrip_RotationTextureDecodesToSkinnedNormalsAndTangents()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: true, hasTwist: true);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Twist.asset");
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
                    var rotation = VatTestUtil.DecodeRotation(texels, asset.Layout, v, clip.StartRow + k);
                    var tangent = (tangents[v] - normals[v] * Vector3.Dot(normals[v], tangents[v])).normalized;
                    maxNormal = Mathf.Max(maxNormal, Vector3.Angle(normals[v], VatMath.FrameNormal(rotation)));
                    maxTangent = Mathf.Max(maxTangent, Vector3.Angle(tangent, VatMath.FrameTangent(rotation)));
                }
            }

            Assert.Less(maxNormal, AngleTolerance, "normal, degrees");
            Assert.Less(maxTangent, AngleTolerance, "tangent, degrees");
            Assert.Greater(Vector3.Angle(_rig.ReferenceNormals(0.5)[_rig.Renderer.sharedMesh.vertexCount - 1], RestNormal), MinTopTwist,
                "the twist turns the top of the strip");
            Assert.IsTrue(Array.TrueForAll(asset.Mesh.tangents, tangent => tangent.w == 1f), "bitangent sign from the rest tangent");
        }

        [Test]
        public void OneShotBake_EndsOnTheClipEnd([Values] bool isLegacy, [Values] bool isLoopingClip)
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy, isLoopingClip);
            _profile = CreateProfile(_rig);
            _profile.IsLooping = false;
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/OneShot.asset");

            var clip = asset.Clips[0];
            Assert.IsFalse(clip.IsLooping, "the clip is baked as a one-shot");
            Assert.AreEqual(LoopFrameCount(Fps) + 1, clip.FrameCount, "round(L·fps) + 1");
            Assert.AreEqual(Fps, clip.FrameRate, 1e-4f, "fps_eff = (F − 1) / L");
            Assert.AreEqual(VatTestRig.Length, clip.FrameTime(clip.FrameCount - 1), 1e-9, "the last frame is the clip end");
            Assert.AreEqual(clip.Frame(0.0), _profile.Material.GetVector(VatShaderIds.Frame), "template shows frame 0");
            AssertDecodesToReference(asset, VatTestUtil.ReadGpu(asset.PositionTexture));
        }

        [Test]
        public void Rebake_WithOtherFps_KeepsFileIdsAndReferences()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Rebake.asset");
            Assert.IsNull(_profile.Prefab, "a bake never creates a prefab");
            VatBaker.CreatePrefab(_profile);
            var ids = Ids(asset);
            var materialPath = AssetDatabase.GetAssetPath(_profile.Material);
            var prefabPath = AssetDatabase.GetAssetPath(_profile.Prefab);
            Assert.AreEqual(LoopFrameCount(Fps), asset.PositionTexture.height, "one row per frame");

            _profile.Fps = RebakeFps;
            var rebaked = VatBaker.Bake(_profile);

            Assert.AreSame(asset, rebaked, "the rebake updates the same asset");
            CollectionAssert.AreEqual(ids, Ids(rebaked), "file ids survive the rebake");
            Assert.AreEqual(LoopFrameCount(RebakeFps), rebaked.PositionTexture.height, "rows after the rebake");
            AssertReferencesSurviveReload(rebaked, materialPath, prefabPath);
        }

        [Test]
        public void Layout_TallerThan4096Rows_FailsWithAClearMessage()
        {
            var clips = new[] { new VatClipRequest("Long", 100f, Fps) };
            var error = Assert.Throws<VatBakeException>(() => VatLayout.ForVertex(TwoBlockVertexCount, clips));
            StringAssert.Contains("4096", error.Message);
            StringAssert.Contains("fps", error.Message);
        }

        private static int LoopFrameCount(float fps)
        {
            return Mathf.RoundToInt(VatTestRig.Length * fps);
        }

        private static void AssertFormat(VatInMemoryBake bake)
        {
            CollectionAssert.AreEqual(VatVertexFormat.Attributes, bake.Mesh.GetVertexAttributes(), "vertex attributes");
            Assert.AreEqual(VatVertexFormat.Strides[0], bake.Mesh.GetVertexBufferStride(0), "stride of stream 0");
            Assert.AreEqual(VatVertexFormat.Strides[1], bake.Mesh.GetVertexBufferStride(1), "stride of stream 1");
            Assert.AreEqual(VatVertexFormat.Position, bake.Position.graphicsFormat, "position texture format");
            Assert.AreEqual(bake.Layout.Info.Width, bake.Position.width, "position texture width");
            Assert.AreEqual(bake.Layout.Info.Height, bake.Position.height, "position texture height");
            Assert.AreEqual(1, bake.Position.mipmapCount, "no mipmaps");
            Assert.AreEqual(VatVertexFormat.Rotation, bake.Rotation.graphicsFormat, "rotation texture format");
            Assert.AreEqual(bake.Position.width, bake.Rotation.width, "rotation texture width");
            Assert.AreEqual(bake.Position.height, bake.Rotation.height, "rotation texture height");
        }

        private static void AssertVertices(VatInMemoryBake bake, Vector2[] sourceUv)
        {
            var mesh = bake.Mesh;
            Assert.IsTrue(mesh.isReadable, "the in-memory mesh is readable");
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
            Assert.AreEqual(source.subMeshCount, mesh.subMeshCount, "sub-mesh count");
            for (var s = 0; s < mesh.subMeshCount; s++)
            {
                Assert.AreEqual(0, mesh.GetSubMesh(s).baseVertex, "baseVertex = 0 on every sub-mesh");
                CollectionAssert.AreEqual(source.GetIndices(s, true), mesh.GetIndices(s, true), $"indices of sub-mesh {s}");
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
                var frameDrift = VatTestUtil.DecodeDrift(drift, clip.StartRow + k);
                for (var v = 0; v < rest.Length; v++)
                {
                    var decoded = rest[v] + frameDrift + VatTestUtil.DecodeOffset(texels, asset.Layout, v, clip.StartRow + k);
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
            Assert.AreEqual(reloaded.PositionTexture, material.GetTexture(VatShaderIds.PositionTexture), "material keeps the position texture");
            Assert.AreEqual(reloaded.RotationTexture, material.GetTexture(VatShaderIds.RotationTexture), "material keeps the rotation texture");
            Assert.AreEqual(reloaded.Mesh, prefab.GetComponent<MeshFilter>().sharedMesh, "prefab keeps the mesh");
            Assert.AreEqual(LoopFrameCount(RebakeFps), material.GetTexture(VatShaderIds.PositionTexture).height, "material sees the rebaked rows");
            Assert.AreEqual(reloaded.Layout.ShaderLayout, material.GetVector(VatShaderIds.Layout), "material layout");
            Assert.AreEqual(reloaded.Clips[0].Frame(0.0), material.GetVector(VatShaderIds.Frame), "material shows frame 0");
            Assert.IsFalse(material.enableInstancing, "instancing stays off");
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
