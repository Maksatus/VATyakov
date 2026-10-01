using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    // §1.3, §1.4: _VatFrame = (row0, row1, frac, 0) that the CPU writes for a frame position.
    public class VatClipFrameTests
    {
        const int StartRow = 100;

        [Test]
        public void LoopOfOneFrame_AlwaysShowsFrameZero([Values(-3.5, -1e-7, 0.0, 0.25, 0.999, 17.0)] double position)
        {
            var frame = Loop(1).Frame(position);
            Assert.AreEqual(StartRow, frame.x);
            Assert.AreEqual(StartRow, frame.y);
        }

        [TestCase(0.0, 0, 1, 0f)]
        [TestCase(0.25, 0, 1, 0.25f)]
        [TestCase(1.0, 1, 0, 0f)]
        [TestCase(1.5, 1, 0, 0.5f)] // the seam: last frame towards the first
        [TestCase(2.0, 0, 1, 0f)]
        [TestCase(-0.5, 1, 0, 0.5f)]
        public void LoopOfTwoFrames_WrapsTheSecondFrame(double position, int f0, int f1, float frac)
        {
            AssertFrame(Loop(2).Frame(position), f0, f1, frac);
        }

        [Test]
        public void OneShotOfOneFrame_AlwaysShowsFrameZero([Values(-5.0, 0.0, 0.5, 7.0)] double position)
        {
            AssertFrame(OneShot(1).Frame(position), 0, 0, 0f);
        }

        [TestCase(-1.0, 0f)]
        [TestCase(0.0, 0f)]
        [TestCase(0.5, 0.5f)]
        [TestCase(1.0, 1f)]
        [TestCase(9.0, 1f)]
        public void OneShotOfTwoFrames_ClampsToBothEnds(double position, float frac)
        {
            AssertFrame(OneShot(2).Frame(position), 0, 1, frac);
        }

        // Shown position f0 + frac is clamp(f, 0, F − 1): no wrap, the last frame is exact.
        [Test]
        public void OneShot_ShowsTheClampedPosition([Values(2, 3, 26, 4096)] int frameCount)
        {
            var clip = OneShot(frameCount);
            for (double f = -2.0; f <= frameCount + 2.0; f += 0.125)
            {
                var frame = clip.Frame(f);
                Assert.AreEqual(Mathf.Clamp((float)f, 0f, frameCount - 1f), Shown(frame), 1e-6f * frameCount, $"f = {f}");
                Assert.AreEqual(frame.x + 1f, frame.y, "neighbouring frames, no wrap");
            }
            AssertFrame(clip.Frame(frameCount - 1), frameCount - 2, frameCount - 1, 1f);
        }

        // A smooth periodic signal sampled per frame stays continuous across the seam.
        [Test]
        public void Loop_SeamIsContinuous([Values(2, 3, 25, 30)] int frameCount)
        {
            var clip = Loop(frameCount);
            float at = Pose(clip.Frame(frameCount), frameCount);
            Assert.AreEqual(at, Pose(clip.Frame(frameCount - 1e-3), frameCount), 1e-2f);
            Assert.AreEqual(at, Pose(clip.Frame(frameCount + 1e-3), frameCount), 1e-2f);
            Assert.AreEqual(Pose(clip.Frame(0.0), frameCount), at, 1e-6f, "the seam is frame 0");
        }

        // Positions of a 4 hour session at 30 fps and beyond: rows stay inside the clip.
        [Test]
        public void Loop_StaysInsideTheClip_AtCycleBoundaries([Values(1, 3, 30, 4096)] int frameCount,
            [Values(1.0, 14400.0 * 30.0, 1e9)] double cycles)
        {
            var clip = Loop(frameCount);
            double k = frameCount * cycles;
            foreach (double f in new[] { k, k - 1e-9, k + 1e-9, -k, -k - 1e-9 })
            {
                var frame = clip.Frame(f);
                Assert.That(frame.x, Is.InRange(StartRow, StartRow + frameCount - 1), $"f = {f:R}");
                Assert.That(frame.y, Is.InRange(StartRow, StartRow + frameCount - 1), $"f = {f:R}");
                Assert.That(frame.z, Is.InRange(0f, 1f), $"f = {f:R}");
            }
        }

        static VatClip Loop(int frameCount) => new VatClip("Loop", StartRow, frameCount, 1f, frameCount, loop: true);

        static VatClip OneShot(int frameCount) => new VatClip("OneShot", StartRow, frameCount, 1f, frameCount - 1, loop: false);

        static float Shown(Vector4 frame) => frame.x - StartRow + (frame.y - frame.x) * frame.z;

        static float Pose(Vector4 frame, int frameCount)
        {
            float a = Mathf.Sin(2f * Mathf.PI * (frame.x - StartRow) / frameCount);
            float b = Mathf.Sin(2f * Mathf.PI * (frame.y - StartRow) / frameCount);
            return Mathf.Lerp(a, b, frame.z);
        }

        internal static void AssertFrame(Vector4 frame, int f0, int f1, float frac)
        {
            Assert.AreEqual(StartRow + f0, frame.x, $"row0 of {frame}");
            Assert.AreEqual(StartRow + f1, frame.y, $"row1 of {frame}");
            Assert.AreEqual(frac, frame.z, 1e-5f, $"frac of {frame}");
            Assert.AreEqual(0f, frame.w);
        }
    }
}
