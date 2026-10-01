using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VATyakov
{
    // §1.6: a MaterialPropertyBlock breaks the SRP Batcher and hides the unit's materials. Checked on every state
    // write in the editor and development builds, reported once per renderer.
    sealed class VatPropertyBlockCheck
    {
        readonly HashSet<Renderer> _reported = new HashSet<Renderer>();

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public void Run(IReadOnlyList<Renderer> renderers)
        {
            foreach (var renderer in renderers)
                if (renderer != null && renderer.HasPropertyBlock() && _reported.Add(renderer))
                    Debug.LogError($"{renderer.name}: the renderer has a MaterialPropertyBlock. VAT state lives in the " +
                        "unit's materials: use VatAnimator.SetFloat/SetColor/SetVector instead.", renderer);
        }
    }
}
