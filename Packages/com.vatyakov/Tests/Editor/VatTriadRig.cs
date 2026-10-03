using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    internal sealed class VatTriadRig
    {
        public const int BoneCount = 3;
        public const float Length = 1f;
        public const int VerticesPerCase = 3;

        private const float Step = 0.3f;
        private const float TurnDegrees = 40f;
        private const float CaseSpacing = 0.18f;
        private const float NearestVertex = 0.2f;
        private const float SlotSpacing = 0.1f;

        private static readonly string[] _axes = { "x", "y", "z" };
        private static readonly string[] _quaternionAxes = { "x", "y", "z", "w" };
        private static readonly Vector3[] _directions = { Vector3.right, Vector3.up, Vector3.forward };

        public readonly GameObject Root;
        public readonly SkinnedMeshRenderer Renderer;
        public readonly AnimationClip Clip;

        public VatTriadRig(Scene scene, params BoneWeight[] cases)
        {
            Root = EditorUtility.CreateGameObjectWithHideFlags("TriadRoot", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(Root, scene);
            var bones = new Transform[BoneCount];
            for (var bone = 0; bone < BoneCount; bone++)
            {
                bones[bone] = NewChild($"Bone{bone}").transform;
            }

            var meshObject = NewChild("Mesh");
            var mesh = BuildMesh(cases);
            mesh.bindposes = Array.ConvertAll(bones, bone => bone.worldToLocalMatrix * meshObject.transform.localToWorldMatrix);
            Renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            Renderer.sharedMesh = mesh;
            Renderer.bones = bones;
            Renderer.rootBone = bones[0];
            Clip = BuildClip(bones);
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Renderer.sharedMesh);
            Object.DestroyImmediate(Clip);
            Object.DestroyImmediate(Root);
        }

        private GameObject NewChild(string name)
        {
            var child = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            child.transform.SetParent(Root.transform, false);
            return child;
        }

        private static Mesh BuildMesh(BoneWeight[] cases)
        {
            var count = cases.Length * VerticesPerCase;
            var vertices = new Vector3[count];
            var weights = new BoneWeight[count];
            var triangles = new int[count];
            for (var vertex = 0; vertex < count; vertex++)
            {
                var slot = vertex % VerticesPerCase;
                vertices[vertex] = new Vector3(NearestVertex + CaseSpacing * (vertex / VerticesPerCase), SlotSpacing * slot, SlotSpacing * slot * slot);
                weights[vertex] = cases[vertex / VerticesPerCase];
                triangles[vertex] = vertex;
            }

            var mesh = new Mesh { name = "VatTriad", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.boneWeights = weights;
            return mesh;
        }

        private AnimationClip BuildClip(Transform[] bones)
        {
            var clip = new AnimationClip { name = "Triad", legacy = true };
            for (var bone = 0; bone < BoneCount; bone++)
            {
                var path = AnimationUtility.CalculateTransformPath(bones[bone], Root.transform);
                for (var axis = 0; axis < _axes.Length; axis++)
                {
                    var distance = axis == bone ? Step * (bone + 1) : 0f;
                    var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), $"m_LocalPosition.{_axes[axis]}");
                    AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, 0f, Length, distance));
                }

                SetTurn(clip, path, Quaternion.AngleAxis(TurnDegrees * (bone + 1), _directions[bone]));
            }

            return clip;
        }

        private static void SetTurn(AnimationClip clip, string path, Quaternion turn)
        {
            for (var component = 0; component < _quaternionAxes.Length; component++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), $"m_LocalRotation.{_quaternionAxes[component]}");
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Linear(0f, Quaternion.identity[component], Length, turn[component]));
            }
        }
    }
}
