using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneBounds
    {
        private readonly VatBoneRig _rig;
        private readonly Vector3[] _min;
        private readonly Vector3[] _max;
        private readonly float[] _maxScale;
        private readonly bool[] _hasPose;

        public VatBoneBounds(VatBoneRig rig)
        {
            _rig = rig;
            _min = new Vector3[rig.BoneCount];
            _max = new Vector3[rig.BoneCount];
            _maxScale = new float[rig.BoneCount];
            _hasPose = new bool[rig.BoneCount];
        }

        public void Add(int bone, Vector3 pivot, float scale)
        {
            _min[bone] = _hasPose[bone] ? Vector3.Min(_min[bone], pivot) : pivot;
            _max[bone] = _hasPose[bone] ? Vector3.Max(_max[bone], pivot) : pivot;
            _maxScale[bone] = Mathf.Max(_maxScale[bone], scale);
            _hasPose[bone] = true;
        }

        public Bounds Build(VatSubMeshes subMeshes)
        {
            for (var subMesh = 0; subMesh < subMeshes.Count; subMesh++)
            {
                foreach (var vertex in subMeshes.Vertices(subMesh))
                {
                    subMeshes.Encapsulate(subMesh, Min(vertex));
                    subMeshes.Encapsulate(subMesh, Max(vertex));
                }
            }

            var total = new VatBoundsBuilder();
            for (var vertex = 0; vertex < _rig.Influences.Length; vertex++)
            {
                total.Add(Min(vertex));
                total.Add(Max(vertex));
            }

            return total.Bounds;
        }

        private Vector3 Min(int vertex)
        {
            var influence = _rig.Influences[vertex];
            return Vector3.Min(Min(influence.Bone0, vertex), Min(influence.Bone1, vertex));
        }

        private Vector3 Max(int vertex)
        {
            var influence = _rig.Influences[vertex];
            return Vector3.Max(Max(influence.Bone0, vertex), Max(influence.Bone1, vertex));
        }

        private Vector3 Min(int bone, int vertex)
        {
            return _min[bone] - Vector3.one * Radius(bone, vertex);
        }

        private Vector3 Max(int bone, int vertex)
        {
            return _max[bone] + Vector3.one * Radius(bone, vertex);
        }

        private float Radius(int bone, int vertex)
        {
            return _maxScale[bone] * Vector3.Distance(_rig.Rest.Positions[vertex], _rig.Pivots[bone]);
        }
    }
}
