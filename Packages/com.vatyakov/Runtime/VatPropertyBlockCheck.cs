using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VATyakov
{
    internal sealed class VatPropertyBlockCheck
    {
        private const string Advice = "VAT state lives in the unit's materials: use VatAnimator.SetFloat/SetColor/SetVector instead.";

        private readonly HashSet<Renderer> _reported = new();

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public void Run(IReadOnlyList<Renderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.HasPropertyBlock() && _reported.Add(renderer))
                {
                    Debug.LogError($"{renderer.name}: the renderer has a MaterialPropertyBlock. {Advice}", renderer);
                }
            }
        }
    }
}
