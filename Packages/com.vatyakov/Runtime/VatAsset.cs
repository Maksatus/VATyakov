using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov
{
    // §1.8: main object of a .asset with Mesh and Texture2D sub-assets.
    // Binary serialization keeps pixel data compact in a Force Text project.
    [PreferBinarySerialization]
    public sealed class VatAsset : ScriptableObject
    {
        // Bump on any incompatible change of the channel map (§1.9).
        public const int CurrentFormatVersion = 1;

        [SerializeField] int _formatVersion;
        [SerializeField] VatLayoutInfo _layout;
        [SerializeField] Mesh _mesh;
        [SerializeField] Texture2D _positionTexture;
        [SerializeField] VatClip[] _clips = Array.Empty<VatClip>();
        [SerializeField] string _sourceHash = string.Empty;

        public int FormatVersion => _formatVersion;

        public VatLayoutInfo Layout => _layout;

        public Mesh Mesh => _mesh;

        public Texture2D PositionTexture => _positionTexture;

        public IReadOnlyList<VatClip> Clips => _clips;

        public string SourceHash => _sourceHash;

        public bool IsFormatSupported => _formatVersion == CurrentFormatVersion;

        bool IsComplete => _mesh != null && _positionTexture != null && _clips.Length > 0;

        public bool TryValidate(out string error)
        {
            error = !IsFormatSupported ? FormatError() : !IsComplete ? IncompleteError() : null;
            return error == null;
        }

        // Template materials show the first frame of the clip; playback is the driver's job (§1.3).
        public void ApplyTo(Material material, int clipIndex)
        {
            RequireApplicable(material, clipIndex);
            material.SetTexture(VatShaderIds.PosTex, _positionTexture);
            material.SetVector(VatShaderIds.Layout, _layout.ShaderLayout);
            material.SetVector(VatShaderIds.Frame, _clips[clipIndex].Frame(0.0));
        }

        internal void SetData(VatLayoutInfo layout, Mesh mesh, Texture2D positionTexture, VatClip[] clips, string sourceHash)
        {
            _formatVersion = CurrentFormatVersion;
            _layout = layout;
            _mesh = mesh;
            _positionTexture = positionTexture;
            _clips = clips;
            _sourceHash = sourceHash;
        }

        void RequireApplicable(Material material, int clipIndex)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));
            if (!TryValidate(out var error))
                throw new InvalidOperationException(error);
            if ((uint)clipIndex >= (uint)_clips.Length)
                throw new ArgumentOutOfRangeException(nameof(clipIndex), clipIndex, $"VAT asset '{name}' has {_clips.Length} clip(s).");
        }

        string FormatError() =>
            $"VAT asset '{name}' has format version {_formatVersion}, this package plays version {CurrentFormatVersion}. Rebake it.";

        string IncompleteError() => $"VAT asset '{name}' is incomplete (mesh, position texture or clips missing). Rebake it.";
    }
}
