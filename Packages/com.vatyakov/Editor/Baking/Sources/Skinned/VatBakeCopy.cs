using System;
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
        public Matrix4x4 RendererToRoot => Root.transform.worldToLocalMatrix * Renderer.transform.localToWorldMatrix;

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

        public static Animator AnimatorWithAvatar(Component renderer)
        {
            return Array.Find(renderer.GetComponentsInParent<Animator>(true), animator => animator.avatar != null);
        }

        public GameObject LegacyRoot()
        {
            var animation = Renderer.GetComponentInParent<Animation>(true);
            return animation != null ? animation.gameObject : Root;
        }

        public Animator PrepareAnimator()
        {
            var animator = FindAnimator();
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
        }

        private void Create(SkinnedMeshRenderer source)
        {
            var sourceRoot = source.transform.root;
            _scene = EditorSceneManager.NewPreviewScene();
            Root = Instantiate(sourceRoot.gameObject, NewHolder());
            Renderer = FindRenderer(source, sourceRoot);
            Renderer.quality = SkinQuality.Bone4;
        }

        private Animator FindAnimator()
        {
            var withAvatar = AnimatorWithAvatar(Renderer);
            if (withAvatar != null)
            {
                return withAvatar;
            }

            var parent = Renderer.GetComponentInParent<Animator>(true);
            return parent != null ? parent : Root.GetComponentInChildren<Animator>(true);
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

        private SkinnedMeshRenderer FindRenderer(SkinnedMeshRenderer source, Transform sourceRoot)
        {
            var index = Array.IndexOf(sourceRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true), source);
            var renderers = Root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (index >= renderers.Length || renderers[index].sharedMesh != source.sharedMesh)
            {
                throw new VatBakeException($"'{source.name}' is not found in the bake copy of '{Root.name}': a script of the prefab changes its renderers.");
            }

            return renderers[index];
        }
    }
}
