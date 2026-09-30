using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kefir.Vat
{
    /// <summary>
    /// Baked VAT data (§1.8): the main object of a .asset with Mesh and Texture2D sub-assets.
    /// Binary serialization keeps texture data compact in a Force Text project.
    /// </summary>
    [PreferBinarySerialization]
    public sealed class VatAsset : ScriptableObject
    {
        /// <summary>Format this runtime plays. Bumped on any incompatible change of the channel map (§1.9).</summary>
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

        /// <summary>_VatPosTex: RGBAHalf offsets from the rest positions (§1.9).</summary>
        public Texture2D PositionTexture => _positionTexture;

        public IReadOnlyList<VatClip> Clips => _clips;

        /// <summary>Hash of the source and the bake settings, used to detect stale bakes.</summary>
        public string SourceHash => _sourceHash;

        /// <summary>False when the asset was baked by an incompatible version of the package.</summary>
        public bool IsFormatSupported => _formatVersion == CurrentFormatVersion;

        /// <summary>Checks that this runtime can play the asset.</summary>
        public bool TryValidate(out string error)
        {
            if (!IsFormatSupported)
            {
                error = $"VAT asset '{name}' has format version {_formatVersion}, this package plays version {CurrentFormatVersion}. Rebake it.";
                return false;
            }

            if (_mesh == null || _positionTexture == null || _clips.Length == 0)
            {
                error = $"VAT asset '{name}' is incomplete (mesh, position texture or clips missing). Rebake it.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Writes the textures, the asset constants and the state of one clip (t0 = 0, speed 1) into a material.
        /// Meant for template materials; per-unit state is written by VatAnimator.
        /// </summary>
        public void ApplyTo(Material material, int clipIndex)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material));
            if (!TryValidate(out var error))
                throw new InvalidOperationException(error);
            if ((uint)clipIndex >= (uint)_clips.Length)
                throw new ArgumentOutOfRangeException(nameof(clipIndex), clipIndex, $"VAT asset '{name}' has {_clips.Length} clip(s).");

            var layout = _layout.ShaderLayout;
            var state = _clips[clipIndex].State(0f);
            Debug.Assert(VatMath.IsFinite(layout) && VatMath.IsFinite(state), "VAT state must be finite.");

            material.SetTexture(VatShaderIds.PosTex, _positionTexture);
            material.SetVector(VatShaderIds.Layout, layout);
            material.SetVector(VatShaderIds.ClipA, state);
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
    }
}
