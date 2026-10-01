using System;
using UnityEngine;

namespace VATyakov
{
    // CPU mirror of Shaders/VatCore.hlsl — change both together. Float casts reproduce GPU float math.
    public static class VatMath
    {
        // Texture size limit (§1.1); the height limit also bounds clip packing (§1.4).
        public const int MaxTextureSize = 4096;

        const int RowShift = 12;
        const uint FrameMask = MaxTextureSize - 1;

        // §1.1: blocks = ceil(E / 4096).
        public static int BlockCount(int elementCount)
        {
            if (elementCount < 1)
                throw new ArgumentOutOfRangeException(nameof(elementCount), elementCount, "Element count must be positive.");
            return (elementCount + MaxTextureSize - 1) / MaxTextureSize;
        }

        // §1.1: W = ceil(E / blocks), not a power of two.
        public static int TextureWidth(int elementCount)
        {
            int blocks = BlockCount(elementCount);
            return (elementCount + blocks - 1) / blocks;
        }

        // §1.1: b = id / W, x = id − b·W, y = b·totalRows + row.
        public static Vector2Int Texel(int element, int width, int totalRows, int row)
        {
            int block = element / width;
            return new Vector2Int(element - block * width, block * totalRows + row);
        }

        // §1.3 loop: F = max(1, round(L·fps)), the last frame does not repeat the first.
        public static int LoopFrameCount(float length, float fps)
        {
            ValidateTiming(length, fps);
            return Math.Max(1, (int)Math.Round((double)length * fps, MidpointRounding.AwayFromZero));
        }

        // fps_eff = F / L.
        public static float LoopFrameRate(int frameCount, float length) => (float)(frameCount / (double)length);

        // t_k = k·L / F.
        public static double LoopFrameTime(int frame, int frameCount, float length) => frame * (double)length / frameCount;

        // f = (t − t0)·rate + offset.
        public static float FramePosition(Vector4 state, float time) => (float)((float)(time - state.z) * state.y) + state.w;

        // §1.3 loop clamp. The lower clamp matters: when f·(1/F) rounds up to an integer,
        // u lands a few ulp below zero (F = 3, f = 3072 − ulp).
        public static int LoopFrame(float f, int frameCount, out float frac)
        {
            float count = frameCount;
            float cycles = Mathf.Floor((float)(f * (float)(1f / count)));
            float u = (float)(f - (float)(count * cycles));
            float f0 = Mathf.Clamp(Mathf.Floor(u), 0f, count - 1f);
            frac = Mathf.Clamp01((float)(u - f0));
            return (int)f0;
        }

        // §1.4: ±(startRow·4096 + F) ∈ [1, 2^24] — exact in float32, sign never ambiguous.
        public static float PackClip(int startRow, int frameCount, bool loop)
        {
            RequirePackable(startRow, frameCount);
            float packed = (startRow << RowShift) + frameCount;
            return loop ? packed : -packed;
        }

        public static void UnpackClip(float packed, out int startRow, out int frameCount, out bool loop)
        {
            uint p = (uint)Math.Abs(packed) - 1u;
            startRow = (int)(p >> RowShift);
            frameCount = (int)(p & FrameMask) + 1;
            loop = packed > 0f;
        }

        public static bool IsFinite(Vector4 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z) && float.IsFinite(v.w);

        static void RequirePackable(int startRow, int frameCount)
        {
            if ((uint)startRow >= MaxTextureSize)
                throw new ArgumentOutOfRangeException(nameof(startRow), startRow, "Start row must be in [0, 4095].");
            if (frameCount < 1 || frameCount > MaxTextureSize)
                throw new ArgumentOutOfRangeException(nameof(frameCount), frameCount, "Frame count must be in [1, 4096].");
        }

        static void ValidateTiming(float length, float fps)
        {
            if (!float.IsFinite(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length), length, "Clip length must be a positive finite number.");
            if (!float.IsFinite(fps) || fps <= 0f)
                throw new ArgumentOutOfRangeException(nameof(fps), fps, "Bake fps must be a positive finite number.");
        }
    }
}
