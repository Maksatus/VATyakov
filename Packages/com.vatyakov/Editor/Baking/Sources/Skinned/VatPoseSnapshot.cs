using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatPoseSnapshot
    {
        private readonly Transform[] _transforms;
        private readonly Vector3[] _positions;
        private readonly Quaternion[] _rotations;
        private readonly Vector3[] _scales;
        private readonly SkinnedMeshRenderer[] _renderers;
        private readonly float[][] _weights;

        public VatPoseSnapshot(GameObject root)
        {
            _transforms = root.GetComponentsInChildren<Transform>(true);
            _positions = new Vector3[_transforms.Length];
            _rotations = new Quaternion[_transforms.Length];
            _scales = new Vector3[_transforms.Length];
            for (var i = 0; i < _transforms.Length; i++)
            {
                _transforms[i].GetLocalPositionAndRotation(out _positions[i], out _rotations[i]);
            }

            for (var i = 0; i < _transforms.Length; i++)
            {
                _scales[i] = _transforms[i].localScale;
            }

            _renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _weights = Array.ConvertAll(_renderers, Weights);
        }

        public void Restore()
        {
            for (var i = 0; i < _transforms.Length; i++)
            {
                _transforms[i].SetLocalPositionAndRotation(_positions[i], _rotations[i]);
                _transforms[i].localScale = _scales[i];
            }

            for (var r = 0; r < _renderers.Length; r++)
            {
                for (var s = 0; s < _weights[r].Length; s++)
                {
                    _renderers[r].SetBlendShapeWeight(s, _weights[r][s]);
                }
            }
        }

        private static float[] Weights(SkinnedMeshRenderer renderer)
        {
            var count = renderer.sharedMesh != null ? renderer.sharedMesh.blendShapeCount : 0;
            var weights = new float[count];
            for (var s = 0; s < count; s++)
            {
                weights[s] = renderer.GetBlendShapeWeight(s);
            }

            return weights;
        }
    }
}
