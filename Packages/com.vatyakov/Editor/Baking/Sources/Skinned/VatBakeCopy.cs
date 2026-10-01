using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    // Copy of the source prefab in a preview scene: user scenes stay clean, nothing is left behind.
    sealed class VatBakeCopy : IDisposable
    {
        Scene _scene;

        public VatBakeCopy(SkinnedMeshRenderer source)
        {
            try
            {
                Create(source);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public GameObject Root { get; private set; }

        public SkinnedMeshRenderer Renderer { get; private set; }

        public Matrix4x4 RendererToRoot => Root.transform.worldToLocalMatrix * Renderer.transform.localToWorldMatrix;

        // §2.1: controller off, root motion off, transform hierarchy exposed.
        public Animator PrepareAnimator()
        {
            var animator = Root.GetComponentInChildren<Animator>(true);
            if (animator == null)
                animator = Root.AddComponent<Animator>();
            animator.enabled = true;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (!animator.hasTransformHierarchy)
                AnimatorUtility.DeoptimizeTransformHierarchy(animator.gameObject);
            return animator;
        }

        public void Dispose()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Root = null;
            Renderer = null;
        }

        void Create(SkinnedMeshRenderer source)
        {
            var sourceRoot = source.transform.root;
            _scene = EditorSceneManager.NewPreviewScene();
            Root = Instantiate(sourceRoot.gameObject, NewHolder());
            Renderer = FindRenderer(AnimationUtility.CalculateTransformPath(source.transform, sourceRoot));
            Renderer.quality = SkinQuality.Bone4; // independent of the editor's quality level
        }

        Transform NewHolder()
        {
            var holder = EditorUtility.CreateGameObjectWithHideFlags("VatBake", HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(holder, _scene);
            return holder.transform;
        }

        static GameObject Instantiate(GameObject source, Transform parent)
        {
            var root = Object.Instantiate(source, parent);
            root.name = source.name;
            root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            return root;
        }

        SkinnedMeshRenderer FindRenderer(string path)
        {
            var transform = path.Length == 0 ? Root.transform : Root.transform.Find(path);
            var renderer = transform != null ? transform.GetComponent<SkinnedMeshRenderer>() : null;
            if (renderer == null)
                throw new InvalidOperationException($"Renderer '{path}' not found in the bake copy of '{Root.name}'.");
            return renderer;
        }
    }
}
