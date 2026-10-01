using UnityEngine;

namespace VATyakov.Editor
{
    // §2.1: the default pose of the bake copy. Restored before each clip, otherwise what a clip leaves unanimated
    // keeps the last pose of the previous clip and the bake depends on the clip order.
    sealed class VatPoseSnapshot
    {
        readonly Transform[] _transforms;
        readonly Vector3[] _positions;
        readonly Quaternion[] _rotations;
        readonly Vector3[] _scales;
        readonly SkinnedMeshRenderer[] _renderers;
        readonly float[][] _weights;

        public VatPoseSnapshot(GameObject root)
        {
            _transforms = root.GetComponentsInChildren<Transform>(true);
            _positions = new Vector3[_transforms.Length];
            _rotations = new Quaternion[_transforms.Length];
            _scales = new Vector3[_transforms.Length];
            for (int i = 0; i < _transforms.Length; i++)
                _transforms[i].GetLocalPositionAndRotation(out _positions[i], out _rotations[i]);
            for (int i = 0; i < _transforms.Length; i++)
                _scales[i] = _transforms[i].localScale;
            _renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            _weights = System.Array.ConvertAll(_renderers, Weights);
        }

        public void Restore()
        {
            for (int i = 0; i < _transforms.Length; i++)
            {
                _transforms[i].SetLocalPositionAndRotation(_positions[i], _rotations[i]);
                _transforms[i].localScale = _scales[i];
            }
            for (int r = 0; r < _renderers.Length; r++)
                for (int s = 0; s < _weights[r].Length; s++)
                    _renderers[r].SetBlendShapeWeight(s, _weights[r][s]);
        }

        static float[] Weights(SkinnedMeshRenderer renderer)
        {
            int count = renderer.sharedMesh != null ? renderer.sharedMesh.blendShapeCount : 0;
            var weights = new float[count];
            for (int s = 0; s < count; s++)
                weights[s] = renderer.GetBlendShapeWeight(s);
            return weights;
        }
    }
}
