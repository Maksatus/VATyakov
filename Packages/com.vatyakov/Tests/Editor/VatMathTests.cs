using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatMathTests
    {
        [TestCase(1, 1, 1)]
        [TestCase(2079, 1, 2079)]
        [TestCase(4096, 1, 4096)]
        [TestCase(4097, 2, 2049)]
        [TestCase(4529, 2, 2265)]
        [TestCase(8192, 2, 4096)]
        [TestCase(8193, 3, 2731)]
        [TestCase(100000, 25, 4000)]
        public void Width_IsElementsOverBlocks_NotAPowerOfTwo(int elements, int blocks, int width)
        {
            Assert.AreEqual(blocks, VatMath.BlockCount(elements));
            Assert.AreEqual(width, VatMath.TextureWidth(elements));
            Assert.LessOrEqual(width, VatMath.MaxTextureSize);
            Assert.GreaterOrEqual(blocks * width, elements);
            Assert.Less((blocks - 1) * width, elements, "no empty block");
        }

        [Test]
        public void Texel_MapsEveryElementAndRowToAUniqueTexel([Values(1, 2079, 4097, 8193)] int elements)
        {
            const int totalRows = 7;
            int width = VatMath.TextureWidth(elements);
            int height = VatMath.BlockCount(elements) * totalRows;
            var seen = new HashSet<Vector2Int>();
            for (int row = 0; row < totalRows; row++)
            {
                for (int element = 0; element < elements; element++)
                {
                    var texel = VatMath.Texel(element, width, totalRows, row);
                    Assert.That(texel.x, Is.InRange(0, width - 1));
                    Assert.That(texel.y, Is.InRange(0, height - 1));
                    Assert.AreEqual(row, texel.y % totalRows);
                    Assert.IsTrue(seen.Add(texel), $"texel {texel} used twice");
                }
            }
        }

        [TestCase(0, 1, true)]
        [TestCase(0, 1, false)]
        [TestCase(0, 4096, true)]
        [TestCase(4095, 1, true)]
        [TestCase(4095, 4096, false)] // |x| = 2^24
        [TestCase(123, 45, false)]
        public void PackClip_RoundTripsExactly(int startRow, int frames, bool loop)
        {
            float packed = VatMath.PackClip(startRow, frames, loop);

            Assert.AreEqual(loop, packed > 0f);
            Assert.AreEqual(startRow * 4096.0 + frames, Math.Abs((double)packed), "exact in float32");
            VatMath.UnpackClip(packed, out int decodedRow, out int decodedFrames, out bool decodedLoop);
            Assert.AreEqual(startRow, decodedRow);
            Assert.AreEqual(frames, decodedFrames);
            Assert.AreEqual(loop, decodedLoop);
        }

        [TestCase(-1, 1)]
        [TestCase(4096, 1)]
        [TestCase(0, 0)]
        [TestCase(0, 4097)]
        public void PackClip_RejectsValuesOutsideTheLayout(int startRow, int frames)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VatMath.PackClip(startRow, frames, true));
        }

        // Frame counts × cycle counts whose product kF is still an exact float (< 2^24).
        // 30 × 14400 is a 4 hour session at 30 fps.
        static IEnumerable<TestCaseData> CycleBoundaries()
        {
            foreach (int frames in new[] { 1, 2, 3, 7, 25, 30, 4096 })
                foreach (int cycles in new[] { 1, 2, 1024, 4095, 14400, 100000 })
                    if ((long)frames * cycles < 1 << 24)
                        yield return new TestCaseData(frames, cycles);
        }

        [TestCaseSource(nameof(CycleBoundaries))]
        public void LoopFrame_StaysInRange_AtCycleBoundaries(int frames, int cycles)
        {
            float k = frames * cycles;

            foreach (float f in new[] { -1e-7f, -k, k, NextUp(k), NextDown(k), NextUp(-k), NextDown(-k) })
            {
                int f0 = VatMath.LoopFrame(f, frames, out float frac);

                Assert.That(f0, Is.InRange(0, frames - 1), $"f = {f:R}");
                Assert.That(frac, Is.InRange(0f, 1f), $"f = {f:R}");

                // (f0 + frac) is the loop phase; exact math in double, compared on the circle of length F.
                // Near a cycle boundary the float result may land on the other side of it: off by the
                // distance to the boundary, i.e. by the precision of f itself.
                double phase = f - frames * Math.Floor(f / (double)frames);
                double distance = Math.Abs(f0 + frac - phase);
                distance = Math.Min(distance, frames - distance);
                double tolerance = Math.Max(1e-4, 2.0 * (NextUp(Math.Abs(f)) - Math.Abs(f)));
                Assert.That(distance, Is.LessThanOrEqualTo(tolerance), $"f = {f:R}: f0 = {f0}, frac = {frac:R}, phase = {phase:R}");
            }
        }

        [Test]
        public void LoopFrame_JustBeforeZero_IsTheLastFrame([Values(1, 2, 3, 25, 30, 4096)] int frames)
        {
            Assert.AreEqual(frames - 1, VatMath.LoopFrame(-1e-7f, frames, out _));
        }

        [Test]
        public void LoopFrame_ExactFrameIndices_MapToThemselves([Values(1, 3, 30)] int frames)
        {
            for (int frame = 0; frame < frames; frame++)
            {
                Assert.AreEqual(frame, VatMath.LoopFrame(frame, frames, out float frac));
                Assert.AreEqual(0f, frac);
            }
        }

        [TestCase(0.8333334f, 30f, 25)]
        [TestCase(1f, 30f, 30)]
        [TestCase(0.01f, 30f, 1)] // L·fps < 0.5 still gives one frame
        public void LoopFrameCount_RoundsLengthTimesFps(float length, float fps, int expected)
        {
            Assert.AreEqual(expected, VatMath.LoopFrameCount(length, fps));
        }

        [TestCase(0f, 30f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 30f)]
        [TestCase(float.NaN, 30f)]
        [TestCase(1f, float.PositiveInfinity)]
        public void LoopFrameCount_RejectsInvalidTiming(float length, float fps)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VatMath.LoopFrameCount(length, fps));
        }

        static float NextUp(float x) => Step(x, +1);

        static float NextDown(float x) => Step(x, -1);

        static float Step(float x, int direction)
        {
            if (x == 0f)
                return direction > 0 ? float.Epsilon : -float.Epsilon;
            int bits = BitConverter.ToInt32(BitConverter.GetBytes(x), 0);
            bits += (x > 0f) == (direction > 0) ? 1 : -1;
            return BitConverter.ToSingle(BitConverter.GetBytes(bits), 0);
        }
    }
}
