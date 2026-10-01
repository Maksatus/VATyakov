using UnityEngine;

namespace VATyakov
{
    // §1.9.
    public static class VatShaderIds
    {
        public static readonly int PosTex = Shader.PropertyToID("_VatPosTex");
        public static readonly int Layout = Shader.PropertyToID("_VatLayout");
        public static readonly int ClipA = Shader.PropertyToID("_VatClipA");
    }
}
