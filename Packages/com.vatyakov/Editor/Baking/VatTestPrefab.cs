using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VATyakov.Editor
{
    internal static class VatTestPrefab
    {
        public static GameObject CreateOrUpdate(VatBakeProfile profile)
        {
            RequireBaked(profile);
            var prefab = profile.Prefab != null ? Update(profile) : Create(profile);
            profile.Prefab = prefab;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return prefab;
        }

        private static void RequireBaked(VatBakeProfile profile)
        {
            if (profile.Asset == null)
            {
                throw new VatBakeException("Bake the profile first.");
            }

            if (!profile.Asset.TryValidate(out var error))
            {
                throw new VatBakeException(error);
            }

            if (profile.Material == null)
            {
                throw new VatBakeException("No template material: bake the profile again.");
            }
        }

        private static GameObject Update(VatBakeProfile profile)
        {
            var path = AssetDatabase.GetAssetPath(profile.Prefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Fill(root, profile);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject Create(VatBakeProfile profile)
        {
            var path = NewPath(profile.Asset);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = NewRoot(scene, Path.GetFileNameWithoutExtension(path));
                Fill(root, profile);
                return PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static string NewPath(VatAsset asset)
        {
            return AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(AssetDatabase.GetAssetPath(asset), ".prefab"));
        }

        private static GameObject NewRoot(Scene scene, string name)
        {
            var root = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.hideFlags = HideFlags.None;
            return root;
        }

        private static void Fill(GameObject root, VatBakeProfile profile)
        {
            GetOrAdd<MeshFilter>(root).sharedMesh = profile.Asset.Mesh;
            GetOrAdd<MeshRenderer>(root).sharedMaterials = Slots(profile.Material, profile.Asset.Mesh.subMeshCount);
            GetOrAdd<VatAnimator>(root).Asset = profile.Asset;
        }

        private static Material[] Slots(Material material, int subMeshCount)
        {
            var slots = new Material[subMeshCount];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i] = material;
            }

            return slots;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
