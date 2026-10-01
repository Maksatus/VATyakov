using UnityEngine;

namespace VATyakov
{
    // §1.9.
    public static class VatShaderIds
    {
        public static readonly int PosTex = Shader.PropertyToID("_VatPosTex");
        public static readonly int RotTex = Shader.PropertyToID("_VatRotTex");
        public static readonly int DriftTex = Shader.PropertyToID("_VatDriftTex");
        public static readonly int Layout = Shader.PropertyToID("_VatLayout");
        public static readonly int Frame = Shader.PropertyToID("_VatFrame");
    }
}
