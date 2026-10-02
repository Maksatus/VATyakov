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
        public const float ModelPitch = -90f;
        private const float TwistDegrees = 90f;
        private const int TwistKeysPerSecond = 60;
        private const float ModelScale = 0.01f;
        private const float Bone0Speed = 20f;
        private const float Bone1Speed = 50f;
        private const float PartialBone0Speed = -30f;
        private const float StripHeight = 200f;
        private const float StripHalfWidth = 10f;
        private const float BulgeDepth = -30f;
        private const float BulgeWeight = 100f;

        private static readonly Vector3 _bone1Rest = new(0f, 100f, 0f);

        public readonly GameObject Root;
        public readonly SkinnedMeshRenderer Renderer;
        public readonly AnimationClip Clip;
        private readonly Transform _bone0;
        private readonly Transform _bone1;
        private readonly bool _hasTwist;
        private readonly bool _hasBlendShape;
        private readonly bool _isLegacy;
        private readonly List<AnimationClip> _partialClips = new();

        public VatTestRig(Scene scene, int vertexCount, bool isLegacy, bool isLoopingClip = false, bool hasTwist = false, bool hasBlendShape = false)
        {
            Assert.That(!hasTwist || isLegacy, "the twist is keyed for legacy clips only");
            _hasTwist = hasTwist;
            _hasBlendShape = hasBlendShape;
            _isLegacy = isLegacy;
            Root = NewObject("RigRoot", scene, null);
            var model = NewObject("Model", scene, Root.transform).transform;
            model.localRotation = Quaternion.Euler(ModelPitch, 0f, 0f);
            model.localScale = Vector3.one * ModelScale;
            _bone0 = NewObject("Bone0", scene, model).transform;
            _bone1 = NewObject("Bone1", scene, _bone0).transform;
            _bone1.localPosition = _bone1Rest;
            var meshObject = NewObject("Mesh", scene, model);

            var mesh = BuildStrip(vertexCount);
            if (hasBlendShape)
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

            Clip = BuildClip(isLegacy, isLoopingClip);
            if (!isLegacy)
            {
                Root.AddComponent<Animator>();
            }
        }

        public Vector3[] ReferencePositions(double time)
        {
            return Skin(time, Renderer.sharedMesh.vertices, (matrix, point) => matrix.MultiplyPoint3x4(point));
        }

        public Vector3[] ReferenceNormals(double time)
        {
            return Normalized(Skin(time, Renderer.sharedMesh.normals, (matrix, normal) => matrix.MultiplyVector(normal)));
        }

        public Vector3[] ReferenceTangents(double time)
        {
            var tangents = Array.ConvertAll(Renderer.sharedMesh.tangents, tangent => (Vector3)tangent);
            return Normalized(Skin(time, tangents, (matrix, direction) => matrix.MultiplyVector(direction)));
        }

        public AnimationClip BuildPartialClip(string name)
        {
            var clip = new AnimationClip { name = name, legacy = _isLegacy };
            var bone0 = AnimationUtility.CalculateTransformPath(_bone0, Root.transform);
            Set(clip, bone0, "x", AnimationCurve.Linear(0f, 0f, Length, PartialBone0Speed * Length));
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

        private void Pose(double time)
        {
            _bone0.localPosition = new Vector3(0f, 0f, (float)(Bone0Speed * time));
            _bone1.localPosition = new Vector3((float)(Bone1Speed * time), _bone1Rest.y, 0f);
            _bone1.localRotation = Twist(time);
        }

        private Quaternion Twist(double time)
        {
            return _hasTwist ? Quaternion.Euler(0f, (float)(TwistDegrees * time), 0f) : Quaternion.identity;
        }

        private Vector3[] Skin(double time, Vector3[] values, Func<Matrix4x4, Vector3, Vector3> transform)
        {
            Pose(time);
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
                var weight = weights[v];
                result[v] = weight.weight0 * transform(skin[weight.boneIndex0], values[v]) + weight.weight1 * transform(skin[weight.boneIndex1], values[v]);
            }

            Pose(0);
            return result;
        }

        private static Vector3[] Normalized(Vector3[] values)
        {
            return Array.ConvertAll(values, value => value.normalized);
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
                    var vertex = r * 2 + c;
                    vertices[vertex] = new Vector3(c == 0 ? -StripHalfWidth : StripHalfWidth, along * StripHeight, 0f);
                    normals[vertex] = Vector3.back;
                    tangents[vertex] = new Vector4(1f, 0f, 0f, 1f);
                    uv[vertex] = new Vector2(c, along);
                    weights[vertex] = along > 0.5f
                        ? new BoneWeight { boneIndex0 = 1, weight0 = along, boneIndex1 = 0, weight1 = 1f - along }
                        : new BoneWeight { boneIndex0 = 0, weight0 = 1f - along, boneIndex1 = 1, weight1 = along };
                }
            }

            var mesh = new Mesh { name = "VatTestStrip", vertices = vertices, normals = normals, tangents = tangents, uv = uv };
            mesh.boneWeights = weights;

            var split = rows / 2;
            var lower = Enumerable.Range(0, split - 1).SelectMany(row => Quad(row * 2)).ToArray();
            var baseVertex = split * 2 - 2;
            var upper = Enumerable.Range(split - 1, rows - split).SelectMany(row => Quad(row * 2).Select(index => index - baseVertex)).ToArray();
            mesh.subMeshCount = 2;
            mesh.SetIndices(lower, MeshTopology.Triangles, 0);
            mesh.SetIndices(upper, MeshTopology.Triangles, 1, true, baseVertex);
            return mesh;
        }

        private static int[] Quad(int first)
        {
            return new[] { first, first + 2, first + 1, first + 1, first + 2, first + 3 };
        }

        private AnimationClip BuildClip(bool isLegacy, bool isLooping)
        {
            var clip = new AnimationClip { name = isLegacy ? "RigLegacy" : "RigGeneric", legacy = isLegacy };
            if (isLegacy && isLooping)
            {
                clip.wrapMode = WrapMode.Loop;
            }

            var bone0 = AnimationUtility.CalculateTransformPath(_bone0, Root.transform);
            var bone1 = AnimationUtility.CalculateTransformPath(_bone1, Root.transform);
            Set(clip, bone0, "x", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "y", AnimationCurve.Constant(0f, Length, 0f));
            Set(clip, bone0, "z", AnimationCurve.Linear(0f, 0f, Length, Bone0Speed * Length));
            Set(clip, bone1, "x", AnimationCurve.Linear(0f, 0f, Length, Bone1Speed * Length));
            Set(clip, bone1, "y", AnimationCurve.Constant(0f, Length, _bone1Rest.y));
            Set(clip, bone1, "z", AnimationCurve.Constant(0f, Length, 0f));
            if (!isLegacy && isLooping)
            {
                SetLoopTime(clip);
            }

            if (_hasTwist)
            {
                SetTwist(clip, bone1);
            }

            if (_hasBlendShape)
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
                deltas[v] = new Vector3(0f, 0f, BulgeDepth * vertices[v].y / StripHeight);
            }

            mesh.AddBlendShapeFrame("Bulge", BulgeWeight, deltas, null, null);
        }

        private void SetBulge(AnimationClip clip)
        {
            var path = AnimationUtility.CalculateTransformPath(Renderer.transform, Root.transform);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(SkinnedMeshRenderer), "blendShape.Bulge"),
                AnimationCurve.Linear(0f, 0f, Length, BulgeWeight));
        }

        private void SetTwist(AnimationClip clip, string path)
        {
            var count = Mathf.RoundToInt(Length * TwistKeysPerSecond) + 1;
            var curves = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
            for (var i = 0; i < count; i++)
            {
                var time = i / (float)TwistKeysPerSecond;
                var rotation = Twist(time);
                for (var c = 0; c < 4; c++)
                {
                    curves[c].AddKey(new Keyframe(time, rotation[c]));
                }
            }

            var axes = new[] { "x", "y", "z", "w" };
            for (var c = 0; c < 4; c++)
            {
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), $"m_LocalRotation.{axes[c]}"), curves[c]);
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
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), $"m_LocalPosition.{axis}"), curve);
        }

        private static GameObject NewObject(string name, Scene scene, Transform parent)
        {
            var target = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            if (parent != null)
            {
                target.transform.SetParent(parent, false);
            }
            else
            {
                SceneManager.MoveGameObjectToScene(target, scene);
            }

            return target;
        }
    }
}
