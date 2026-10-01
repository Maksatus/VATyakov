using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: loop detection is only a hint, the user decides. A clip loops when its end repeats its start.
    static class VatLoopGap
    {
        public const float MaxLoopGap = 1e-3f; // meters, the frame error budget

        // Max vertex distance between the clip start and its end.
        public static float Measure(IVatFrameSource source, int clip)
        {
            var start = new VatFrame(source.Mesh.VertexCount);
            var end = new VatFrame(source.Mesh.VertexCount);
            source.Sample(clip, 0.0, start);
            source.Sample(clip, source.Clips[clip].Length, end);
            float gap = 0f;
            for (int v = 0; v < start.Positions.Length; v++)
                gap = Mathf.Max(gap, Vector3.Distance(start.Positions[v], end.Positions[v]));
            return gap;
        }
    }
}
