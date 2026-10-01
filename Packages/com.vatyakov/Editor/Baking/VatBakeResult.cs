using UnityEngine;

namespace VATyakov.Editor
{
    // Readable in-memory data, consumed by VatAssetWriter.
    sealed class VatBakeResult
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly Texture2D Position;
        public readonly Texture2D Rotation;
        public readonly VatQuantizationStats Stats;
        public readonly VatChirality Chirality;

        public VatBakeResult(VatLayout layout, Mesh mesh, Texture2D position, Texture2D rotation, VatQuantizationStats stats,
            VatChirality chirality)
        {
            Layout = layout;
            Mesh = mesh;
            Position = position;
            Rotation = rotation;
            Stats = stats;
            Chirality = chirality;
        }
    }
}
