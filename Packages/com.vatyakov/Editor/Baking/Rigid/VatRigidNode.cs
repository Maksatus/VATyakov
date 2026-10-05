using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidNode
    {
        private const float MaxDeformation = 1e-5f;

        public readonly VatRigidTrack Track;

        private VatRigidIsland[] _islands;

        public bool IsSplit => _islands != null;
        public IReadOnlyList<VatRigidIsland> Islands => _islands ?? Array.Empty<VatRigidIsland>();

        public VatRigidNode(VatRigidTrack track)
        {
            Track = track;
        }

        public void Sample(int sample, IReadOnlyList<Vector3> positions, Matrix4x4 localToWorld, bool isVisible, double time)
        {
            Track.SetSample(sample, localToWorld, isVisible);
            var rest = Track.Local.Positions;
            if (_islands == null && Deforms(rest, positions))
            {
                Split(sample);
            }

            if (_islands == null)
            {
                return;
            }

            foreach (var island in _islands)
            {
                island.Fit(sample, rest, positions, localToWorld, time);
            }
        }

        public VatRigidTrack[] Pieces()
        {
            if (_islands == null)
            {
                return new[] { Track };
            }

            var groups = VatRigidIslandMerge.Group(_islands, Track);
            var pieces = new VatRigidTrack[groups.Count];
            for (var index = 0; index < pieces.Length; index++)
            {
                var name = pieces.Length == 1 ? Track.Name : FormattableString.Invariant($"{Track.Name}/{index}");
                pieces[index] = Piece(name, groups[index]);
            }

            return pieces;
        }

        private void Split(int sample)
        {
            var islands = VatRigidIslands.Find(Track.Source);
            _islands = new VatRigidIsland[islands.Length];
            for (var index = 0; index < islands.Length; index++)
            {
                _islands[index] = new VatRigidIsland(index, islands[index], Track.Local.Positions, Track.SampleCount);
                for (var held = 0; held < sample; held++)
                {
                    _islands[index].Hold(held);
                }
            }
        }

        private VatRigidTrack Piece(string name, VatRigidIslandGroup group)
        {
            var vertices = group.Vertices();
            var mesh = VatRigidSubset.Mesh(Track.Source, vertices, name);
            var piece = new VatRigidTrack(name, mesh, VatRigidSubset.Frame(Track.Local, vertices), Track.Frames.Length, Track.Inner.Length);
            for (var sample = 0; sample < Track.SampleCount; sample++)
            {
                piece.SetSample(sample, Track.Sample(sample) * group.Representative.Fits[sample], Track.IsVisibleAt(sample));
            }

            return piece;
        }

        private static bool Deforms(Vector3[] rest, IReadOnlyList<Vector3> positions)
        {
            for (var vertex = 0; vertex < rest.Length; vertex++)
            {
                if (Vector3.Distance(positions[vertex], rest[vertex]) > MaxDeformation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
