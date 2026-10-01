using UnityEngine;

namespace VATyakov.Editor
{
    // Readable in-memory data, consumed by VatAssetWriter.
    sealed class VatBakeResult
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly Texture2D Position;
        public readonly VatQuantizationStats Stats;

        public VatBakeResult(VatLayout layout, Mesh mesh, Texture2D position, VatQuantizationStats stats)
        {
            Layout = layout;
            Mesh = mesh;
            Position = position;
            Stats = stats;
        }
    }
}
