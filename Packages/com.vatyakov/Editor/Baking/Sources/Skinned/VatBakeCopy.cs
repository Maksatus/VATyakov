using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal sealed class VatBakeCopy : IDisposable
    {
        public const string BakeName = "VatBake";

        private Scene _scene;

        public GameObject Root { get; private set; }
        public SkinnedMeshRenderer Renderer { get; private set; }
        public Renderer[] Extras { get; private set; } = Array.Empty<Renderer>();
        public Matrix4x4 RendererToRoot => Root.transform.worldToLocalMatrix * Renderer.transform.localToWorldMatrix;

        public VatBakeCopy(SkinnedMeshRenderer source, IReadOnlyList<Renderer> extras)
        {
            try
            {
                Create(source, extras);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public Animator PrepareAnimator()
        {
            var animator = Root.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = Root.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (!animator.hasTransformHierarchy)
            {
                AnimatorUtility.DeoptimizeTransformHierarchy(animator.gameObject);
            }

            return animator;
        }

        public void Dispose()
        {
            if (_scene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(_scene);
            }

            Root = null;
            Renderer = null;
            Extras = Array.Empty<Renderer>();
        }

        private void Create(SkinnedMeshRenderer source, IReadOnlyList<Renderer> extras)
        {
            var sourceRoot = source.transform.root;
            _scene = EditorSceneManager.NewPreviewScene();
            Root = Instantiate(sourceRoot.gameObject, NewHolder());
            Renderer = FindRenderer<SkinnedMeshRenderer>(source, sourceRoot);
            Renderer.quality = SkinQuality.Bone4;
            Extras = new Renderer[extras.Count];
            for (var i = 0; i < Extras.Length; i++)
            {
                Extras[i] = FindRenderer<Renderer>(extras[i], sourceRoot);
            }
        }

        private Transform NewHolder()
        {
            var holder = EditorUtility.CreateGameObjectWithHideFlags(BakeName, HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(holder, _scene);
            return holder.transform;
        }

        private static GameObject Instantiate(GameObject source, Transform parent)
        {
            var root = Object.Instantiate(source, parent);
            root.name = source.name;
            root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        private T FindRenderer<T>(Renderer source, Transform sourceRoot) where T : Renderer
        {
            var path = AnimationUtility.CalculateTransformPath(source.transform, sourceRoot);
            var transform = path.Length == 0 ? Root.transform : Root.transform.Find(path);
            var renderer = transform != null ? transform.GetComponent<T>() : null;
            if (renderer == null)
            {
                throw new InvalidOperationException($"Renderer '{path}' not found in the bake copy of '{Root.name}'.");
            }

            return renderer;
        }
    }
}
