using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VATyakov
{
    internal sealed class VatBlendCheck
    {
        private const string Advice = "Transitions need a blend VAT shader (such as vat_lit_vertex_blend) on every VAT material of the unit.";

        private bool _isReported;

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public void ReportInstantCrossFade(Object context, VatClip clip, IReadOnlyList<Material> materials)
        {
            if (_isReported)
            {
                return;
            }

            _isReported = true;
            var material = materials.First(candidate => !VatMaterialCopies.IsBlend(candidate));
            Debug.LogWarning($"{context.name}: CrossFade to '{clip.Name}' plays instantly, material '{material.name}' has no transition. {Advice}", context);
        }
    }
}
