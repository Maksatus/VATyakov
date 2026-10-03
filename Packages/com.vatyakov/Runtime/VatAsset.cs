using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov
{
    [PreferBinarySerialization]
    public sealed class VatAsset : ScriptableObject
    {
        public const int CurrentFormatVersion = 4;

        [SerializeField]
        private int _formatVersion;
        [SerializeField]
        private VatLayoutInfo _layout;
        [SerializeField]
        private Mesh _mesh;
        [SerializeField]
        private Texture2D _positionTexture;
        [SerializeField]
        private Texture2D _rotationTexture;
        [SerializeField]
        private Vector3[] _drift = Array.Empty<Vector3>();
        [SerializeField]
        private VatClip[] _clips = Array.Empty<VatClip>();
        [SerializeField]
        private VatPrecision _precision;
        [SerializeField]
        private string _sourceHash = string.Empty;

        public int FormatVersion => _formatVersion;
        public VatLayoutInfo Layout => _layout;
        public Mesh Mesh => _mesh;
        public Texture2D PositionTexture => _positionTexture;
        public Texture2D RotationTexture => _rotationTexture;
        public IReadOnlyList<VatClip> Clips => _clips;
        public VatPrecision Precision => _precision;
        public string SourceHash => _sourceHash;
        public bool IsFormatSupported => _formatVersion == CurrentFormatVersion;

        private bool IsComplete =>
            _mesh != null && _positionTexture != null && _rotationTexture != null && _drift.Length == _layout.TotalRows && _clips.Length > 0;

        public bool TryValidate(out string error)
        {
            error = !IsFormatSupported ? FormatError() : !IsComplete ? IncompleteError() : null;
            return error == null;
        }

        public bool TryFindClip(string clipName, out int clipIndex)
        {
            for (var i = 0; i < _clips.Length; i++)
            {
                if (_clips[i].Name == clipName)
                {
                    clipIndex = i;
                    return true;
                }
            }

            clipIndex = default;
            return false;
        }

        public void ApplyTo(Material material, int clipIndex)
        {
            RequireApplicable(material, clipIndex);
            material.SetTexture(VatShaderIds.PositionTexture, _positionTexture);
            material.SetTexture(VatShaderIds.RotationTexture, _rotationTexture);
            material.SetVector(VatShaderIds.Layout, _layout.ShaderLayout);
            ApplyFrame(material, _clips[clipIndex].Frame(0.0));
        }

        public void ApplyFrame(Material material, Vector4 frame)
        {
            material.SetVector(VatShaderIds.Frame, frame);
            material.SetVector(VatShaderIds.Drift, Drift(frame));
        }

        public Vector4 Drift(Vector4 frame)
        {
            return Vector3.LerpUnclamped(_drift[(int)frame.x], _drift[(int)frame.y], frame.z);
        }

        public Vector4 Drift(Vector4 frame, Vector4 frameB)
        {
            return Vector4.LerpUnclamped(Drift(frame), Drift(frameB), frameB.w);
        }

        internal void SetData(VatLayoutInfo layout, Mesh mesh, Texture2D positionTexture, Texture2D rotationTexture,
            Vector3[] drift, VatClip[] clips, VatPrecision precision, string sourceHash)
        {
            _formatVersion = CurrentFormatVersion;
            _layout = layout;
            _mesh = mesh;
            _positionTexture = positionTexture;
            _rotationTexture = rotationTexture;
            _drift = drift;
            _clips = clips;
            _precision = precision;
            _sourceHash = sourceHash;
        }

        private void RequireApplicable(Material material, int clipIndex)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            if (!TryValidate(out var error))
            {
                throw new InvalidOperationException(error);
            }

            if ((uint)clipIndex >= (uint)_clips.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(clipIndex), clipIndex,
                    FormattableString.Invariant($"VAT asset '{name}' has {_clips.Length} clip(s)."));
            }
        }

        private string FormatError()
        {
            return FormattableString.Invariant(
                $"VAT asset '{name}' has format version {_formatVersion}, this package plays version {CurrentFormatVersion}. Rebake it.");
        }

        private string IncompleteError()
        {
            return $"VAT asset '{name}' is incomplete (mesh, textures or clips missing). Rebake it.";
        }
    }
}
