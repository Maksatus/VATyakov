using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneEncoder
    {
        public readonly VatLayout Layout;
        public readonly VatBoneRig Rig;
        public readonly VatRotationSigns Signs;

        private readonly VatBoneTexels _texels;
        private readonly VatBoneBounds _bounds;
        private readonly Vector4[] _offsetScales;
        private readonly Vector4[] _rotations;

        public float MaxError { get; private set; }
        public VatPrecision Precision => new VatPrecision(0f, MaxError, 0f);

        public VatBoneEncoder(VatLayout layout, VatBoneRig rig)
        {
            Layout = layout;
            Rig = rig;
            Signs = new VatRotationSigns(rig.BoneCount, VatRotationSigns.Bone);
            _texels = new VatBoneTexels(layout.Info);
            _bounds = new VatBoneBounds(rig);
            _offsetScales = new Vector4[rig.BoneCount];
            _rotations = new Vector4[rig.BoneCount];
            for (var bone = 0; bone < rig.BoneCount; bone++)
            {
                _texels.WritePivot(bone, rig.Pivots[bone]);
            }
        }

        public string AddFrame(int clip, int frame, Matrix4x4[] skin)
        {
            var clipLayout = Layout.Clips[clip];
            for (var bone = 0; bone < Rig.BoneCount; bone++)
            {
                var similarity = VatSimilarity.Of(skin[bone]);
                var problem = Rig.IsUsed(bone) ? VatBoneCheck.Problem(similarity, Rig, bone) : null;
                if (problem != null)
                {
                    return problem;
                }

                EncodeBone(bone, clipLayout.StartRow + frame, frame, skin[bone], similarity);
            }

            MeasureError(skin);
            if (clipLayout.IsLooping && frame == clipLayout.FrameCount - 1)
            {
                Signs.CloseLoop(clipLayout.Name);
            }

            return null;
        }

        public Mesh BuildMesh(int skinIndex, string name)
        {
            var skin = Rig.Skins[skinIndex];
            var subMeshes = new VatSubMeshes(skin.Source);
            var bounds = _bounds.Build(skin, subMeshes);
            return VatBoneMeshBuilder.Build(name, skin, subMeshes, bounds);
        }

        public Texture2D BuildTexture(string name)
        {
            return _texels.Build(name);
        }

        private void EncodeBone(int bone, int row, int frame, Matrix4x4 skin, VatSimilarity similarity)
        {
            var pivot = Rig.Pivots[bone];
            var offset = skin.MultiplyPoint3x4(pivot) - pivot;
            var rotation = Signs.Align(bone, frame, similarity.Rotation);
            var offsetScale = new Vector4(offset.x, offset.y, offset.z, similarity.Scale);
            _texels.WritePose(bone, row, offsetScale, rotation, out _offsetScales[bone], out _rotations[bone]);
            _bounds.Add(bone, pivot + (Vector3)_offsetScales[bone], _offsetScales[bone].w);
        }

        private void MeasureError(Matrix4x4[] skin)
        {
            foreach (var boneSkin in Rig.Skins)
            {
                MeasureError(boneSkin, skin);
            }
        }

        private void MeasureError(VatBoneSkin boneSkin, Matrix4x4[] skin)
        {
            for (var vertex = 0; vertex < boneSkin.Influences.Length; vertex++)
            {
                var influence = boneSkin.Influences[vertex];
                var rest = boneSkin.Rest.Positions[vertex];
                var decoded = Vector3.LerpUnclamped(Point(influence.Bone1, rest), Point(influence.Bone0, rest), influence.StoredWeight0);
                var reference = Vector3.LerpUnclamped(skin[influence.Bone1].MultiplyPoint3x4(rest), skin[influence.Bone0].MultiplyPoint3x4(rest),
                    influence.Weight0);
                MaxError = Mathf.Max(MaxError, Vector3.Distance(decoded, reference));
            }
        }

        private Vector3 Point(int bone, Vector3 rest)
        {
            return VatMath.BonePoint(_offsetScales[bone], _rotations[bone], Rig.Pivots[bone], rest);
        }
    }
}
