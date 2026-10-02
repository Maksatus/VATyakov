using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    internal sealed class VatTestRig
    {
        public const float Length = 1f;
        private const float TwistDegrees = 90f;
        private const int TwistKeysPerSecond = 60;

        private static readonly Vector3 _bone1Rest = new(0f, 100f, 0f);

        public readonly GameObject Root;
        public readonly SkinnedMeshRenderer Renderer;
        public readonly AnimationClip Clip;
        private readonly Transform _bone0;
        private readonly Transform _bone1;
        private readonly bool _twist;
        private readonly bool _blendShape;
        private readonly bool _legacy;
        private readonly List<AnimationClip> _partialClips = new();

        public VatTestRig(Scene scene, int vertexCount, bool legacy, bool loopingClip = false, bool twist = false, bool blendShape = false)
        {
            Assert.That(!twist || legacy, "the twist is keyed for legacy clips only");
            _twist = twist;
            _blendShape = blendShape;
            _legacy = legacy;
            Root = NewObject("RigRoot", scene, null);
            var model = NewObject("Model", scene, Root.transform).transform;
            model.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            model.localScale = Vector3.one * 0.01f;
            _bone0 = NewObject("Bone0", scene, model).transform;
            _bone1 = NewObject("Bone1", scene, _bone0).transform;
            _bone1.localPosition = _bone1Rest;
            var meshObject = NewObject("Mesh", scene, model);

            var mesh = BuildStrip(vertexCount);
            if (blendShape)
            {
                AddBulge(mesh);
            }

            mesh.bindposes = new[]
            {
                _bone0.worldToLocalMatrix * meshObject.transform.localToWorldMatrix,
                _bone1.worldToLocalMatrix * meshObject.transform.localToWorldMatrix,
            };
            Renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            Renderer.sharedMesh = mesh;
            Renderer.bones = new[] { _bone0, _bone1 };
            Renderer.rootBone = _bone0;

            Clip = BuildClip(legacy, loopingClip);
            if (!legacy)
            {
                Root.AddComponent<Animator>();
            }
        }

        public Vector3[] ReferencePositions(double t)
        {
            return Skin(t, Renderer.sharedMesh.vertices, (m, p) => m.MultiplyPoint3x4(p));
        }

        public Vector3[] ReferenceNormals(double t)
        {
            return Normalized(Skin(t, Renderer.sharedMesh.normals, (m, n) => m.MultiplyVector(n)));
        }

        public Vector3[] ReferenceTangents(double t)
        {
            return Normalized(Skin(t, Array.ConvertAll(Renderer.sharedMesh.tangents, x => (Vector3)x), (m, d) => m.MultiplyVector(d)));
        }

        public AnimationClip BuildPartialClip(string name)
        {
            var clip = new AnimationClip { name = name, legacy = _legacy };
            var bone0 = AnimationUtility.CalculateTransformPath(_bone0, Root.transform);
            Set(clip, bone0, "x", AnimationCurve.Linear(0f, 0f, Length, -30f * Length));
            Set(clip, bone0, "y", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "z", AnimationCurve.Constant(0f, Length, 0f));
            _partialClips.Add(clip);
            return clip;
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Renderer.sharedMesh);
            Object.DestroyImmediate(Clip);
            foreach (var clip in _partialClips)
            {
                Object.DestroyImmediate(clip);
            }

            Object.DestroyImmediate(Root);
        }

        private void Pose(double t)
        {
            _bone0.localPosition = new Vector3(0f, 0f, (float)(20.0 * t));
            _bone1.localPosition = new Vector3((float)(50.0 * t), _bone1Rest.y, 0f);
            _bone1.localRotation = Twist(t);
        }

        private Quaternion Twist(double t)
        {
            return _twist ? Quaternion.Euler(0f, (float)(TwistDegrees * t), 0f) : Quaternion.identity;
        }

        private Vector3[] Skin(double t, Vector3[] values, Func<Matrix4x4, Vector3, Vector3> transform)
        {
            Pose(t);
            var mesh = Renderer.sharedMesh;
            var weights = mesh.boneWeights;
            var bindposes = mesh.bindposes;
            var bones = Renderer.bones;
            var skin = new Matrix4x4[bones.Length];
            for (var i = 0; i < bones.Length; i++)
            {
                skin[i] = Root.transform.worldToLocalMatrix * bones[i].localToWorldMatrix * bindposes[i];
            }

            var result = new Vector3[values.Length];
            for (var v = 0; v < values.Length; v++)
            {
                var w = weights[v];
                result[v] = w.weight0 * transform(skin[w.boneIndex0], values[v]) + w.weight1 * transform(skin[w.boneIndex1], values[v]);
            }

            Pose(0);
            return result;
        }

        private static Vector3[] Normalized(Vector3[] values)
        {
            return Array.ConvertAll(values, v => v.normalized);
        }

        private static Mesh BuildStrip(int vertexCount)
        {
            Assert.That(vertexCount % 2 == 0 && vertexCount >= 8, "even vertex count, at least 4 rows");
            var rows = vertexCount / 2;
            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var tangents = new Vector4[vertexCount];
            var uv = new Vector2[vertexCount];
            var weights = new BoneWeight[vertexCount];
            for (var r = 0; r < rows; r++)
            {
                var along = r / (float)(rows - 1);
                for (var c = 0; c < 2; c++)
                {
                    var v = r * 2 + c;
                    vertices[v] = new Vector3(c == 0 ? -10f : 10f, along * 200f, 0f);
                    normals[v] = Vector3.back;
                    tangents[v] = new Vector4(1f, 0f, 0f, 1f);
                    uv[v] = new Vector2(c, along);
                    var w1 = along;
                    weights[v] = w1 > 0.5f
                        ? new BoneWeight { boneIndex0 = 1, weight0 = w1, boneIndex1 = 0, weight1 = 1f - w1 }
                        : new BoneWeight { boneIndex0 = 0, weight0 = 1f - w1, boneIndex1 = 1, weight1 = w1 };
                }
            }

            var mesh = new Mesh { name = "VatTestStrip", vertices = vertices, normals = normals, tangents = tangents, uv = uv };
            mesh.boneWeights = weights;

            var split = rows / 2;
            var lower = Enumerable.Range(0, split - 1).SelectMany(r => Quad(r * 2)).ToArray();
            var baseVertex = split * 2 - 2;
            var upper = Enumerable.Range(split - 1, rows - split).SelectMany(r => Quad(r * 2).Select(i => i - baseVertex)).ToArray();
            mesh.subMeshCount = 2;
            mesh.SetIndices(lower, MeshTopology.Triangles, 0);
            mesh.SetIndices(upper, MeshTopology.Triangles, 1, true, baseVertex);
            return mesh;
        }

        private static int[] Quad(int i)
        {
            return new[] { i, i + 2, i + 1, i + 1, i + 2, i + 3 };
        }

        private AnimationClip BuildClip(bool legacy, bool looping)
        {
            var clip = new AnimationClip { name = legacy ? "RigLegacy" : "RigGeneric", legacy = legacy };
            if (legacy && looping)
            {
                clip.wrapMode = WrapMode.Loop;
            }

            var bone0 = AnimationUtility.CalculateTransformPath(_bone0, Root.transform);
            var bone1 = AnimationUtility.CalculateTransformPath(_bone1, Root.transform);
            Set(clip, bone0, "x", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "y", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "z", AnimationCurve.Linear(0f, 0f, Length, 20f * Length));
            Set(clip, bone1, "x", AnimationCurve.Linear(0f, 0f, Length, 50f * Length));
            Set(clip, bone1, "y", AnimationCurve.Constant(0f, Length, _bone1Rest.y));
            Set(clip, bone1, "z", AnimationCurve.Constant(0f, Length, 0f));
            if (!legacy && looping)
            {
                SetLoopTime(clip);
            }

            if (_twist)
            {
                SetTwist(clip, bone1);
            }

            if (_blendShape)
            {
                SetBulge(clip);
            }

            return clip;
        }

        private static void AddBulge(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var deltas = new Vector3[vertices.Length];
            for (var v = 0; v < deltas.Length; v++)
            {
                deltas[v] = new Vector3(0f, 0f, -30f * vertices[v].y / 200f);
            }

            mesh.AddBlendShapeFrame("Bulge", 100f, deltas, null, null);
        }

        private void SetBulge(AnimationClip clip)
        {
            var path = AnimationUtility.CalculateTransformPath(Renderer.transform, Root.transform);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape.Bulge"),
                AnimationCurve.Linear(0f, 0f, Length, 100f));
        }

        private void SetTwist(AnimationClip clip, string path)
        {
            var count = Mathf.RoundToInt(Length * TwistKeysPerSecond) + 1;
            var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
            for (var i = 0; i < count; i++)
            {
                var time = i / (float)TwistKeysPerSecond;
                var q = Twist(time);
                for (var c = 0; c < 4; c++)
                {
                    curves[c].AddKey(new Keyframe(time, q[c]));
                }
            }

            var axes = new[] { "x", "y", "z", "w" };
            for (var c = 0; c < 4; c++)
            {
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalRotation." + axes[c]), curves[c]);
            }
        }

        private static void SetLoopTime(AnimationClip clip)
        {
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
        }

        private static void Set(AnimationClip clip, string path, string axis, AnimationCurve curve)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axis), curve);
        }

        private static GameObject NewObject(string name, Scene scene, Transform parent)
        {
            var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            else
            {
                SceneManager.MoveGameObjectToScene(go, scene);
            }

            return go;
        }
    }
}
