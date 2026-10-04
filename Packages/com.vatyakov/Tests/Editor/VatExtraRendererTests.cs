using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatExtraRendererTests
    {
        private const string TempFolder = "Assets/__VatExtraRendererTestTemp";
        private const float Fps = 30f;
        private const int VertexCount = 64;
        private const float Millimeter = 1e-3f;
        private const float AngleTolerance = 1f;
        private const float BoundsTolerance = 1e-5f;
        private const float BindposeShift = 0.01f;
        private const float EquipmentScale = 0.5f;

        private static readonly int[] _shuffledBones = { 2, 0, 1 };
        private static readonly Vector3 _lodOffset = new(0.5f, -0.25f, 2f);
        private static readonly Vector3 _gripOffset = new(0.05f, 0.1f, -0.02f);
        private static readonly Vector3 _equipmentOffset = new(0.2f, 0.05f, 0.1f);
        private static readonly Vector3 _equipmentTurn = new(30f, -45f, 60f);
        private static readonly Vector3 _nonUniformScale = new(2f, 1f, 0.5f);
        private static readonly Vector3 _equipmentShift = new(0.1f, -0.2f, 0.3f);

        private readonly List<Object> _created = new();

        private Scene _scene;
        private VatTriadRig _triad;
        private VatTestRig _rig;
        private VatBakeProfile _profile;
        private VatBakeResult _result;
        private Material _material;

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
                Array.ForEach(_result.ExtraMeshes, Object.DestroyImmediate);
                _result.Textures.Destroy();
            }

            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _triad?.Destroy();
            _rig?.Destroy();
            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _created.Clear();
            _result = null;
            _triad = null;
            _rig = null;
            _profile = null;
            _material = null;
            EditorSceneManager.ClosePreviewScene(_scene);
            AssetDatabase.DeleteAsset(TempFolder);
        }

        [Test]
        public void ExtraSkinnedMesh_WithShuffledBones_MatchesBakeMeshWithTwoBones()
        {
            _triad = Triad();
            var lod = ShuffledCopy(_triad.Renderer, _shuffledBones);

            _result = VatBakePipeline.Run(Profile(_triad.Renderer, _triad.Clip, lod), "Lod");

            Assert.AreEqual(VatMode.Bone, _result.Mode, _result.Fallback);
            Assert.AreEqual(1, _result.ExtraMeshes.Length, "one mesh per extra renderer");
            CollectionAssert.AreEqual(VatBoneDecoder.BoneUvs(_result.Mesh).Select(VatBoneDecoder.Influence),
                VatBoneDecoder.BoneUvs(_result.ExtraMeshes[0]).Select(VatBoneDecoder.Influence), "bone indices of the LOD are bones of the rig");
            var error = MaxSkinError(_result.ExtraMeshes[0], lod, out var angle);
            Assert.Less(error, Millimeter, "positions of the LOD against its BakeMesh at Skin Weights = 2 Bones");
            Assert.Less(angle, AngleTolerance, "normals of the LOD against its BakeMesh");
            Assert.Less(MaxSkinError(_result.Mesh, _triad.Renderer, out _), Millimeter, "the main mesh still matches its BakeMesh");
        }

        [Test]
        public void MeshRendererUnderABone_FollowsThatBone()
        {
            _triad = Triad();
            var equipment = Equipment(_triad.Root.transform.Find("Bone2"));

            _result = VatBakePipeline.Run(Profile(_triad.Renderer, _triad.Clip, equipment), "Equipment");

            Assert.AreEqual(VatMode.Bone, _result.Mode, _result.Fallback);
            var mesh = _result.ExtraMeshes[0];
            var boneUvs = VatBoneDecoder.BoneUvs(mesh);
            Assert.That(boneUvs.Select(VatBoneDecoder.Influence), Is.All.EqualTo(new Vector3Int(2, 2, (int)VatMath.BoneWeightMax)), "100% on Bone2");
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var rest = mesh.vertices;
            var restNormals = mesh.normals;
            var source = equipment.GetComponent<MeshFilter>().sharedMesh;
            var clip = _result.Layout.Clips[0];
            var error = 0f;
            var angle = 0f;
            for (var k = 0; k < clip.FrameCount; k++)
            {
                _triad.Clip.SampleAnimation(_triad.Root, (float)clip.FrameTime(k));
                var toRoot = _triad.Root.transform.worldToLocalMatrix * equipment.transform.localToWorldMatrix;
                var frame = VatTestUtil.Row(clip.StartRow + k);
                for (var v = 0; v < rest.Length; v++)
                {
                    var point = decoder.Position(boneUvs[v], rest[v], frame);
                    error = Mathf.Max(error, Vector3.Distance(point, toRoot.MultiplyPoint3x4(source.vertices[v])));
                    var normal = toRoot.inverse.transpose.MultiplyVector(source.normals[v]);
                    angle = Mathf.Max(angle, Vector3.Angle(decoder.Direction(boneUvs[v], restNormals[v], frame), normal));
                    Assert.LessOrEqual(mesh.bounds.SqrDistance(point), BoundsTolerance * BoundsTolerance, $"vertex {v} of frame {k} is inside the bounds");
                }
            }

            Assert.Less(error, Millimeter, "the equipment stays where its bone carries it");
            Assert.Less(angle, AngleTolerance, "normals of the equipment");
        }

        [Test]
        public void ExtraRenderers_OffTheRig_AreProblems()
        {
            _triad = Triad();
            var bindpose = ShuffledCopy(_triad.Renderer, _shuffledBones);
            var poses = bindpose.sharedMesh.bindposes;
            poses[0] = Matrix4x4.Translate(Vector3.one * BindposeShift) * poses[0];
            bindpose.sharedMesh.bindposes = poses;
            var foreignBone = ShuffledCopy(_triad.Renderer, _shuffledBones);
            var bones = foreignBone.bones;
            bones[1] = New("Foreign", _triad.Root.transform).transform;
            foreignBone.bones = bones;
            var loose = Equipment(_triad.Root.transform);
            var stranger = Equipment(New("Stranger", null).transform);
            var profile = Profile(_triad.Renderer, _triad.Clip, bindpose, foreignBone, loose, stranger, null);

            var problems = VatBaker.Validate(profile);

            AssertProblem(problems, $"Bone 'Bone2' of '{bindpose.name}' has another bindpose than in '{_triad.Renderer.name}'");
            AssertProblem(problems, $"Bone 'Foreign' of '{foreignBone.name}' is not a bone of '{_triad.Renderer.name}'");
            AssertProblem(problems, $"'{loose.name}' is not under a bone of '{_triad.Renderer.name}'");
            AssertProblem(problems, $"'{stranger.name}' is not in the hierarchy of '{_triad.Renderer.name}'");
            AssertProblem(problems, "An Extra Renderer is not set");
            profile.SetExtraRenderers(new VatExtraRenderer { Renderer = Equipment(_triad.Root.transform.Find("Bone1")) });
            profile.Mode = VatMode.Vertex;
            CollectionAssert.AreEqual(new[] { "Extra Renderers bake only in Mode = Bone." }, VatBaker.Validate(profile));
        }

        [Test]
        public void ExtraRenderers_WithANonUniformlyScaledBone_FailTheBake()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: true);
            var bone1 = _rig.Root.transform.Find("Model/Bone0/Bone1");
            bone1.localScale = _nonUniformScale;
            var profile = Profile(_rig.Renderer, _rig.Clip, Equipment(bone1));

            var exception = Assert.Throws<VatBakeException>(() => VatBakePipeline.Run(profile, "Scaled"));

            StringAssert.Contains("bone 'Bone1' scales non-uniformly", exception.Message);
            StringAssert.Contains("Extra Renderers bake only in Bone mode", exception.Message);
        }

        [Test]
        public void Rebake_KeepsTheExtraMeshes_WritesTheExtraMaterial_RemovesDroppedMeshes()
        {
            AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
            _triad = Triad();
            var equipment = Equipment(_triad.Root.transform.Find("Bone1"));
            var lod = ShuffledCopy(_triad.Renderer, _shuffledBones);
            _material = new Material(Shader.Find(VatBaker.DefaultShaderName));
            _created.Add(_material);
            var profile = Profile(_triad.Renderer, _triad.Clip, equipment, lod);
            profile.SetExtraRenderers(new VatExtraRenderer { Renderer = equipment, Material = _material }, new VatExtraRenderer { Renderer = lod });
            var asset = VatBaker.Bake(profile, $"{TempFolder}/extra.asset");
            var equipmentId = Id(asset.ExtraMeshes[0]);

            var rebaked = VatBaker.Bake(profile);

            Assert.AreSame(asset, rebaked, "the rebake updates the same asset");
            Assert.AreEqual("extra_mesh_equipment", rebaked.ExtraMeshes[0].name);
            Assert.AreEqual(equipmentId, Id(rebaked.ExtraMeshes[0]), "the extra mesh keeps its file id");
            Assert.IsFalse(VatSourceHash.IsOutdated(profile, rebaked), "the bake is fresh");
            Assert.AreEqual(VatBaker.BoneShaderName, _material.shader.name, "the extra material plays Bone assets");
            Assert.AreEqual(rebaked.BoneTexture, _material.GetTexture(VatShaderIds.BoneTexture), "the extra material has the bone texture");
            Assert.AreEqual(rebaked.Layout.ShaderLayout, _material.GetVector(VatShaderIds.Layout), "the extra material has the layout");
            profile.SetExtraRenderers(new VatExtraRenderer { Renderer = equipment, Material = _material });
            Assert.IsTrue(VatSourceHash.IsOutdated(profile, rebaked), "a dropped renderer makes the bake outdated");

            VatBaker.Bake(profile);

            var parts = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(rebaked));
            CollectionAssert.AreEquivalent(new Object[] { rebaked.Mesh, rebaked.ExtraMeshes[0], rebaked.BoneTexture }, parts, "the dropped mesh is removed");
        }

        [Test]
        public void Rebake_OfAMovedEquipment_PutsTheNewVerticesOnTheGpu()
        {
            AssetDatabase.CreateFolder("Assets", TempFolder.Substring("Assets/".Length));
            _triad = Triad();
            var equipment = Equipment(_triad.Root.transform.Find("Bone1"));
            var profile = Profile(_triad.Renderer, _triad.Clip, equipment);
            var asset = VatBaker.Bake(profile, $"{TempFolder}/moved.asset");
            var before = GpuPositions(asset.ExtraMeshes[0]);
            equipment.transform.localPosition += _equipmentShift;

            var rebaked = VatBaker.Bake(profile);

            var rest = rebaked.ExtraMeshes[0].vertices;
            var gpu = GpuPositions(rebaked.ExtraMeshes[0]);
            Assert.That(Vector3.Distance(before[0], rest[0]), Is.GreaterThan(Millimeter), "the move changes the rest pose");
            for (var v = 0; v < rest.Length; v++)
            {
                Assert.That(Vector3.Distance(gpu[v], rest[v]), Is.LessThan(BoundsTolerance), $"vertex {v} on the GPU is the rebaked one");
            }
        }

        private VatTriadRig Triad()
        {
            return new VatTriadRig(_scene, Weight(0, 0.5f, 1, 0.5f), Weight(1, 0.7f, 2, 0.3f), Weight(2, 1f));
        }

        private VatBakeProfile Profile(SkinnedMeshRenderer renderer, AnimationClip clip, params Renderer[] extras)
        {
            _profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            _profile.Source = renderer;
            _profile.SetClips(clip);
            _profile.SetExtraRenderers(extras.Select(extra => new VatExtraRenderer { Renderer = extra }).ToArray());
            _profile.Fps = Fps;
            _profile.IsLooping = false;
            _profile.Mode = VatMode.Bone;
            _profile.Shader = Shader.Find(VatBaker.BoneShaderName);
            return _profile;
        }

        private SkinnedMeshRenderer ShuffledCopy(SkinnedMeshRenderer source, int[] order)
        {
            var mesh = Object.Instantiate(source.sharedMesh);
            _created.Add(mesh);
            var newIndex = new int[order.Length];
            for (var i = 0; i < order.Length; i++)
            {
                newIndex[order[i]] = i;
            }

            mesh.boneWeights = mesh.boneWeights.Select(weight => Remap(weight, newIndex)).ToArray();
            mesh.bindposes = order.Select(bone => source.sharedMesh.bindposes[bone]).ToArray();
            var lod = New("Lod", source.transform.parent);
            lod.transform.localPosition = _lodOffset;
            var renderer = lod.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = mesh;
            renderer.bones = order.Select(bone => source.bones[bone]).ToArray();
            renderer.rootBone = source.rootBone;
            return renderer;
        }

        private MeshRenderer Equipment(Transform bone)
        {
            var grip = New("Grip", bone);
            grip.transform.localPosition = _gripOffset;
            var equipment = New("Equipment", grip.transform);
            equipment.transform.SetLocalPositionAndRotation(_equipmentOffset, Quaternion.Euler(_equipmentTurn));
            equipment.transform.localScale = Vector3.one * EquipmentScale;
            var mesh = new Mesh
            {
                name = "VatEquipment",
                vertices = new[] { Vector3.zero, Vector3.right * 0.3f, Vector3.up * 0.4f, Vector3.forward * 0.2f },
                triangles = new[] { 0, 1, 2, 0, 2, 3, 0, 3, 1 },
            };
            mesh.RecalculateNormals();
            _created.Add(mesh);
            equipment.AddComponent<MeshFilter>().sharedMesh = mesh;
            return equipment.AddComponent<MeshRenderer>();
        }

        private GameObject New(string name, Transform parent)
        {
            var created = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            if (parent != null)
            {
                created.transform.SetParent(parent, false);
            }
            else
            {
                SceneManager.MoveGameObjectToScene(created, _scene);
                _created.Add(created);
            }

            return created;
        }

        private float MaxSkinError(Mesh mesh, SkinnedMeshRenderer renderer, out float angle)
        {
            var clip = _result.Layout.Clips[0];
            var decoder = new VatBoneDecoder(_result.Textures.Bone);
            var boneUvs = VatBoneDecoder.BoneUvs(mesh);
            var rest = mesh.vertices;
            var restNormals = mesh.normals;
            var error = 0f;
            angle = 0f;
            for (var k = 0; k < clip.FrameCount; k++)
            {
                var reference = VatTestUtil.BakeMesh(_triad.Root, renderer, _triad.Clip, clip.FrameTime(k), SkinQuality.Bone2, out var normals);
                var frame = VatTestUtil.Row(clip.StartRow + k);
                for (var v = 0; v < rest.Length; v++)
                {
                    error = Mathf.Max(error, Vector3.Distance(decoder.Position(boneUvs[v], rest[v], frame), reference[v]));
                    angle = Mathf.Max(angle, Vector3.Angle(decoder.Direction(boneUvs[v], restNormals[v], frame), normals[v]));
                }
            }

            return error;
        }

        private static BoneWeight Remap(BoneWeight weight, int[] newIndex)
        {
            weight.boneIndex0 = newIndex[weight.boneIndex0];
            weight.boneIndex1 = newIndex[weight.boneIndex1];
            weight.boneIndex2 = newIndex[weight.boneIndex2];
            weight.boneIndex3 = newIndex[weight.boneIndex3];
            return weight;
        }

        private static BoneWeight Weight(int bone0, float weight0, int bone1 = 0, float weight1 = 0f)
        {
            return new BoneWeight { boneIndex0 = bone0, weight0 = weight0, boneIndex1 = bone1, weight1 = weight1 };
        }

        private static void AssertProblem(List<string> problems, string expected)
        {
            Assert.That(problems.Any(problem => problem.StartsWith(expected, StringComparison.Ordinal)), $"'{expected}' in:\n{string.Join("\n", problems)}");
        }

        private static Vector3[] GpuPositions(Mesh mesh)
        {
            using var buffer = mesh.GetVertexBuffer(0);
            var stride = buffer.stride / sizeof(float);
            var data = new float[buffer.count * stride];
            buffer.GetData(data);
            var positions = new Vector3[buffer.count];
            for (var v = 0; v < positions.Length; v++)
            {
                positions[v] = new Vector3(data[v * stride], data[v * stride + 1], data[v * stride + 2]);
            }

            return positions;
        }

        private static long Id(Object target)
        {
            Assert.IsTrue(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string _, out var id), $"{target} is not an asset");
            return id;
        }
    }
}
