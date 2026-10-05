using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBakeResult
    {
        public readonly VatMode Mode;
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly Mesh[] ExtraMeshes = Array.Empty<Mesh>();
        public readonly VatBakeTextures Textures;
        public readonly VatPositionFormat PositionFormat;
        public readonly VatPositionRange PositionRange = VatPositionRange.Identity;
        public readonly Vector3[] Drift = Array.Empty<Vector3>();
        public readonly VatPrecision Precision;
        public readonly float MaxOffset;
        public readonly VatQuantizationStats Stats;
        public readonly VatChirality Chirality;
        public readonly VatRotationSigns Signs;
        public readonly IReadOnlyList<string> Warnings;
        public readonly string Fallback;

        public VatBakeResult(VatVertexEncoder encoder, Mesh mesh, VatBakeTextures textures, IReadOnlyList<string> warnings, string fallback)
        {
            Mode = VatMode.Vertex;
            Layout = encoder.Layout;
            Mesh = mesh;
            Textures = textures;
            PositionFormat = encoder.Positions.Format;
            PositionRange = encoder.Positions.Range;
            Drift = encoder.Drift;
            Precision = encoder.Precision;
            MaxOffset = encoder.MaxOffset;
            Stats = encoder.Stats;
            Chirality = encoder.Chirality;
            Signs = encoder.Signs;
            Warnings = warnings;
            Fallback = fallback;
        }

        public VatBakeResult(VatBoneEncoder encoder, Mesh[] meshes, VatBakeTextures textures, IReadOnlyList<string> warnings)
        {
            Mode = VatMode.Bone;
            Layout = encoder.Layout;
            Mesh = meshes[0];
            ExtraMeshes = meshes[1..];
            Textures = textures;
            Precision = encoder.Precision;
            Signs = encoder.Signs;
            Warnings = warnings;
        }

        public VatBakeResult(VatRigidEncoder encoder, Mesh mesh, VatBakeTextures textures, IReadOnlyList<string> warnings)
        {
            Mode = VatMode.Rigid;
            Layout = encoder.Layout;
            Mesh = mesh;
            Textures = textures;
            Precision = encoder.Precision;
            Signs = encoder.Signs;
            Warnings = warnings;
        }
    }
}
