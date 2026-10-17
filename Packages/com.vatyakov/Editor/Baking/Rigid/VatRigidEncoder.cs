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

        private readonly VatRotationSigns _signs;
        private readonly VatBoneTexels _texels;
        private readonly VatBoneBounds _bounds;
        private readonly Vector4[][] _exactOffsetScales;
        private readonly Vector4[][] _exactRotations;

        public float MaxError { get; private set; }
        public VatPrecision Precision => new(0f, MaxError, 0f);
        private VatClip Clip => Layout.Clips[0];

        public VatRigidEncoder(VatLayout layout, VatRigidPiece[] pieces, string meshName)
        {
            Layout = layout;
            Pieces = pieces;
            Skin = VatRigidSkin.Build(meshName, pieces);
            _signs = new VatRotationSigns(pieces.Length);
            _texels = new VatBoneTexels(layout.Info);
            _bounds = new VatBoneBounds(Array.ConvertAll(pieces, piece => piece.Pivot));
            _exactOffsetScales = new Vector4[pieces.Length][];
            _exactRotations = new Vector4[pieces.Length][];
        }

        public void EncodeAll()
        {
            for (var index = 0; index < Pieces.Length; index++)
            {
                _texels.WritePivot(index, Pieces[index].Pivot);
                Encode(index);
            }
        }

        public Vector3 ExactPoint(int index, int frame0, int frame1, float fraction, Vector3 rest)
        {
            var offsetScale = Vector4.LerpUnclamped(_exactOffsetScales[index][frame0], _exactOffsetScales[index][frame1], fraction);
            var rotation = Vector4.LerpUnclamped(_exactRotations[index][frame0], _exactRotations[index][frame1], fraction);
            return VatMath.BonePoint(offsetScale, rotation, Pieces[index].Pivot, rest);
        }

        public Mesh BuildMesh(string name)
        {
            var subMeshes = new VatSubMeshes(Skin.Source);
            return VatBoneMeshBuilder.Build(name, Skin, VatRigidStream0.Build(Skin), subMeshes, _bounds.Build(Skin, subMeshes));
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
            var previous = VatSimilarity.Identity;
            for (var frame = 0; frame < frameCount; frame++)
            {
                if (piece.Visibility.IsVisible(frame))
                {
                    var similarity = Similarity(piece, frame);
                    var offset = piece.Motions[frame].MultiplyPoint3x4(piece.Pivot) - piece.Pivot;
                    poses[frame] = new Vector4(offset.x, offset.y, offset.z, similarity.Scale);
                    rotations[frame] = _signs.Align(index, aligned, similarity.RotationAfter(previous));
                    previous = rotations[frame];
                    aligned++;
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
            _exactOffsetScales[index] = new Vector4[frameCount];
            _exactRotations[index] = new Vector4[frameCount];
            for (var frame = 0; frame < frameCount; frame++)
            {
                var source = piece.Visibility.PoseFrame(frame);
                var pose = piece.Visibility.IsVisible(frame) ? poses[source] : Collapsed(piece, source);
                _exactOffsetScales[index][frame] = pose;
                _exactRotations[index][frame] = rotations[source];
                _texels.WritePose(index, Clip.StartRow + frame, pose, rotations[source], out var storedPose, out var storedRotation);
                _bounds.Add(index, piece.Pivot + (Vector3)storedPose, storedPose.w);
                if (piece.Visibility.IsVisible(frame))
                {
                    MeasureError(index, frame, storedPose, storedRotation);
                }
            }
        }

        private static Vector4 Collapsed(VatRigidPiece piece, int source)
        {
            var offset = piece.Motions[source].MultiplyPoint3x4(piece.Center) - piece.Pivot;
            return new Vector4(offset.x, offset.y, offset.z, 0f);
        }

        private void MeasureError(int index, int frame, Vector4 offsetScale, Vector4 rotation)
        {
            var piece = Pieces[index];
            var motion = piece.Motions[frame];
            foreach (var rest in piece.RestPositions)
            {
                MaxError = Mathf.Max(MaxError, Vector3.Distance(VatMath.BonePoint(offsetScale, rotation, piece.Pivot, rest), motion.MultiplyPoint3x4(rest)));
            }
        }
    }
}
