using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatRigidIslandMerge
    {
        public static List<VatRigidIslandGroup> Group(IReadOnlyList<VatRigidIsland> islands, VatRigidNode node)
        {
            var ordered = new List<VatRigidIsland>(islands);
            ordered.Sort(ByVertexCount);
            var groups = new List<VatRigidIslandGroup>();
            foreach (var island in ordered)
            {
                var group = groups.Find(candidate => Matches(candidate.Representative, island, node));
                if (group == null)
                {
                    groups.Add(new VatRigidIslandGroup(island));
                }
                else
                {
                    group.Add(island);
                }
            }

            groups.Sort((a, b) => a.FirstIndex.CompareTo(b.FirstIndex));
            return groups;
        }

        private static int ByVertexCount(VatRigidIsland a, VatRigidIsland b)
        {
            var count = b.Vertices.Length.CompareTo(a.Vertices.Length);
            return count != 0 ? count : a.Index.CompareTo(b.Index);
        }

        private static bool Matches(VatRigidIsland representative, VatRigidIsland island, VatRigidNode node)
        {
            var budget = VatRigidFormat.MaxIslandResidual - island.Residual;
            var track = node.Track;
            for (var sample = track.SampleCount - 1; sample >= 0; sample--)
            {
                if (node.IsFitted(sample) && Gap(representative, island, track, sample, island.Center) > budget)
                {
                    return false;
                }
            }

            var rest = track.Local.Positions;
            for (var sample = track.SampleCount - 1; sample >= 0; sample--)
            {
                if (!node.IsFitted(sample))
                {
                    continue;
                }

                foreach (var vertex in island.Vertices)
                {
                    if (Gap(representative, island, track, sample, rest[vertex]) > budget)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static float Gap(VatRigidIsland representative, VatRigidIsland island, VatRigidTrack track, int sample, Vector3 point)
        {
            var difference = representative.Fits[sample].MultiplyPoint3x4(point) - island.Fits[sample].MultiplyPoint3x4(point);
            return track.Sample(sample).MultiplyVector(difference).magnitude;
        }
    }
}
