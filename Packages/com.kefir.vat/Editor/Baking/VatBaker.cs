using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Kefir.Vat.Editor
{
    /// <summary>Bake entry point: VatBakeProfile → IVatFrameSource → VatLayout → encoder → VatAssetWriter (§4).</summary>
    public static class VatBaker
    {
        /// <summary>Shader of the Unlit sample, the default for new template materials.</summary>
        public const string DefaultShaderName = "Kefir/VAT/VAT_Unlit_Vertex";

        /// <summary>Problems that block a bake, as messages for the user. Empty when the profile can be baked.</summary>
        public static List<string> Validate(VatBakeProfile profile)
        {
            var problems = new List<string>();
            if (profile.Source == null)
                problems.Add("Не задан Source — SkinnedMeshRenderer из префаба или модели.");
            else if (profile.Source.sharedMesh == null)
                problems.Add($"У '{profile.Source.name}' нет меша.");

            if (profile.Clip == null)
                problems.Add("Не задан Clip.");
            else if (!float.IsFinite(profile.Clip.length) || profile.Clip.length <= 0f)
                problems.Add($"У клипа '{profile.Clip.name}' нулевая длина.");

            if (!float.IsFinite(profile.Fps) || profile.Fps <= 0f)
                problems.Add("Fps должен быть положительным числом.");

            if (profile.Material == null && ResolveShader(profile) == null)
                problems.Add($"Нет материала-шаблона: назначьте Material или Shader (по умолчанию '{DefaultShaderName}').");
            return problems;
        }

        /// <summary>
        /// Bakes the profile. Throws VatBakeException with a message for the user; in that case existing assets are
        /// untouched. assetPath is used only when the profile has no asset yet (defaults to next to the profile).
        /// </summary>
        public static VatAsset Bake(VatBakeProfile profile, string assetPath = null)
        {
            var problems = Validate(profile);
            if (problems.Count > 0)
                throw new VatBakeException(string.Join("\n", problems));

            string path = profile.Asset != null ? AssetDatabase.GetAssetPath(profile.Asset) : assetPath ?? DefaultAssetPath(profile);
            string name = Path.GetFileNameWithoutExtension(path);

            VatLayout layout = null;
            VertexEncoder encoder = null;
            Mesh mesh = null;
            Texture2D position = null;
            try
            {
                using (var source = new SkinnedFrameSource(profile.Source, new[] { profile.Clip }))
                {
                    var requests = new VatLayout.ClipRequest[source.Clips.Count];
                    for (int c = 0; c < requests.Length; c++)
                        requests[c] = new VatLayout.ClipRequest(source.Clips[c].Name, source.Clips[c].Length, profile.Fps);

                    layout = VatLayout.ForVertex(source.Mesh.VertexCount, requests);
                    encoder = new VertexEncoder(layout, source.Mesh);
                    var frame = new VatFrame(source.Mesh.VertexCount);
                    for (int c = 0; c < layout.Clips.Length; c++)
                    {
                        var clip = layout.Clips[c];
                        for (int k = 0; k < clip.FrameCount; k++)
                        {
                            if (EditorUtility.DisplayCancelableProgressBar("VAT bake", $"{clip.Name}: кадр {k + 1}/{clip.FrameCount}", (float)k / clip.FrameCount))
                                throw new VatBakeException("Бейк отменён.");
                            source.Sample(c, VatMath.LoopFrameTime(k, clip.FrameCount, clip.Length), frame);
                            encoder.AddFrame(c, k, frame);
                        }
                    }
                }

                mesh = encoder.BuildMesh(name + "_Mesh");
                position = encoder.BuildPositionTexture(name + "_Pos");
                layout.Verify(mesh, position);
            }
            catch
            {
                if (mesh != null)
                    Object.DestroyImmediate(mesh);
                if (position != null)
                    Object.DestroyImmediate(position);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var asset = VatAssetWriter.Write(profile.Asset, path, layout, mesh, position, SourceHash(profile));
            profile.Asset = asset;

            var material = profile.Material != null ? profile.Material : CreateMaterial(profile, path);
            material.enableInstancing = false; // §1.6: GPU instancing stays off on VAT materials
            asset.ApplyTo(material, 0);
            EditorUtility.SetDirty(material);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            Debug.Log(Describe(asset) + string.Format(CultureInfo.InvariantCulture,
                "; max |Δ| {0:0.###} м, max ошибка half {1:0.###} мм", encoder.MaxOffset, encoder.MaxQuantizationError * 1000f), asset);
            return asset;
        }

        /// <summary>One-line summary of a baked asset for logs and inspectors.</summary>
        public static string Describe(VatAsset asset)
        {
            if (asset == null)
                return "Ещё не запечён.";
            if (!asset.TryValidate(out var error))
                return error;

            var info = asset.Layout;
            var clip = asset.Clips[0];
            long bytes = PositionTextureBytes(info);
            return string.Format(CultureInfo.InvariantCulture,
                "VAT '{0}': {1} вертексов, клип '{2}' — {3} кадров, {4:0.###} fps; _VatPosTex {5}×{6} ({7} блок.), {8:0.##} МБ",
                asset.name, info.Elements, clip.Name, clip.FrameCount, clip.FrameRate, info.Width, info.Height, info.Blocks,
                bytes / (1024.0 * 1024.0));
        }

        /// <summary>Actual size of _VatPosTex in bytes: width × height × 8 (RGBAHalf), padding texels included.</summary>
        internal static long PositionTextureBytes(VatLayoutInfo info) => (long)info.Width * info.Height * 8;

        static Shader ResolveShader(VatBakeProfile profile) => profile.Shader != null ? profile.Shader : Shader.Find(DefaultShaderName);

        static string DefaultAssetPath(VatBakeProfile profile)
        {
            string profilePath = AssetDatabase.GetAssetPath(profile);
            if (string.IsNullOrEmpty(profilePath))
                throw new VatBakeException("Профиль не сохранён как ассет — передайте путь для VatAsset.");
            string folder = Path.GetDirectoryName(profilePath)?.Replace('\\', '/');
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/{profile.name}_Vat.asset");
        }

        static string SourceHash(VatBakeProfile profile)
        {
            var hash = new Hash128();
            hash.Append(VatAsset.CurrentFormatVersion);
            hash.Append(DependencyHash(profile.Source));
            hash.Append(AnimationUtility.CalculateTransformPath(profile.Source.transform, profile.Source.transform.root));
            hash.Append(DependencyHash(profile.Clip));
            hash.Append(profile.Clip.name);
            hash.Append(profile.Fps);
            return hash.ToString();
        }

        static string DependencyHash(Object target)
        {
            string path = AssetDatabase.GetAssetPath(target);
            return string.IsNullOrEmpty(path) ? "scene:" + target.name : AssetDatabase.GetAssetDependencyHash(path).ToString();
        }

        static Material CreateMaterial(VatBakeProfile profile, string assetPath)
        {
            var material = new Material(ResolveShader(profile)) { enableInstancing = false };
            // Start from the source's base map so the template looks like the source out of the box.
            var sourceMaterial = profile.Source.sharedMaterial;
            if (sourceMaterial != null && sourceMaterial.mainTexture != null && material.HasTexture("_BaseMap"))
                material.SetTexture("_BaseMap", sourceMaterial.mainTexture);

            AssetDatabase.CreateAsset(material, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(assetPath, ".mat")));
            profile.Material = material;
            return material;
        }

        /// <summary>
        /// Test prefab for the Compare scene: MeshFilter + MeshRenderer with the template material in every slot.
        /// Game prefabs are assembled by hand, so a bake never calls this. An existing prefab is updated in place,
        /// which also refreshes its material slots after the submesh count changed.
        /// </summary>
        public static GameObject CreatePrefab(VatBakeProfile profile)
        {
            var asset = profile.Asset;
            if (asset == null)
                throw new VatBakeException("Сначала запеките профиль.");
            if (!asset.TryValidate(out var error))
                throw new VatBakeException(error);
            if (profile.Material == null)
                throw new VatBakeException("Нет материала-шаблона — запеките профиль заново.");

            string path = profile.Prefab != null
                ? AssetDatabase.GetAssetPath(profile.Prefab)
                : AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(AssetDatabase.GetAssetPath(asset), ".prefab"));

            GameObject prefab;
            if (profile.Prefab != null)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    Fill(root, asset, profile.Material);
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            else
            {
                // Built in a preview scene so the user's scenes are never marked dirty.
                var scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    var root = EditorUtility.CreateGameObjectWithHideFlags(Path.GetFileNameWithoutExtension(path), HideFlags.HideAndDontSave);
                    SceneManager.MoveGameObjectToScene(root, scene);
                    root.hideFlags = HideFlags.None;
                    Fill(root, asset, profile.Material);
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }

            profile.Prefab = prefab;
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssetIfDirty(profile);
            return prefab;
        }

        static void Fill(GameObject root, VatAsset asset, Material material)
        {
            var filter = root.GetComponent<MeshFilter>();
            if (filter == null)
                filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = asset.Mesh;

            var renderer = root.GetComponent<MeshRenderer>();
            if (renderer == null)
                renderer = root.AddComponent<MeshRenderer>();
            var materials = new Material[Mathf.Max(1, asset.Mesh.subMeshCount)];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }
}
