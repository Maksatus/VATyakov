using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov
{
    [PreferBinarySerialization]
    public sealed class VatAsset : ScriptableObject
    {
        public const int CurrentFormatVersion = 6;

        [SerializeField]
        private int _formatVersion;
        [SerializeField]
        private VatMode _mode;
        [SerializeField]
        private VatLayoutInfo _layout;
        [SerializeField]
        private Mesh _mesh;
        [SerializeField]
        private Texture2D _positionTexture;
        [SerializeField]
        private Texture2D _rotationTexture;
        [SerializeField]
        private Texture2D _boneTexture;
        [SerializeField]
        private VatPositionFormat _positionFormat;
        [SerializeField]
        private VatPositionRange _positionRange = VatPositionRange.Identity;
        [SerializeField]
        private Vector3[] _drift = Array.Empty<Vector3>();
        [SerializeField]
        private VatClip[] _clips = Array.Empty<VatClip>();
        [SerializeField]
        private VatPrecision _precision;
        [SerializeField]
        private string _fallback = string.Empty;
        [SerializeField]
        private string _sourceHash = string.Empty;

        public int FormatVersion => _formatVersion;
        public VatMode Mode => _mode;
        public VatLayoutInfo Layout => _layout;
        public Mesh Mesh => _mesh;
        public Texture2D PositionTexture => _positionTexture;
        public Texture2D RotationTexture => _rotationTexture;
        public Texture2D BoneTexture => _boneTexture;
        public VatPositionFormat PositionFormat => _positionFormat;
        public VatPositionRange PositionRange => _positionRange;
        public IReadOnlyList<VatClip> Clips => _clips;
        public VatPrecision Precision => _precision;
        public string Fallback => _fallback;
        public string SourceHash => _sourceHash;
        public bool IsFormatSupported => _formatVersion == CurrentFormatVersion;
        public bool HasDrift => _mode == VatMode.Vertex;
        public int BoneCount => _mode == VatMode.Bone ? _layout.Elements / VatMath.TexelsPerBone : 0;

        private bool IsComplete => _mesh != null && _clips.Length > 0 && (_mode == VatMode.Bone ? IsBoneComplete : IsVertexComplete);
        private bool IsVertexComplete => _positionTexture != null && _rotationTexture != null && _drift.Length == _layout.TotalRows;
        private bool IsBoneComplete => _boneTexture != null;

        public bool TryValidate(out string error)
        {
            error = !IsFormatSupported ? FormatError() : !IsComplete ? IncompleteError() : null;
            return error == null;
        }

        public int IndexOf(string clipName)
        {
            for (var i = 0; i < _clips.Length; i++)
            {
                if (_clips[i].Name == clipName)
                {
                    return i;
                }
            }

            return -1;
        }

        public void ApplyTo(Material material, int clipIndex)
        {
            if (_mode == VatMode.Bone)
            {
                material.SetTexture(VatShaderIds.BoneTexture, _boneTexture);
            }
            else
            {
                ApplyVertexData(material);
            }

            material.SetVector(VatShaderIds.Layout, _layout.ShaderLayout);
            ApplyFrame(material, _clips[clipIndex].Frame(0.0));
        }

        public void ApplyFrame(Material material, Vector4 frame)
        {
            material.SetVector(VatShaderIds.Frame, frame);
            if (HasDrift)
            {
                material.SetVector(VatShaderIds.Drift, Drift(frame));
            }
        }

        public Vector4 Drift(Vector4 frame)
        {
            return Vector3.LerpUnclamped(_drift[(int)frame.x], _drift[(int)frame.y], frame.z) + _positionRange.Min;
        }

        public Vector4 Drift(Vector4 frame, Vector4 frameB)
        {
            return Vector4.LerpUnclamped(Drift(frame), Drift(frameB), frameB.w);
        }

        internal void SetData(VatLayoutInfo layout, Mesh mesh, Texture2D positionTexture, Texture2D rotationTexture, VatPositionFormat positionFormat,
            VatPositionRange positionRange, Vector3[] drift, VatClip[] clips, VatPrecision precision, string sourceHash)
        {
            SetCommon(VatMode.Vertex, layout, mesh, clips, precision, sourceHash);
            _positionTexture = positionTexture;
            _rotationTexture = rotationTexture;
            _positionFormat = positionFormat;
            _positionRange = positionRange;
            _drift = drift;
        }

        internal void SetBoneData(VatLayoutInfo layout, Mesh mesh, Texture2D boneTexture, VatClip[] clips, VatPrecision precision, string sourceHash)
        {
            SetCommon(VatMode.Bone, layout, mesh, clips, precision, sourceHash);
            _boneTexture = boneTexture;
        }

        internal void SetFallback(string fallback)
        {
            _fallback = fallback ?? string.Empty;
        }

        private void SetCommon(VatMode mode, VatLayoutInfo layout, Mesh mesh, VatClip[] clips, VatPrecision precision, string sourceHash)
        {
            _formatVersion = CurrentFormatVersion;
            _mode = mode;
            _layout = layout;
            _mesh = mesh;
            _clips = clips;
            _precision = precision;
            _sourceHash = sourceHash;
            _positionTexture = null;
            _rotationTexture = null;
            _boneTexture = null;
            _positionFormat = VatPositionFormat.Half;
            _positionRange = VatPositionRange.Identity;
            _drift = Array.Empty<Vector3>();
            _fallback = string.Empty;
        }

        private void ApplyVertexData(Material material)
        {
            material.SetTexture(VatShaderIds.PositionTexture, _positionTexture);
            material.SetTexture(VatShaderIds.RotationTexture, _rotationTexture);
            material.SetVector(VatShaderIds.PositionScale, _positionRange.ShaderScale);
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
