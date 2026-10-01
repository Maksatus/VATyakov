using UnityEngine;

namespace VATyakov.Editor
{
    // Renderer space → prefab-root space (FBX: −90° X and 0.01 scale).
    readonly struct VatRootSpace
    {
        readonly Matrix4x4 _toRoot;
        readonly Matrix4x4 _normal;
        readonly float _handedness;

        public VatRootSpace(Matrix4x4 toRoot)
        {
            _toRoot = toRoot;
            _normal = toRoot.inverse.transpose;
            _handedness = toRoot.determinant < 0f ? -1f : 1f;
        }

        public Vector3 Point(Vector3 point) => _toRoot.MultiplyPoint3x4(point);

        public Vector3 Normal(Vector3 normal) => _normal.MultiplyVector(normal).normalized;

        public Vector4 Tangent(Vector4 tangent)
        {
            var direction = _toRoot.MultiplyVector(tangent).normalized;
            return new Vector4(direction.x, direction.y, direction.z, tangent.w * _handedness);
        }
    }
}
