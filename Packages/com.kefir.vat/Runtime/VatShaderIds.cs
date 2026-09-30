using UnityEngine;

namespace Kefir.Vat
{
    /// <summary>Material property IDs of the VAT shaders (§1.9).</summary>
    public static class VatShaderIds
    {
        /// <summary>RGBAHalf offsets from the rest positions (Vertex mode).</summary>
        public static readonly int PosTex = Shader.PropertyToID("_VatPosTex");

        /// <summary>Asset constants: (W, totalRows, driftOn, 0).</summary>
        public static readonly int Layout = Shader.PropertyToID("_VatLayout");

        /// <summary>Source clip state: (±(startRow·4096 + F), rate, t0, offset).</summary>
        public static readonly int ClipA = Shader.PropertyToID("_VatClipA");
    }
}
