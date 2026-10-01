using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VATyakov.Editor
{
    // Prefab for the Compare scene; game prefabs are assembled by hand. Updating in place keeps references
    // and refreshes material slots after the submesh count changed.
    static class VatTestPrefab
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

        static void RequireBaked(VatBakeProfile profile)
        {
            if (profile.Asset == null)
                throw new VatBakeException("Сначала запеките профиль.");
            if (!profile.Asset.TryValidate(out var error))
                throw new VatBakeException(error);
            if (profile.Material == null)
                throw new VatBakeException("Нет материала-шаблона — запеките профиль заново.");
        }

        static GameObject Update(VatBakeProfile profile)
        {
            string path = AssetDatabase.GetAssetPath(profile.Prefab);
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

        // Built in a preview scene so the user's scenes are never marked dirty.
        static GameObject Create(VatBakeProfile profile)
        {
            string path = NewPath(profile.Asset);
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

        static string NewPath(VatAsset asset) =>
            AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(AssetDatabase.GetAssetPath(asset), ".prefab"));

        static GameObject NewRoot(Scene scene, string name)
        {
            var root = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            SceneManager.MoveGameObjectToScene(root, scene);
            root.hideFlags = HideFlags.None;
            return root;
        }

        static void Fill(GameObject root, VatBakeProfile profile)
        {
            GetOrAdd<MeshFilter>(root).sharedMesh = profile.Asset.Mesh;
            GetOrAdd<MeshRenderer>(root).sharedMaterials = Slots(profile.Material, profile.Asset.Mesh.subMeshCount);
        }

        static Material[] Slots(Material material, int subMeshCount)
        {
            var slots = new Material[Mathf.Max(1, subMeshCount)];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = material;
            return slots;
        }

        static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
