using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    internal sealed class VatSwingRig
    {
        public const float Length = 1f;
        public const float FromDegrees = -30f;
        public const float ToDegrees = 90f;

        private static readonly Vector3[] _vertices =
        {
            new(1f, 0f, 0f),
            new(1f, 0f, 0.1f),
            new(1.2f, 0f, 0f),
            new(1.2f, 0f, 0.1f),
        };

        private static readonly int[] _triangles = { 0, 2, 1, 1, 2, 3 };

        public readonly GameObject Root;
        public readonly SkinnedMeshRenderer Renderer;
        public readonly AnimationClip Clip;

        public VatSwingRig(Scene scene)
        {
            Root = EditorUtility.CreateGameObjectWithHideFlags("SwingRoot", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(Root, scene);
            var bone = NewChild("Bone").transform;
            var meshObject = NewChild("Mesh");
            var mesh = new Mesh { name = "VatSwing", vertices = _vertices, triangles = _triangles };
            mesh.RecalculateNormals();
            mesh.boneWeights = Array.ConvertAll(_vertices, _ => new BoneWeight { boneIndex0 = 0, weight0 = 1f });
            mesh.bindposes = new[] { bone.worldToLocalMatrix * meshObject.transform.localToWorldMatrix };
            Renderer = meshObject.AddComponent<SkinnedMeshRenderer>();
            Renderer.sharedMesh = mesh;
            Renderer.bones = new[] { bone };
            Renderer.rootBone = bone;
            Clip = BuildClip(AnimationUtility.CalculateTransformPath(bone, Root.transform));
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

        private static AnimationClip BuildClip(string path)
        {
            var clip = new AnimationClip { name = "Swing", legacy = true };
            var from = Quaternion.Euler(0f, 0f, FromDegrees);
            var to = Quaternion.Euler(0f, 0f, ToDegrees);
            var axes = new[] { "x", "y", "z", "w" };
            for (var c = 0; c < axes.Length; c++)
            {
                var curve = AnimationCurve.Linear(0f, from[c], Length, to[c]);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), $"m_LocalRotation.{axes[c]}"), curve);
            }

            return clip;
        }
    }
}
