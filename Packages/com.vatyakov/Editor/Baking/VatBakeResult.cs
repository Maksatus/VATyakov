using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBakeResult
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly VatBakeTextures Textures;
        public readonly Vector3[] Drift;
        public readonly VatPrecision Precision;
        public readonly float MaxOffset;
        public readonly VatQuantizationStats Stats;
        public readonly VatChirality Chirality;
        public readonly IReadOnlyList<string> Warnings;

        public VatBakeResult(VatLayout layout, Mesh mesh, VatBakeTextures textures, VatVertexEncoder encoder, IReadOnlyList<string> warnings)
        {
            Layout = layout;
            Mesh = mesh;
            Textures = textures;
            Drift = encoder.Drift;
            Precision = encoder.Precision;
            MaxOffset = encoder.MaxOffset;
            Stats = encoder.Stats;
            Chirality = encoder.Chirality;
            Warnings = warnings;
        }
    }
}
