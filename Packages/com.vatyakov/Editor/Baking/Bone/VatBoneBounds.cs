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

        public Bounds Build(VatBoneSkin skin, VatSubMeshes subMeshes)
        {
            for (var subMesh = 0; subMesh < subMeshes.Count; subMesh++)
            {
                foreach (var vertex in subMeshes.Vertices(subMesh))
                {
                    subMeshes.Encapsulate(subMesh, Min(skin, vertex));
                    subMeshes.Encapsulate(subMesh, Max(skin, vertex));
                }
            }

            var total = new VatBoundsBuilder();
            for (var vertex = 0; vertex < skin.Influences.Length; vertex++)
            {
                total.Add(Min(skin, vertex));
                total.Add(Max(skin, vertex));
            }

            return total.Bounds;
        }

        private Vector3 Min(VatBoneSkin skin, int vertex)
        {
            var influence = skin.Influences[vertex];
            var point = skin.Rest.Positions[vertex];
            return Vector3.Min(Min(influence.Bone0, point), Min(influence.Bone1, point));
        }

        private Vector3 Max(VatBoneSkin skin, int vertex)
        {
            var influence = skin.Influences[vertex];
            var point = skin.Rest.Positions[vertex];
            return Vector3.Max(Max(influence.Bone0, point), Max(influence.Bone1, point));
        }

        private Vector3 Min(int bone, Vector3 point)
        {
            return _min[bone] - Vector3.one * Radius(bone, point);
        }

        private Vector3 Max(int bone, Vector3 point)
        {
            return _max[bone] + Vector3.one * Radius(bone, point);
        }

        private float Radius(int bone, Vector3 point)
        {
            return _maxScale[bone] * Vector3.Distance(point, _rig.Pivots[bone]);
        }
    }
}
