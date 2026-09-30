using System;
using UnityEngine;

namespace Kefir.Vat
{
    /// <summary>
    /// CPU mirror of Shaders/VatCore.hlsl: addressing (§1.1), clip layout and frame clamp (§1.3),
    /// clip packing (§1.4). Keep both files in sync.
    /// </summary>
    public static class VatMath
    {
        /// <summary>Texture width and height limit (§1.1). The height limit also bounds clip packing (§1.4).</summary>
        public const int MaxTextureSize = 4096;

        const int RowShift = 12; // startRow·4096 + F
        const uint FrameMask = MaxTextureSize - 1;

        // --- §1.1 addressing -----------------------------------------------------------------

        /// <summary>Element blocks stacked vertically: ceil(E / 4096).</summary>
        public static int BlockCount(int elementCount)
        {
            if (elementCount < 1)
                throw new ArgumentOutOfRangeException(nameof(elementCount), elementCount, "Element count must be positive.");
            return (elementCount + MaxTextureSize - 1) / MaxTextureSize;
        }

        /// <summary>Texture width, not a power of two: W = ceil(E / ceil(E / 4096)).</summary>
        public static int TextureWidth(int elementCount)
        {
            int blocks = BlockCount(elementCount);
            return (elementCount + blocks - 1) / blocks;
        }

        /// <summary>
        /// Texel of an element in a row counted inside its block: b = id / W, x = id − b·W, y = b·totalRows + row.
        /// </summary>
        public static Vector2Int Texel(int element, int width, int totalRows, int row)
        {
            int block = element / width;
            return new Vector2Int(element - block * width, block * totalRows + row);
        }

        // --- §1.3 clip layout and frame index ------------------------------------------------

        /// <summary>Loop layout: F = max(1, round(L·fps)). The last frame does not repeat the first.</summary>
        public static int LoopFrameCount(float length, float fps)
        {
            ValidateTiming(length, fps);
            return Math.Max(1, (int)Math.Round((double)length * fps, MidpointRounding.AwayFromZero));
        }

        /// <summary>Loop playback rate in frames per second of clip time: fps_eff = F / L.</summary>
        public static float LoopFrameRate(int frameCount, float length) => (float)(frameCount / (double)length);

        /// <summary>Time of baked frame k of a loop clip: t_k = k·L / F.</summary>
        public static double LoopFrameTime(int frame, int frameCount, float length) => frame * (double)length / frameCount;

        /// <summary>Continuous frame position: f = (t − t0)·rate + offset, state = (packed, rate, t0, offset).</summary>
        public static float FramePosition(Vector4 state, float time) => (float)((float)(time - state.z) * state.y) + state.w;

        /// <summary>
        /// Loop frame clamp (§1.3, floor semantics): u = f − F·floor(f/F), f0 = clamp(floor(u), 0, F−1),
        /// frac = saturate(u − f0). The lower clamp matters: when f·(1/F) rounds up to an integer,
        /// u lands a few ulp below zero (for example F = 3, f = 3072 − ulp).
        /// </summary>
        public static int LoopFrame(float f, int frameCount, out float frac)
        {
            float count = frameCount;
            float cycles = Mathf.Floor((float)(f * (float)(1f / count)));
            float u = (float)(f - (float)(count * cycles));
            float f0 = Mathf.Clamp(Mathf.Floor(u), 0f, count - 1f);
            frac = Mathf.Clamp01((float)(u - f0));
            return (int)f0;
        }

        // --- §1.4 clip packing ----------------------------------------------------------------

        /// <summary>
        /// Packs a clip into _VatClip*.x: ±(startRow·4096 + F), plus for loop, minus for one-shot.
        /// The value lies in [1, 2^24], so float32 stores it exactly and the sign is never ambiguous.
        /// </summary>
        public static float PackClip(int startRow, int frameCount, bool loop)
        {
            if ((uint)startRow >= MaxTextureSize)
                throw new ArgumentOutOfRangeException(nameof(startRow), startRow, "Start row must be in [0, 4095].");
            if (frameCount < 1 || frameCount > MaxTextureSize)
                throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount, "Frame count must be in [1, 4096].");
            float packed = (startRow << RowShift) + frameCount;
            return loop ? packed : -packed;
        }

        /// <summary>Decodes _VatClip*.x: p = |x| − 1, startRow = p >> 12, F = (p &amp; 4095) + 1.</summary>
        public static void UnpackClip(float packed, out int startRow, out int frameCount, out bool loop)
        {
            uint p = (uint)Math.Abs(packed) - 1u;
            startRow = (int)(p >> RowShift);
            frameCount = (int)(p & FrameMask) + 1;
            loop = packed > 0f;
        }

        // --- helpers --------------------------------------------------------------------------

        public static bool IsFinite(Vector4 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z) && float.IsFinite(v.w);

        static void ValidateTiming(float length, float fps)
        {
            if (!float.IsFinite(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Clip length must be a positive finite number.");
            if (!float.IsFinite(fps) || fps <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fps), fps, "Bake fps must be a positive finite number.");
        }
    }
}
