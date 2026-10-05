using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VATyakov.Editor
{
    internal static class VatRigidSplit
    {
        private const int ListedIslands = 10;
        private const float MillimetersPerMeter = 1000f;

        public static VatRigidTrack[] Pieces(IReadOnlyList<VatRigidNode> nodes, string name)
        {
            var failures = nodes
                .SelectMany(node => node.Islands.Where(island => island.Residual > VatRigidFormat.MaxIslandResidual).Select(island => (node, island)))
                .OrderByDescending(failure => failure.island.Residual)
                .ToList();
            if (failures.Count > 0)
            {
                throw new VatBakeException(Report(name, failures, nodes.Sum(node => node.Islands.Count)));
            }

            return nodes.SelectMany(node => node.Pieces()).ToArray();
        }

        private static string Report(string name, List<(VatRigidNode node, VatRigidIsland island)> failures, int islandCount)
        {
            var limit = VatRigidFormat.MaxIslandResidual * MillimetersPerMeter;
            var report = new StringBuilder(FormattableString.Invariant(
                $"'{name}': {failures.Count} of {islandCount} islands of deforming meshes don't move rigidly (residual over {limit:0.###} mm)."));
            report.Append(" Rigid mode needs every connected part of a mesh to move as a whole: use Mode = Vertex for deforming meshes.");
            foreach (var (node, island) in failures.Take(ListedIslands))
            {
                var center = node.Track.Frames[0].MultiplyPoint3x4(island.Center);
                var residual = island.Residual * MillimetersPerMeter;
                report.Append(FormattableString.Invariant($"\n'{node.Track.Name}' island {island.Index}: {island.Vertices.Length} vertices"));
                report.Append(FormattableString.Invariant($" around ({center.x:0.###}, {center.y:0.###}, {center.z:0.###}),"));
                report.Append(FormattableString.Invariant($" {residual:0.###} mm at {island.ResidualTime:0.###} s"));
            }

            if (failures.Count > ListedIslands)
            {
                report.Append(FormattableString.Invariant($"\n... and {failures.Count - ListedIslands} more."));
            }

            return report.ToString();
        }
    }
}
