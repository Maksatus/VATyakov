using System.Collections.Generic;
using UnityEngine;

namespace VATyakov
{
    internal sealed class VatMaterialCopies
    {
        private readonly List<Renderer> _renderers = new();
        private readonly List<Material[]> _shared = new();
        private readonly Dictionary<Material, Material> _copies = new();
        private readonly List<Material> _materials = new();

        public IReadOnlyList<Material> Materials => _materials;
        public IReadOnlyList<Renderer> Renderers => _renderers;
        public bool CanBlend { get; }

        public VatMaterialCopies(Renderer[] renderers, string owner)
        {
            foreach (var renderer in renderers)
            {
                Replace(renderer, owner);
            }

            CanBlend = _materials.TrueForAll(IsBlend);
        }

        public static bool IsVat(Material material)
        {
            return material != null && material.HasProperty(VatShaderIds.Frame);
        }

        public static bool IsBlend(Material material)
        {
            return material.HasProperty(VatShaderIds.FrameB);
        }

        public void Dispose()
        {
            for (var i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].sharedMaterials = _shared[i];
                }
            }

            foreach (var material in _materials)
            {
                Destroy(material);
            }

            _renderers.Clear();
            _shared.Clear();
            _copies.Clear();
            _materials.Clear();
        }

        private void Replace(Renderer renderer, string owner)
        {
            var shared = renderer.sharedMaterials;
            var slots = (Material[])shared.Clone();
            var isReplaced = false;
            for (var i = 0; i < slots.Length; i++)
            {
                if (IsVat(slots[i]))
                {
                    slots[i] = CopyOf(slots[i], owner);
                    isReplaced = true;
                }
            }

            if (!isReplaced)
            {
                return;
            }

            renderer.sharedMaterials = slots;
            _renderers.Add(renderer);
            _shared.Add(shared);
        }

        private Material CopyOf(Material template, string owner)
        {
            if (_copies.TryGetValue(template, out var copy))
            {
                return copy;
            }

            copy = new Material(template) { name = $"{template.name} ({owner})", hideFlags = HideFlags.DontSave };
            _copies.Add(template, copy);
            _materials.Add(copy);
            return copy;
        }

        private static void Destroy(Material material)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }
    }
}
