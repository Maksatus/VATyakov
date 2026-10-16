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
    public class VatSourceTests
    {
        private const float Fps = 30f;
        private const int VertexCount = 64;
        private const float Tolerance = 5e-4f;
        private const float MinMotion = 0.1f;
        private const float TwinShift = 0.5f;

        private static readonly Vector3 _twinOffset = new(50f, 0f, 0f);

        private Scene _scene;
        private VatTestRig _rig;
        private VatBakeProfile _profile;
        private VatBakeResult _result;
        private GameObject _wrapper;

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

            if (_profile != null)
            {
                Object.DestroyImmediate(_profile);
            }

            _rig?.Destroy();
            if (_wrapper != null)
            {
                Object.DestroyImmediate(_wrapper);
            }

            _result = null;
            _profile = null;
            _rig = null;
            _wrapper = null;
            EditorSceneManager.ClosePreviewScene(_scene);
        }

        [Test]
        public void MirroredModel_KeepsTheFacesTurnedOut([Values(VatMode.Vertex, VatMode.Bone)] VatMode mode)
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);
            var model = _rig.Root.transform.Find("Model");
            model.localScale = Vector3.Scale(model.localScale, new Vector3(-1f, 1f, 1f));

            _result = VatBakePipeline.Run(Profile(mode), "Mirrored");

            Assert.AreEqual(mode, _result.Mode, "the bake keeps the mode");
            Assert.AreEqual(Facing(_rig.Renderer.sharedMesh), Facing(_result.Mesh), "every face turns to its normals as in the source mesh");
        }

        [Test]
        public void MirroredAlembicNode_KeepsTheFacesTurnedOut()
        {
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
            mesh.RecalculateNormals();
            _wrapper = EditorUtility.CreateGameObjectWithHideFlags("Alembic", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(_wrapper, _scene);
            try
            {
                var reader = new VatAlembicReader(new[] { AddNode("Plain", mesh, Vector3.one), AddNode("Mirrored", mesh, new Vector3(-1f, 1f, 1f)) });
                var source = reader.SourceMesh("Alembic");
                var frame = new VatFrame(source.VertexCount);
                reader.Read(frame);

                var indices = source.SubMeshes.SelectMany(subMesh => subMesh.Indices).ToArray();
                var facing = Facing(mesh)[0];
                CollectionAssert.AreEqual(new[] { facing, facing }, Facing(frame.Positions, frame.Normals, indices), "both nodes face their normals");
            }
            finally
            {
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void Clip_UnderAWrapperRootWithoutAnimator_Animates([Values] bool isLegacy)
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy);
            if (isLegacy)
            {
                _rig.Root.AddComponent<Animation>();
            }

            _wrapper = EditorUtility.CreateGameObjectWithHideFlags("Wrapper", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(_wrapper, _scene);
            _rig.Root.transform.SetParent(_wrapper.transform, false);

            var bake = new VatInMemoryBake(_rig, Fps);
            try
            {
                var texels = VatTestUtil.ReadGpu(bake.Position);
                var clip = bake.Layout.Clips[0];
                var last = clip.FrameCount - 1;
                var reference = _rig.ReferencePositions(clip.FrameTime(last));
                var maxError = 0f;
                for (var v = 0; v < reference.Length; v++)
                {
                    maxError = Mathf.Max(maxError, Vector3.Distance(bake.DecodePosition(texels, bake.Rest[v], v, clip.StartRow + last), reference[v]));
                }

                Assert.That(Vector3.Distance(reference[^1], bake.Rest[^1]), Is.GreaterThan(MinMotion), "the clip moves the strip");
                Assert.Less(maxError, bake.Precision.Error + Tolerance, "the last frame is the animated pose, not the rest pose");
            }
            finally
            {
                bake.Destroy();
            }
        }

        [Test]
        public void SameNamedSiblings_BakeTheChosenRenderer()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);
            var twin = Object.Instantiate(_rig.Renderer.gameObject, _rig.Renderer.transform.parent);
            twin.name = _rig.Renderer.name;
            var mesh = Object.Instantiate(_rig.Renderer.sharedMesh);
            mesh.vertices = mesh.vertices.Select(vertex => vertex + _twinOffset).ToArray();
            twin.GetComponent<SkinnedMeshRenderer>().sharedMesh = mesh;
            var bake = new VatInMemoryBake(new VatSkinnedFrameSource(twin.GetComponent<SkinnedMeshRenderer>(), new[] { _rig.Clip }), Fps);
            try
            {
                var reference = _rig.ReferencePositions(0.0);
                Assert.AreEqual(TwinShift, bake.Rest[0].x - reference[0].x, Tolerance, "the bake takes the twin, not the first renderer of that name");
            }
            finally
            {
                bake.Destroy();
                Object.DestroyImmediate(mesh);
            }
        }

        [Test]
        public void ClipThatMovesTheBody_WarnsAboutRootMotion()
        {
            _rig = new VatTestRig(_scene, VertexCount, isLegacy: false);
            var bake = new VatInMemoryBake(_rig, Fps, _rig.BuildPartialClip("Travel"));
            try
            {
                var warning = bake.Warnings.SingleOrDefault(text => text.Contains("root motion"));
                Assert.IsNotNull(warning, string.Join("\n", bake.Warnings));
                StringAssert.Contains("'Travel' moves the body", warning);
            }
            finally
            {
                bake.Destroy();
            }
        }

        private VatBakeProfile Profile(VatMode mode)
        {
            _profile = ScriptableObject.CreateInstance<VatBakeProfile>();
            _profile.Source = _rig.Renderer;
            _profile.SetClips(_rig.Clip);
            _profile.Fps = Fps;
            _profile.IsLooping = false;
            _profile.Mode = mode;
            return _profile;
        }

        private MeshFilter AddNode(string name, Mesh mesh, Vector3 scale)
        {
            var node = new GameObject(name);
            node.transform.SetParent(_wrapper.transform, false);
            node.transform.localScale = scale;
            node.AddComponent<MeshFilter>().sharedMesh = mesh;
            return node.GetComponent<MeshFilter>();
        }

        private static int[] Facing(Mesh mesh)
        {
            var indices = Enumerable.Range(0, mesh.subMeshCount).SelectMany(subMesh => mesh.GetIndices(subMesh, true)).ToArray();
            return Facing(mesh.vertices, mesh.normals, indices);
        }

        private static int[] Facing(Vector3[] vertices, Vector3[] normals, int[] indices)
        {
            var facing = new int[indices.Length / 3];
            for (var t = 0; t < facing.Length; t++)
            {
                var a = indices[t * 3];
                var b = indices[t * 3 + 1];
                var c = indices[t * 3 + 2];
                var face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                facing[t] = (int)Mathf.Sign(Vector3.Dot(face, normals[a] + normals[b] + normals[c]));
            }

            return facing;
        }
    }
}
