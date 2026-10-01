using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    // Readable in-memory data, consumed by VatAssetWriter.
    sealed class VatBakeResult
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly VatBakeTextures Textures;
        public readonly VatPrecision Precision;
        public readonly float MaxOffset;
        public readonly VatQuantizationStats Stats;
        public readonly VatChirality Chirality;
        public readonly IReadOnlyList<string> Warnings;

        public VatBakeResult(VatLayout layout, Mesh mesh, VatBakeTextures textures, VertexEncoder encoder, IReadOnlyList<string> warnings)
        {
            Layout = layout;
            Mesh = mesh;
            Textures = textures;
            Precision = encoder.Precision;
            MaxOffset = encoder.MaxOffset;
            Stats = encoder.Stats;
            Chirality = encoder.Chirality;
            Warnings = warnings;
        }
    }
}
