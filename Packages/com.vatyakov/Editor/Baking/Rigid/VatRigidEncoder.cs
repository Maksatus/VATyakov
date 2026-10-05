using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidEncoder
    {
        private const string ScaleHint = "Rigid mode takes only rotation and uniform scale of pieces: fix the transforms in the .abc or use Mode = Vertex.";

        public readonly VatLayout Layout;
        public readonly VatRigidPiece[] Pieces;
        public readonly VatBoneSkin Skin;
        public readonly VatRotationSigns Signs;

        private readonly VatBoneTexels _texels;
        private readonly VatBoneBounds _bounds;
        private readonly Vector4[][] _offsetScales;
        private readonly Vector4[][] _rotations;

        public float MaxError { get; private set; }
        public VatPrecision Precision => new(0f, MaxError, 0f);
        private VatClip Clip => Layout.Clips[0];

        public VatRigidEncoder(VatLayout layout, VatRigidPiece[] pieces, string meshName)
        {
            Layout = layout;
            Pieces = pieces;
            Skin = VatRigidSkin.Build(meshName, pieces);
            Signs = new VatRotationSigns(pieces.Length, VatRotationSigns.Piece);
            _texels = new VatBoneTexels(layout.Info);
            _bounds = new VatBoneBounds(Array.ConvertAll(pieces, piece => piece.Pivot));
            _offsetScales = new Vector4[pieces.Length][];
            _rotations = new Vector4[pieces.Length][];
        }

        public void EncodeAll()
        {
            for (var index = 0; index < Pieces.Length; index++)
            {
                _texels.WritePivot(index, Pieces[index].Pivot);
                Encode(index);
            }

            if (Clip.IsLooping)
            {
                Signs.CloseLoop(Clip.Name);
            }
        }

        public Vector3 Point(int index, int frame0, int frame1, float fraction, Vector3 rest)
        {
            var offsetScale = Vector4.LerpUnclamped(_offsetScales[index][frame0], _offsetScales[index][frame1], fraction);
            var rotation = Vector4.LerpUnclamped(_rotations[index][frame0], _rotations[index][frame1], fraction);
            return VatMath.BonePoint(offsetScale, rotation, Pieces[index].Pivot, rest);
        }

        public Mesh BuildMesh(string name)
        {
            var subMeshes = new VatSubMeshes(Skin.Source);
            return VatRigidMeshBuilder.Build(name, Skin, subMeshes, _bounds.Build(Skin, subMeshes));
        }

        public Texture2D BuildTexture(string name)
        {
            return _texels.Build(name);
        }

        private void Encode(int index)
        {
            var piece = Pieces[index];
            var frameCount = Clip.FrameCount;
            var poses = new Vector4[frameCount];
            var rotations = new Vector4[frameCount];
            var aligned = 0;
            for (var frame = 0; frame < frameCount; frame++)
            {
                if (piece.Visibility.IsVisible(frame))
                {
                    var similarity = Similarity(piece, frame);
                    var offset = piece.Motions[frame].MultiplyPoint3x4(piece.Pivot) - piece.Pivot;
                    poses[frame] = new Vector4(offset.x, offset.y, offset.z, similarity.Scale);
                    rotations[frame] = Signs.Align(index, aligned++, similarity.Rotation);
                }
            }

            Write(index, poses, rotations);
        }

        private VatSimilarity Similarity(VatRigidPiece piece, int frame)
        {
            var similarity = VatSimilarity.Of(piece.Motions[frame]);
            var problem = VatBoneCheck.Problem(similarity, $"piece '{piece.Name}'", piece.RestPositions, piece.Pivot);
            if (problem != null)
            {
                throw new VatBakeException(FormattableString.Invariant($"Frame {frame}: {problem}. {ScaleHint}"));
            }

            return similarity;
        }

        private void Write(int index, Vector4[] poses, Vector4[] rotations)
        {
            var piece = Pieces[index];
            var frameCount = poses.Length;
            _offsetScales[index] = new Vector4[frameCount];
            _rotations[index] = new Vector4[frameCount];
            for (var frame = 0; frame < frameCount; frame++)
            {
                var source = piece.Visibility.PoseFrame(frame);
                var pose = poses[source];
                pose.w = piece.Visibility.IsVisible(frame) ? pose.w : 0f;
                _texels.WritePose(index, Clip.StartRow + frame, pose, rotations[source], out _offsetScales[index][frame], out _rotations[index][frame]);
                _bounds.Add(index, piece.Pivot + (Vector3)_offsetScales[index][frame], _offsetScales[index][frame].w);
                if (piece.Visibility.IsVisible(frame))
                {
                    MeasureError(index, frame);
                }
            }
        }

        private void MeasureError(int index, int frame)
        {
            var piece = Pieces[index];
            var motion = piece.Motions[frame];
            foreach (var rest in piece.RestPositions)
            {
                MaxError = Mathf.Max(MaxError, Vector3.Distance(Point(index, frame, frame, 0f, rest), motion.MultiplyPoint3x4(rest)));
            }
        }
    }
}
