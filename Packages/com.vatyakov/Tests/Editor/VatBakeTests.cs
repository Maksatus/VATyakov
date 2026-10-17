using System;
using System.Collections.Generic;
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
        private const string FormatVersionProperty = "_formatVersion";
        private const int FormatThree = 3;
        private const float Fps = 30f;
        private const float RebakeFps = 10f;
        private const float Tolerance = 5e-4f;
        private const float AngleTolerance = 1f;
        private const float MinTopTwist = 30f;
        private const int TwoBlockVertexCount = 5000;
        private const int OneBlockVertexCount = 300;
        private const float Millimeter = 1e-3f;
        private const float GpuTolerance = 1e-5f;
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private static readonly Vector3 _modelShift = new(0.1f, -0.2f, 0.3f);

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
            Assert.IsTrue(reloaded.TryValidate(out var error), error);
            var template = _profile.Material;
            Assert.AreEqual(reloaded.Drift(template.GetVector(VatShaderIds.Frame)), template.GetVector(VatShaderIds.Drift), "template gets _VatDrift");
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
        public void Bake_EndsOnTheClipEnd_AndLoopsLikeTheClip([Values] bool isLegacy, [Values] bool isLoopingClip)
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy, isLoopingClip);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/EveryFrame.asset");

            var clip = asset.Clips[0];
            Assert.AreEqual(isLoopingClip, clip.IsLooping, "the clip loops when its Loop Time or Wrap Mode says so");
            Assert.AreEqual(FrameCount(Fps), clip.FrameCount, "round(L·fps) + 1");
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
            var prefabPath = CreatePrefab(asset, _profile.Material);
            var ids = Ids(asset);
            var materialPath = AssetDatabase.GetAssetPath(_profile.Material);
            Assert.AreEqual(FrameCount(Fps), asset.PositionTexture.height, "one row per frame");

            _profile.Fps = RebakeFps;
            var rebaked = VatBaker.Bake(_profile);

            Assert.AreSame(asset, rebaked, "the rebake updates the same asset");
            CollectionAssert.AreEqual(ids, Ids(rebaked), "file ids survive the rebake");
            Assert.AreEqual(FrameCount(RebakeFps), rebaked.PositionTexture.height, "rows after the rebake");
            AssertReferencesSurviveReload(rebaked, materialPath, prefabPath);
        }

        [Test]
        public void Rebake_OfAMovedModel_PutsTheNewVerticesOnTheGpu([Values(VatMode.Vertex, VatMode.Bone)] VatMode mode)
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            _profile.Mode = mode;
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Moved.asset");
            var before = VatTestUtil.GpuPositions(asset.Mesh);
            _rig.Root.transform.Find("Model").localPosition += _modelShift;

            var rebaked = VatBaker.Bake(_profile);

            var rest = rebaked.Mesh.vertices;
            var gpu = VatTestUtil.GpuPositions(rebaked.Mesh);
            Assert.That(Vector3.Distance(before[0], rest[0]), Is.GreaterThan(Millimeter), "the move changes the rest pose");
            for (var v = 0; v < rest.Length; v++)
            {
                Assert.That(Vector3.Distance(gpu[v], rest[v]), Is.LessThan(GpuTolerance), $"vertex {v} on the GPU is the rebaked one");
            }
        }

        [Test]
        public void DuplicatedProfile_SharesTheResult_UntilItIsCleared()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            var original = CreateProfile(_rig);
            AssetDatabase.CreateAsset(original, $"{TempFolder}/original.asset");
            VatBaker.Bake(original);
            var copyPath = $"{TempFolder}/copy.asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), copyPath);
            var copy = AssetDatabase.LoadAssetAtPath<VatBakeProfile>(copyPath);
            copy.Source = _rig.Renderer;
            copy.SetClips(_rig.Clip);

            AssertProblem(VatBaker.Validate(copy), "VAT Asset 'original_vat' is baked by profile 'original' too");
            AssertProblem(VatBaker.Validate(original), "VAT Asset 'original_vat' is baked by profile 'copy' too");

            copy.Asset = null;
            AssertProblem(VatBaker.Validate(copy), "Template Material 'original_vat' is baked by profile 'original' too");

            copy.Material = null;
            CollectionAssert.IsEmpty(VatBaker.Validate(copy), "a cleared copy has its own result");
            CollectionAssert.IsEmpty(VatBaker.Validate(original), "the original owns its result again, before the copy is saved");
            Assert.AreNotSame(original.Asset, VatBaker.Bake(copy), "the copy bakes a separate asset");
        }

        [Test]
        public void BlendShape_InVertexMode_MatchesBakeMesh()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: true, hasBlendShape: true);
            var bake = new VatInMemoryBake(_rig, Fps);
            try
            {
                var texels = VatTestUtil.ReadGpu(bake.Position);
                var clip = bake.Layout.Clips[0];
                var maxError = 0f;
                for (var k = 0; k < clip.FrameCount; k++)
                {
                    var reference = VatTestUtil.BakeMesh(_rig.Root, _rig.Renderer, _rig.Clip, clip.FrameTime(k), SkinQuality.Bone4, out _);
                    for (var v = 0; v < reference.Length; v++)
                    {
                        maxError = Mathf.Max(maxError, Vector3.Distance(bake.DecodePosition(texels, bake.Rest[v], v, clip.StartRow + k), reference[v]));
                    }
                }

                Assert.Less(maxError, bake.Precision.Error + Tolerance, "decoded positions against SkinnedMeshRenderer.BakeMesh");
            }
            finally
            {
                bake.Destroy();
            }
        }

        [Test]
        public void DuplicatedProfile_GetsItsOwnResult_OnItsFirstBake()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            var original = CreateProfile(_rig);
            AssetDatabase.CreateAsset(original, $"{TempFolder}/original.asset");
            var asset = VatBaker.Bake(original);
            var copyPath = $"{TempFolder}/copy.asset";
            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(original), copyPath);
            var copy = AssetDatabase.LoadAssetAtPath<VatBakeProfile>(copyPath);
            copy.Source = _rig.Renderer;
            copy.SetClips(_rig.Clip);
            Assert.AreSame(asset, copy.Asset, "the duplicate starts with the references of the original");

            var copyAsset = VatBaker.Bake(copy);

            Assert.AreNotSame(asset, copyAsset, "the duplicate bakes a separate asset");
            Assert.AreNotSame(original.Material, copy.Material, "and a separate template");
            VatProfileDuplicate.Detach(original);
            Assert.AreSame(asset, original.Asset, "the original keeps its result");
        }

        [Test]
        public void TemplateMaterial_WithoutAVatShader_IsAProblem()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            var material = new Material(Shader.Find(LitShaderName)) { name = "Lit" };
            try
            {
                _profile.Material = material;
                AssertProblem(VatBaker.Validate(_profile), $"Template Material 'Lit' uses '{LitShaderName}', not a VAT shader");
            }
            finally
            {
                Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void Rebake_FormatThreeAsset_PlaysAgainWithoutTheDriftTexture()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/FormatThree.asset");
            MakeFormatThree(asset);
            Assert.IsFalse(asset.TryValidate(out var error), "format 3 does not play");
            StringAssert.Contains("Rebake", error);

            var rebaked = VatBaker.Bake(_profile);

            Assert.IsTrue(rebaked.TryValidate(out error), error);
            var parts = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(rebaked));
            CollectionAssert.AreEquivalent(new Object[] { rebaked.Mesh, rebaked.PositionTexture, rebaked.RotationTexture }, parts, "no drift texture left");
        }

        [Test]
        public void SourceHash_ChangedSettings_OutdateTheBakeUntilRebake()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Outdated.asset");
            Assert.IsFalse(VatSourceHash.IsOutdated(_profile, asset), "a fresh bake");

            _profile.Fps = RebakeFps;
            Assert.IsTrue(VatSourceHash.IsOutdated(_profile, asset), "fps changed");

            VatBaker.Bake(_profile);
            Assert.IsFalse(VatSourceHash.IsOutdated(_profile, asset), "rebaked");

            _profile.SetClips(_rig.Clip, _rig.BuildPartialClip("Partial"));
            Assert.IsTrue(VatSourceHash.IsOutdated(_profile, asset), "a clip added");

            _profile.Source = null;
            Assert.IsFalse(VatSourceHash.IsOutdated(_profile, asset), "an invalid profile has no hash to compare");
        }

        [Test]
        public void BatchBake_BakesTheValidProfiles_AndNamesTheFailedOnes()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Batch.asset");
            _profile.Fps = RebakeFps;
            var broken = ScriptableObject.CreateInstance<VatBakeProfile>();
            broken.name = "Broken";
            try
            {
                var error = Assert.Throws<VatBakeException>(() => VatBatchBake.BakeAll(new[] { broken, _profile }));

                StringAssert.Contains("'Broken'", error.Message);
                Assert.IsFalse(VatSourceHash.IsOutdated(_profile, asset), "the valid profile is baked after the broken one");
            }
            finally
            {
                Object.DestroyImmediate(broken);
            }
        }

        [Test]
        public void SourceHash_AnotherSourceMesh_OutdatesTheBake()
        {
            _rig = new VatTestRig(_scene, OneBlockVertexCount, isLegacy: false);
            _profile = CreateProfile(_rig);
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Remesh.asset");
            var mesh = _rig.Renderer.sharedMesh;
            var reexported = Object.Instantiate(mesh);
            try
            {
                _rig.Renderer.sharedMesh = reexported;
                Assert.IsTrue(VatSourceHash.IsOutdated(_profile, asset), "the source mesh changed");
            }
            finally
            {
                _rig.Renderer.sharedMesh = mesh;
                Object.DestroyImmediate(reexported);
            }
        }

        [Test]
        public void Layout_Of4096Rows_Fits_4097RowsFail()
        {
            var fits = VatLayout.ForVertex(OneBlockVertexCount, new[] { new VatClipRequest("Fits", VatMath.MaxTextureSize - 1, 1f, false) });
            Assert.AreEqual(VatMath.MaxTextureSize, fits.Info.Height, "4096 rows fit");

            var tall = new[] { new VatClipRequest("Tall", VatMath.MaxTextureSize, 1f, false) };
            var error = Assert.Throws<VatBakeException>(() => VatLayout.ForVertex(OneBlockVertexCount, tall));
            StringAssert.Contains("= 4097 rows, the limit is 4096", error.Message);
        }

        private static void AssertProblem(List<string> problems, string expected)
        {
            Assert.That(problems.Exists(problem => problem.StartsWith(expected, StringComparison.Ordinal)), $"'{expected}' in:\n{string.Join("\n", problems)}");
        }

        private static int FrameCount(float fps)
        {
            return Mathf.RoundToInt(VatTestRig.Length * fps) + 1;
        }

        private static void AssertFormat(VatInMemoryBake bake)
        {
            CollectionAssert.AreEqual(VatVertexFormat.Attributes, bake.Mesh.GetVertexAttributes(), "vertex attributes");
            Assert.AreEqual(VatVertexFormat.Strides[0], bake.Mesh.GetVertexBufferStride(0), "stride of stream 0");
            Assert.AreEqual(VatVertexFormat.Strides[1], bake.Mesh.GetVertexBufferStride(1), "stride of stream 1");
            Assert.AreEqual(VatVertexFormat.Position(bake.PositionFormat), bake.Position.graphicsFormat, "position texture format");
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
            var maxError = 0f;
            for (var k = 0; k < clip.FrameCount; k++)
            {
                var reference = _rig.ReferencePositions(clip.FrameTime(k));
                var frameDrift = asset.Drift(VatTestUtil.Row(clip.StartRow + k));
                for (var v = 0; v < rest.Length; v++)
                {
                    var sample = VatTestUtil.PositionSample(texels, asset.Layout, asset.PositionFormat, v, clip.StartRow + k);
                    var decoded = VatTestUtil.ShaderPosition(rest[v], frameDrift, sample, asset.PositionRange);
                    maxError = Mathf.Max(maxError, (decoded - reference[v]).magnitude);
                    Assert.IsTrue(Contains(asset.Mesh.bounds, decoded), $"vertex {v} of frame {k} outside bounds");
                }
            }

            Assert.Less(maxError, asset.Precision.Error + Tolerance, $"max error {maxError * 1000f:0.###} mm");
        }

        private static void AssertReferencesSurviveReload(VatAsset asset, string materialPath, string prefabPath)
        {
            var reloaded = Reload(asset);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.AreEqual(reloaded.PositionTexture, material.GetTexture(VatShaderIds.PositionTexture), "material keeps the position texture");
            Assert.AreEqual(reloaded.RotationTexture, material.GetTexture(VatShaderIds.RotationTexture), "material keeps the rotation texture");
            Assert.AreEqual(reloaded.Mesh, prefab.GetComponent<MeshFilter>().sharedMesh, "prefab keeps the mesh");
            Assert.AreEqual(FrameCount(RebakeFps), material.GetTexture(VatShaderIds.PositionTexture).height, "material sees the rebaked rows");
            Assert.AreEqual(reloaded.Layout.ShaderLayout, material.GetVector(VatShaderIds.Layout), "material layout");
            Assert.AreEqual(reloaded.PositionRange.ShaderScale, material.GetVector(VatShaderIds.PositionScale), "material position scale");
            Assert.AreEqual(reloaded.Clips[0].Frame(0.0), material.GetVector(VatShaderIds.Frame), "material shows frame 0");
            Assert.IsFalse(material.enableInstancing, "instancing stays off");
        }

        private static VatBakeProfile CreateProfile(VatTestRig rig)
        {
            var profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            profile.Source = rig.Renderer;
            profile.SetClips(rig.Clip);
            profile.Fps = Fps;
            return profile;
        }

        private static void MakeFormatThree(VatAsset asset)
        {
            var drift = new Texture2D(2, 1, TextureFormat.RGBAHalf, false) { name = $"{asset.name}_drift" };
            AssetDatabase.AddObjectToAsset(drift, asset);
            using var serialized = new SerializedObject(asset);
            serialized.FindProperty(FormatVersionProperty).intValue = FormatThree;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }

        private string CreatePrefab(VatAsset asset, Material material)
        {
            var root = new GameObject("Rebake");
            SceneManager.MoveGameObjectToScene(root, _scene);
            root.AddComponent<MeshFilter>().sharedMesh = asset.Mesh;
            root.AddComponent<MeshRenderer>().sharedMaterial = material;
            var path = $"{TempFolder}/Rebake.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return path;
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
