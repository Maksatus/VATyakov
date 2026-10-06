using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VATyakov.Editor;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatBoneTests
    {
        private const string TempFolder = "Assets/__VatBoneTestTemp";
        private const float Fps = 30f;
        private const float SwingFps = 1f;
        private const int VertexCount = 64;
        private const int HalfWeightVertexCount = 66;
        private const float Millimeter = 1e-3f;
        private const float AngleTolerance = 1f;
        private const float BoundsTolerance = 1e-5f;
        private const int ArcSteps = 64;
        private const int Seed = 11;
        private const int RandomMatrices = 200;
        private const float MinScale = 0.3f;
        private const float MaxScale = 3f;

        private static readonly Vector3 _nonUniformScale = new(2f, 1f, 0.5f);

        private Scene _scene;
        private VatTestRig _rig;
        private VatSwingRig _swing;
        private VatTriadRig _triad;
        private VatBakeProfile _profile;
        private VatBakeResult _result;

        [SetUp]
        public void SetUp()
        {
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (_result != null)
            {
                Object.DestroyImmediate(_result.Mesh);
                _result.Textures.Destroy();
            }

            _rig?.Destroy();
            _swing?.Destroy();
            _triad?.Destroy();
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _result = null;
            _rig = null;
            _swing = null;
            _triad = null;
            _profile = null;
            EditorSceneManager.ClosePreviewScene(_scene);
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void SkinMatrix_Diag2_1_Half_IsNotAUniformScale()
        {
            var similarity = VatSimilarity.Of(Matrix4x4.Scale(_nonUniformScale));

            Assert.IsTrue(similarity.IsProper, "det = 1, no mirror");
            Assert.AreEqual(1f, similarity.Scale, 1e-6f, "s = cbrt(det)");
            Assert.Greater(similarity.Residual, VatBoneCheck.MaxResidual, "‖B − sR‖ / s");
        }

        [Test]
        public void SkinMatrix_Mirror_IsNotProper()
        {
            Assert.IsFalse(VatSimilarity.Of(Matrix4x4.Scale(new Vector3(-1f, 1f, 1f))).IsProper);
        }

        [Test]
        public void SkinMatrix_RotationAndUniformScale_DecomposeExactly()
        {
            var random = new Random(Seed);
            for (var i = 0; i < RandomMatrices; i++)
            {
                var q = VatTestUtil.RandomRotation(random);
                var rotation = new Quaternion(q.x, q.y, q.z, q.w);
                var scale = Mathf.Lerp(MinScale, MaxScale, (float)random.NextDouble());
                var similarity = VatSimilarity.Of(Matrix4x4.TRS(Vector3.one, rotation, Vector3.one * scale));
                var probe = VatTestUtil.RandomDirection(random);

                Assert.Less(similarity.Residual, 1e-5, $"matrix {i}");
                Assert.AreEqual(scale, similarity.Scale, scale * 1e-5f, $"scale of matrix {i}");
                Assert.Less(Vector3.Distance(rotation * probe, VatMath.Rotate(similarity.Rotation, probe)), 1e-5f, $"rotation of matrix {i}");
            }
        }

        [Test]
        public void NonUniformScaleBone_BakesTheAssetAsVertex()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: true);
            _rig.Root.transform.Find("Model/Bone0/Bone1").localScale = _nonUniformScale;

            _result = VatBakePipeline.Run(Profile(_rig.Renderer, _rig.Clip, Fps, true), "Scaled");

            Assert.AreEqual(VatMode.Vertex, _result.Mode, "the asset falls back to Vertex");
            Assert.IsNotNull(_result.Textures.Position, "Vertex textures");
            StringAssert.Contains("bone 'Bone1' scales non-uniformly", _result.Fallback);
            StringAssert.Contains($"Clip '{_rig.Clip.name}', frame 0", _result.Fallback);
        }

        [Test]
        public void BlendShape_BakesTheAssetAsVertex()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false, hasBlendShape: true);

            _result = VatBakePipeline.Run(Profile(_rig.Renderer, _rig.Clip, Fps, true), "BlendShape");

            Assert.AreEqual(VatMode.Vertex, _result.Mode, "Bone mode does not skin blend shapes");
            StringAssert.Contains("blend shape 'Bulge'", _result.Fallback);
        }

        [Test]
        public void Capsule_ContainsTheArc_OfATurnFromMinus30To90DegreesInOneFrame()
        {
            _swing = new VatSwingRig(_scene);

            _result = VatBakePipeline.Run(Profile(_swing.Renderer, _swing.Clip, SwingFps, false), "Swing");

            Assert.AreEqual(VatMode.Bone, _result.Mode, _result.Fallback);
            var clip = _result.Layout.Clips[0];
            Assert.AreEqual(2, clip.FrameCount, "the whole turn is one frame step");
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var boneUvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            var rest = _result.Mesh.vertices;
            var keys = new VatBoundsBuilder();
            var isArcOutsideKeys = false;
            for (var step = 0; step <= ArcSteps; step++)
            {
                var frame = new Vector4(clip.StartRow, clip.StartRow + 1, step / (float)ArcSteps, 0f);
                for (var v = 0; v < rest.Length; v++)
                {
                    var point = decoder.Position(boneUvs[v], rest[v], frame);
                    AssertContains(_result.Mesh.bounds, point, $"mesh bounds at {frame.z}");
                    AssertContains(_result.Mesh.GetSubMesh(0).bounds, point, $"sub-mesh bounds at {frame.z}");
                    if (step == 0 || step == ArcSteps)
                    {
                        keys.Add(point);
                    }
                }
            }

            for (var v = 0; v < rest.Length; v++)
            {
                isArcOutsideKeys |= !Contains(keys.Bounds, decoder.Position(boneUvs[v], rest[v], new Vector4(clip.StartRow, clip.StartRow + 1, 0.25f, 0f)));
            }

            Assert.IsTrue(isArcOutsideKeys, "control: the arc leaves the box of the two keyed poses");
        }

        [Test]
        public void Reconstruction_MatchesBakeMeshWithTwoBones_WithinAMillimeter()
        {
            _rig = new VatTestRig(_scene, HalfWeightVertexCount, isLegacy: true, hasTwist: true);

            _result = VatBakePipeline.Run(Profile(_rig.Renderer, _rig.Clip, Fps, false), "Reconstruct");

            Assert.AreEqual(VatMode.Bone, _result.Mode, _result.Fallback);
            Assert.That(_rig.Renderer.sharedMesh.boneWeights.Any(weight => weight.weight0 == 0.5f), "the strip has a vertex at weight 0.5");
            var errors = Reconstruct(_rig.Root, _rig.Renderer, _rig.Clip, out var angle);
            Assert.Less(errors[SkinQuality.Bone2], Millimeter, "positions against BakeMesh at Skin Weights = 2 Bones");
            Assert.Less(angle, AngleTolerance, "normals against BakeMesh at Skin Weights = 2 Bones");
            Assert.Greater(errors[SkinQuality.Bone1], 10f * Millimeter, "control: one-bone skinning is visibly different");
        }

        [Test]
        public void TwoBones_FollowUnitySlotOrder_WithTiesUnsortedSlotsAndALongLever()
        {
            _triad = new VatTriadRig(_scene,
                Weight(0, 0.5f, 1, 0.5f),
                Weight(0, 0.5f, 1, 0.3f, 2, 0.2f),
                Weight(0, 0.5f, 2, 0.25f, 1, 0.25f),
                Weight(0, 0.5f, 1, 0.25f, 2, 0.25f),
                Weight(1, 0.2f, 0, 0.5f, 2, 0.3f),
                Weight(2, 1f));

            _result = VatBakePipeline.Run(Profile(_triad.Renderer, _triad.Clip, Fps, false), "Triad");

            Assert.AreEqual(VatMode.Bone, _result.Mode, _result.Fallback);
            var influences = VatBoneDecoder.BoneUvs(_result.Mesh).Select(VatBoneDecoder.Influence).ToArray();
            Assert.AreEqual(new Vector2Int(0, 2), Bones(influences[2 * VatTriadRig.VerticesPerCase]), "a tie keeps the slot order: bone 2 first");
            Assert.AreEqual(new Vector2Int(0, 1), Bones(influences[3 * VatTriadRig.VerticesPerCase]), "a tie keeps the slot order: bone 1 first");
            Assert.AreEqual(new Vector2Int(1, 0), Bones(influences[4 * VatTriadRig.VerticesPerCase]), "unsorted slots are not sorted");
            Assert.AreEqual(new Vector2Int(2, 2), Bones(influences[5 * VatTriadRig.VerticesPerCase]), "one weight: both slots on one bone");
            var errors = Reconstruct(_triad.Root, _triad.Renderer, _triad.Clip, out _);
            Assert.Less(errors[SkinQuality.Bone2], Millimeter, "positions against BakeMesh at Skin Weights = 2 Bones");
            Assert.Greater(errors[SkinQuality.Bone4], 10f * Millimeter, "control: four-bone skinning is visibly different");
        }

        [Test]
        public void ZeroFrame_IsTheFirstFrameOfTheFirstClip_PivotsAreTheLastRow()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);

            _result = VatBakePipeline.Run(Profile(_rig.Renderer, _rig.Clip, Fps, true), "Zero");

            var info = _result.Layout.Info;
            var clip = _result.Layout.Clips[0];
            Assert.AreEqual(0, clip.StartRow, "clips start at row 0, as in Vertex");
            Assert.AreEqual(clip.FrameCount + 1, info.TotalRows, "frames and one pivot row");
            Assert.AreEqual(info.TotalRows, _result.Textures.Bone.height);
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var boneUvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            var rest = _result.Mesh.vertices;
            for (var v = 0; v < rest.Length; v++)
            {
                var zero = decoder.Position(boneUvs[v], rest[v], Vector4.zero);
                Assert.IsTrue(float.IsFinite(zero.x) && float.IsFinite(zero.y) && float.IsFinite(zero.z), $"vertex {v} at _VatFrameB = 0");
                Assert.AreEqual(decoder.Position(boneUvs[v], rest[v], clip.Frame(0.0)), zero, $"vertex {v}: (0, 0, 0, 0) is frame 0");
            }
        }

        [Test]
        public void BoneMesh_HasBindPoseAndTheFirstTwoSlotsInTexCoord6()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);

            _result = VatBakePipeline.Run(Profile(_rig.Renderer, _rig.Clip, Fps, true), "Layout");

            var mesh = _result.Mesh;
            CollectionAssert.AreEqual(VatBoneFormat.Attributes, mesh.GetVertexAttributes(), "streams and formats");
            Assert.IsFalse(mesh.HasVertexAttribute(VertexAttribute.TexCoord4), "TEXCOORD4 belongs to URP motion vectors");
            var source = _rig.Renderer.sharedMesh;
            var toRoot = _rig.Root.transform.worldToLocalMatrix * _rig.Renderer.transform.localToWorldMatrix;
            var weights = source.boneWeights;
            var boneUvs = VatBoneDecoder.BoneUvs(mesh);
            var rest = mesh.vertices;
            for (var v = 0; v < rest.Length; v++)
            {
                Assert.AreEqual(Expected(weights[v]), VatBoneDecoder.Influence(boneUvs[v]), $"bones and 16-bit weight of vertex {v}");
                Assert.Less(Vector3.Distance(toRoot.MultiplyPoint3x4(source.vertices[v]), rest[v]), 1e-5f, $"bind pose of vertex {v}");
            }

            for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                Assert.AreEqual(0, mesh.GetSubMesh(subMesh).baseVertex, $"baseVertex of sub-mesh {subMesh}");
            }
        }

        [Test]
        public void Rebake_VertexToBone_SwapsTheTexturesAndTheShader_KeepsTheMesh()
        {
            AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);
            _profile = Profile(_rig.Renderer, _rig.Clip, Fps, true);
            _profile.Mode = VatMode.Vertex;
            var asset = VatBaker.Bake(_profile, $"{TempFolder}/Swap.asset");
            var meshId = Id(asset.Mesh);

            _profile.Mode = VatMode.Bone;
            var rebaked = VatBaker.Bake(_profile);

            Assert.AreSame(asset, rebaked, "the rebake updates the same asset");
            Assert.AreEqual(VatMode.Bone, rebaked.Mode);
            Assert.AreEqual(meshId, Id(rebaked.Mesh), "the mesh keeps its file id");
            var parts = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(rebaked));
            CollectionAssert.AreEquivalent(new Object[] { rebaked.Mesh, rebaked.BoneTexture }, parts, "no Vertex textures left");
            Assert.AreEqual(VatBaker.BoneShaderName, _profile.Material.shader.name, "the template plays Bone assets");
            Assert.AreEqual(rebaked.BoneTexture, _profile.Material.GetTexture(VatShaderIds.BoneTexture), "the template has the bone texture");
            Assert.AreEqual(rebaked.Clips[0].Frame(0.0), _profile.Material.GetVector(VatShaderIds.Frame), "the template shows frame 0");
            Assert.IsFalse(rebaked.BoneTexture.isReadable, "the bone texture is not readable");
        }

        [Test]
        public void BoneAsset_ApplyFrame_WritesNoDrift()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);
            _result = VatBakePipeline.Run(Profile(_rig.Renderer, _rig.Clip, Fps, true), "Apply");
            var asset = ScriptableObject.CreateInstance<VatAsset>();
            var material = new Material(Shader.Find(VatBaker.BoneShaderName));
            try
            {
                asset.SetBoneData(_result.Mode, _result.Layout.Info, _result.Mesh, _result.ExtraMeshes, _result.Textures.Bone, _result.Layout.Clips,
                    _result.Precision, string.Empty);
                asset.ApplyTo(material, 0);
                var frame = _result.Layout.Clips[0].Frame(3.5);
                asset.ApplyFrame(material, frame);

                Assert.IsFalse(asset.HasDrift);
                Assert.AreEqual(_result.Textures.Bone, material.GetTexture(VatShaderIds.BoneTexture));
                Assert.AreEqual(frame, material.GetVector(VatShaderIds.Frame));
                Assert.IsFalse(material.HasVector(VatShaderIds.Drift), "the bone shader has no drift");
                Assert.AreEqual(_rig.Renderer.bones.Length, asset.BoneCount);
            }
            finally
            {
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(asset);
            }
        }

        private VatBakeProfile Profile(SkinnedMeshRenderer renderer, AnimationClip clip, float fps, bool isLooping)
        {
            _profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            _profile.Source = renderer;
            _profile.SetClips(clip);
            _profile.Fps = fps;
            _profile.IsLooping = isLooping;
            _profile.Mode = VatMode.Bone;
            _profile.Shader = Shader.Find(VatBaker.BoneShaderName);
            return _profile;
        }

        private Dictionary<SkinQuality, float> Reconstruct(GameObject root, SkinnedMeshRenderer renderer, AnimationClip clip, out float angle)
        {
            var errors = new Dictionary<SkinQuality, float> { [SkinQuality.Bone1] = 0f, [SkinQuality.Bone2] = 0f, [SkinQuality.Bone4] = 0f };
            var baked = _result.Layout.Clips[0];
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var boneUvs = VatBoneDecoder.BoneUvs(_result.Mesh);
            var rest = _result.Mesh.vertices;
            var restNormals = _result.Mesh.normals;
            angle = 0f;
            for (var k = 0; k < baked.FrameCount; k++)
            {
                var frame = VatTestUtil.Row(baked.StartRow + k);
                foreach (var quality in errors.Keys.ToArray())
                {
                    var reference = VatTestUtil.BakeMesh(root, renderer, clip, baked.FrameTime(k), quality, out var normals);
                    for (var v = 0; v < rest.Length; v++)
                    {
                        errors[quality] = Mathf.Max(errors[quality], Vector3.Distance(decoder.Position(boneUvs[v], rest[v], frame), reference[v]));
                        if (quality == SkinQuality.Bone2)
                        {
                            angle = Mathf.Max(angle, Vector3.Angle(decoder.Direction(boneUvs[v], restNormals[v], frame), normals[v]));
                        }
                    }
                }
            }

            return errors;
        }

        private static BoneWeight Weight(int bone0, float weight0, int bone1 = 0, float weight1 = 0f, int bone2 = 0, float weight2 = 0f)
        {
            return new BoneWeight { boneIndex0 = bone0, weight0 = weight0, boneIndex1 = bone1, weight1 = weight1, boneIndex2 = bone2, weight2 = weight2 };
        }

        private static Vector2Int Bones(Vector3Int influence)
        {
            return new Vector2Int(influence.x, influence.y);
        }

        private static Vector3Int Expected(BoneWeight weight)
        {
            var bone1 = weight.weight1 > 0f ? weight.boneIndex1 : weight.boneIndex0;
            var bits = Mathf.RoundToInt(weight.weight0 / (weight.weight0 + weight.weight1) * VatMath.BoneWeightMax);
            return new Vector3Int(weight.boneIndex0, bone1, bits);
        }

        private static void AssertContains(Bounds bounds, Vector3 point, string message)
        {
            Assert.IsTrue(Contains(bounds, point), $"{message}: {point} is outside {bounds}");
        }

        private static bool Contains(Bounds bounds, Vector3 point)
        {
            return bounds.SqrDistance(point) <= BoundsTolerance * BoundsTolerance;
        }

        private static long Id(Object target)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string _, out var id), $"{target} is not an asset");
            return id;
        }
    }
}
