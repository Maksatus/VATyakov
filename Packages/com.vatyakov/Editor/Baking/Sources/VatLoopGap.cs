using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatLoopGap
    {
        public const float Max = 1e-3f;

        public static float Measure(IVatFrameSource source, int clip)
        {
            var start = new VatFrame(source.Mesh.VertexCount);
            var end = new VatFrame(source.Mesh.VertexCount);
            source.Sample(clip, 0.0, start);
            source.Sample(clip, source.Clips[clip].Length, end);
            var gap = 0f;
            for (var vertex = 0; vertex < start.Positions.Length; vertex++)
            {
                gap = Mathf.Max(gap, Vector3.Distance(start.Positions[vertex], end.Positions[vertex]));
            }

            return gap;
        }
    }
}
