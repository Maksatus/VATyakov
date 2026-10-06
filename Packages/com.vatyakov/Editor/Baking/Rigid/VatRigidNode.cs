using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidNode
    {
        private const float MaxDeformation = 1e-5f;

        public readonly VatRigidTrack Track;

        private readonly double[] _times;
        private readonly bool[] _isFitted;

        private VatRigidIsland[] _islands;

        public bool IsSplit => _islands != null;
        public IReadOnlyList<VatRigidIsland> Islands => _islands ?? Array.Empty<VatRigidIsland>();

        public VatRigidNode(VatRigidTrack track)
        {
            Track = track;
            _times = new double[track.SampleCount];
            _isFitted = new bool[track.SampleCount];
        }

        public bool IsFitted(int sample)
        {
            return _isFitted[sample];
        }

        public void Sample(int sample, IReadOnlyList<Vector3> positions, Matrix4x4 localToWorld, bool isVisible, double time, bool isSourceSample)
        {
            Track.SetSample(sample, localToWorld, isVisible);
            _times[sample] = time;
            _isFitted[sample] = isSourceSample;
            var rest = Track.Local.Positions;
            if (_islands == null && Deforms(rest, positions))
            {
                Split(sample);
            }

            if (_islands == null || !isSourceSample)
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

            var groups = VatRigidIslandMerge.Group(_islands, this);
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

            Array.Fill(_isFitted, true, 0, sample);
        }

        private VatRigidTrack Piece(string name, VatRigidIslandGroup group)
        {
            var vertices = group.Vertices();
            var mesh = VatRigidSubset.Mesh(Track.Source, vertices, name);
            var piece = new VatRigidTrack(name, mesh, VatRigidSubset.Frame(Track.Local, vertices), Track.Frames.Length, Track.Inner.Length);
            var fits = VatRigidFitBlend.Fill(group.Representative.Fits, Center(vertices), _times, _isFitted);
            for (var sample = 0; sample < Track.SampleCount; sample++)
            {
                piece.SetSample(sample, Track.Sample(sample) * fits[sample], Track.IsVisibleAt(sample));
            }

            return piece;
        }

        private Vector3 Center(int[] vertices)
        {
            return VatCentroid.Of(Array.ConvertAll(vertices, vertex => Track.Local.Positions[vertex]));
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
