using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidPiece
    {
        private const float MinDeterminant = 1e-12f;

        public readonly VatRigidTrack Track;
        public readonly VatRigidVisibility Visibility;
        public readonly Matrix4x4[] Motions;
        public readonly Vector3[] RestPositions;
        public readonly Vector3 Pivot;

        private readonly Matrix4x4 _rest;
        private readonly Matrix4x4 _restInverse;

        public string Name => Track.Name;
        public int VertexCount => Track.Source.VertexCount;

        public VatRigidPiece(VatRigidTrack track, VatRigidVisibility visibility)
        {
            Track = track;
            Visibility = visibility;
            _rest = track.Frames[visibility.First];
            if (!(Mathf.Abs(_rest.determinant) > MinDeterminant))
            {
                throw new VatBakeException(FormattableString.Invariant($"Piece '{track.Name}' has zero scale on frame {visibility.First}, its first visible frame."));
            }

            _restInverse = _rest.inverse;
            Motions = Array.ConvertAll(track.Frames, frame => frame * _restInverse);
            RestPositions = Array.ConvertAll(track.Local.Positions, _rest.MultiplyPoint3x4);
            Pivot = new VatHalf3(VatRigidPivot.Solve(Motions, visibility, Center(RestPositions))).ToVector3();
        }

        public Matrix4x4 InnerMotion(int sample)
        {
            return Track.Inner[sample] * _restInverse;
        }

        public void WriteRest(VatFrame frame, int offset)
        {
            var space = new VatRootSpace(_rest);
            var local = Track.Local;
            for (var vertex = 0; vertex < RestPositions.Length; vertex++)
            {
                frame.Positions[offset + vertex] = RestPositions[vertex];
                frame.Normals[offset + vertex] = space.Normal(local.Normals[vertex]);
                frame.Tangents[offset + vertex] = space.Tangent(local.Tangents[vertex]);
            }
        }

        private static Vector3 Center(Vector3[] points)
        {
            var bounds = new VatBoundsBuilder();
            foreach (var point in points)
            {
                bounds.Add(point);
            }

            return bounds.Bounds.center;
        }
    }
}
