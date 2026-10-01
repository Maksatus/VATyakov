using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    // Skinned strip with an analytic animation: references do not depend on the bake pipeline.
    // FBX-like hierarchy: Root → Model (−90° X, scale 0.01) → Mesh (SMR) and Bone0 → Bone1.
    // Two sub-meshes, the second with a non-zero baseVertex in the source.
    sealed class VatTestRig
    {
        public const float Length = 1f;

        static readonly Vector3 Bone1Rest = new Vector3(0f, 100f, 0f);

        public readonly GameObject Root;
        public readonly SkinnedMeshRenderer Renderer;
        public readonly AnimationClip Clip;
        readonly Transform _bone0;
        readonly Transform _bone1;

        public VatTestRig(Scene scene, int vertexCount, bool legacy)
        {
            Root = NewObject("RigRoot", scene, null);
            var model = NewObject("Model", scene, Root.transform).transform;
            model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            model.localScale = Vector3.one * 0.01f;
            _bone0 = NewObject("Bone0", scene, model).transform;
            _bone1 = NewObject("Bone1", scene, _bone0).transform;
            _bone1.localPosition = Bone1Rest;
            var meshObject = NewObject("Mesh", scene, model);

            var mesh = BuildStrip(vertexCount);
            mesh.bindposes = new[]
            {
                _bone0.worldToLocalMatrix * meshObject.transform.localToWorldMatrix,
                _bone1.worldToLocalMatrix * meshObject.transform.localToWorldMatrix,
            };
            Renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            Renderer.sharedMesh = mesh;
            Renderer.bones = new[] { _bone0, _bone1 };
            Renderer.rootBone = _bone0;

            Clip = BuildClip(legacy);
            if (!legacy)
                Root.AddComponent<Animator>();
        }

        // Bone0 slides along Z, Bone1 along X, both linear.
        void Pose(double t)
        {
            _bone0.localPosition = new Vector3(0f, 0f, (float)(20.0 * t));
            _bone1.localPosition = new Vector3((float)(50.0 * t), Bone1Rest.y, 0f);
        }

        // Linear blend skinning of the analytic pose, root space.
        public Vector3[] ReferencePositions(double t)
        {
            Pose(t);
            var mesh = Renderer.sharedMesh;
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            var bindposes = mesh.bindposes;
            var bones = Renderer.bones;
            var skin = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++)
                skin[i] = Root.transform.worldToLocalMatrix * bones[i].localToWorldMatrix * bindposes[i];

            var result = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                var w = weights[v];
                result[v] = w.weight0 * skin[w.boneIndex0].MultiplyPoint3x4(vertices[v])
                            + w.weight1 * skin[w.boneIndex1].MultiplyPoint3x4(vertices[v]);
            }

            Pose(0);
            return result;
        }

        static Mesh BuildStrip(int vertexCount)
        {
            Assert.That(vertexCount % 2 == 0 && vertexCount >= 8, "even vertex count, at least 4 rows");
            int rows = vertexCount / 2;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var tangents = new Vector4[vertexCount];
            var uv = new Vector2[vertexCount];
            var weights = new BoneWeight[vertexCount];
            for (int r = 0; r < rows; r++)
            {
                float along = r / (float)(rows - 1);
                for (int c = 0; c < 2; c++)
                {
                    int v = r * 2 + c;
                    vertices[v] = new Vector3(c == 0 ? -10f : 10f, along * 200f, 0f);
                    normals[v] = Vector3.back;
                    tangents[v] = new Vector4(1f, 0f, 0f, 1f);
                    uv[v] = new Vector2(c, along);
                    float w1 = along;
                    weights[v] = w1 > 0.5f
                        ? new BoneWeight { boneIndex0 = 1, weight0 = w1, boneIndex1 = 0, weight1 = 1f - w1 }
                        : new BoneWeight { boneIndex0 = 0, weight0 = 1f - w1, boneIndex1 = 1, weight1 = w1 };
                }
            }

            var mesh = new Mesh { name = "VatTestStrip", vertices = vertices, normals = normals, tangents = tangents, uv = uv };
            mesh.boneWeights = weights;

            // Triangles of the lower half in sub-mesh 0 (absolute indices), the upper half in sub-mesh 1 with baseVertex.
            int split = rows / 2;
            var lower = Enumerable.Range(0, split - 1).SelectMany(r => Quad(r * 2)).ToArray();
            int baseVertex = split * 2 - 2;
            var upper = Enumerable.Range(split - 1, rows - split).SelectMany(r => Quad(r * 2).Select(i => i - baseVertex)).ToArray();
            mesh.subMeshCount = 2;
            mesh.SetIndices(lower, MeshTopology.Triangles, 0);
            mesh.SetIndices(upper, MeshTopology.Triangles, 1, true, baseVertex);
            return mesh;
        }

        static int[] Quad(int i) => new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 };

        AnimationClip BuildClip(bool legacy)
        {
            var clip = new AnimationClip { name = legacy ? "RigLegacy" : "RigGeneric", legacy = legacy };
            string bone0 = AnimationUtility.CalculateTransformPath(_bone0, Root.transform);
            string bone1 = AnimationUtility.CalculateTransformPath(_bone1, Root.transform);
            Set(clip, bone0, "x", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "y", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "z", AnimationCurve.Linear(0f, 0f, Length, 20f * Length));
            Set(clip, bone1, "x", AnimationCurve.Linear(0f, 0f, Length, 50f * Length));
            Set(clip, bone1, "y", AnimationCurve.Constant(0f, Length, Bone1Rest.y));
            Set(clip, bone1, "z", AnimationCurve.Constant(0f, Length, 0f));
            return clip;
        }

        static void Set(AnimationClip clip, string path, string axis, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axis), curve);
        }

        static GameObject NewObject(string name, Scene scene, Transform parent)
        {
            // Hidden objects created straight into the preview scene never dirty the user's scene.
            var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            if (parent != null)
                go.transform.SetParent(parent, false);
            else
                SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Renderer.sharedMesh);
            Object.DestroyImmediate(Clip);
            Object.DestroyImmediate(Root);
        }
    }
}
