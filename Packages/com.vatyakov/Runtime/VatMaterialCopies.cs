using System.Collections.Generic;
using UnityEngine;

namespace VATyakov
{
    // §1.6: one DontSave copy per unique VAT template in the renderers' slots, put into the slots in its place.
    // Never renderer.material: it clones on its own and the clone leaks.
    sealed class VatMaterialCopies
    {
        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<Material[]> _shared = new List<Material[]>();
        readonly Dictionary<Material, Material> _copies = new Dictionary<Material, Material>();
        readonly List<Material> _materials = new List<Material>();

        public VatMaterialCopies(Renderer[] renderers, string owner)
        {
            foreach (var renderer in renderers)
                Replace(renderer, owner);
        }

        public IReadOnlyList<Material> Materials => _materials;

        // Only the renderers with a VAT slot.
        public IReadOnlyList<Renderer> Renderers => _renderers;

        public static bool IsVat(Material material) => material != null && material.HasProperty(VatShaderIds.Frame);

        void Replace(Renderer renderer, string owner)
        {
            var shared = renderer.sharedMaterials;
            var slots = (Material[])shared.Clone();
            bool replaced = false;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!IsVat(slots[i]))
                    continue;
                slots[i] = CopyOf(slots[i], owner);
                replaced = true;
            }

            if (!replaced)
                return;
            renderer.sharedMaterials = slots;
            _renderers.Add(renderer);
            _shared.Add(shared);
        }

        Material CopyOf(Material template, string owner)
        {
            if (_copies.TryGetValue(template, out var copy))
                return copy;
            copy = new Material(template) { name = $"{template.name} ({owner})", hideFlags = HideFlags.DontSave };
            _copies.Add(template, copy);
            _materials.Add(copy);
            return copy;
        }

        // Renderers destroyed together with the unit are skipped.
        public void Dispose()
        {
            for (int i = 0; i < _renderers.Count; i++)
                if (_renderers[i] != null)
                    _renderers[i].sharedMaterials = _shared[i];
            foreach (var material in _materials)
                Destroy(material);
            _renderers.Clear();
            _shared.Clear();
            _copies.Clear();
            _materials.Clear();
        }

        static void Destroy(Material material)
        {
            if (Application.isPlaying)
                Object.Destroy(material);
            else
                Object.DestroyImmediate(material);
        }
    }
}
