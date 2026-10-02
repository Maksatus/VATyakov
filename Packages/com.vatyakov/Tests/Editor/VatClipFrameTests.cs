using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatClipFrameTests
    {
        private const int StartRow = 100;

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
        [TestCase(1.5, 1, 0, 0.5f)]
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

        [Test]
        public void OneShot_ShowsTheClampedPosition([Values(2, 3, 26, 4096)] int frameCount)
        {
            var clip = OneShot(frameCount);
            for (var f = -2.0; f <= frameCount + 2.0; f += 0.125)
            {
                var frame = clip.Frame(f);
                Assert.AreEqual(Mathf.Clamp((float)f, 0f, frameCount - 1f), Shown(frame), 1e-6f * frameCount, $"f = {f}");
                Assert.AreEqual(frame.x + 1f, frame.y, "neighbouring frames, no wrap");
            }

            AssertFrame(clip.Frame(frameCount - 1), frameCount - 2, frameCount - 1, 1f);
        }

        [Test]
        public void Loop_SeamIsContinuous([Values(2, 3, 25, 30)] int frameCount)
        {
            var clip = Loop(frameCount);
            var at = Pose(clip.Frame(frameCount), frameCount);
            Assert.AreEqual(at, Pose(clip.Frame(frameCount - 1e-3), frameCount), 1e-2f);
            Assert.AreEqual(at, Pose(clip.Frame(frameCount + 1e-3), frameCount), 1e-2f);
            Assert.AreEqual(Pose(clip.Frame(0.0), frameCount), at, 1e-6f, "the seam is frame 0");
        }

        [Test]
        public void Loop_StaysInsideTheClip_AtCycleBoundaries([Values(1, 3, 30, 4096)] int frameCount,
            [Values(1.0, 14400.0 * 30.0, 1e9)] double cycles)
        {
            var clip = Loop(frameCount);
            var k = frameCount * cycles;
            foreach (var f in new[] { k, k - 1e-9, k + 1e-9, -k, -k - 1e-9 })
            {
                var frame = clip.Frame(f);
                Assert.That(frame.x, Is.InRange(StartRow, StartRow + frameCount - 1), $"f = {f:R}");
                Assert.That(frame.y, Is.InRange(StartRow, StartRow + frameCount - 1), $"f = {f:R}");
                Assert.That(frame.z, Is.InRange(0f, 1f), $"f = {f:R}");
            }
        }

        internal static void AssertFrame(Vector4 frame, int f0, int f1, float frac)
        {
            Assert.AreEqual(StartRow + f0, frame.x, $"row0 of {frame}");
            Assert.AreEqual(StartRow + f1, frame.y, $"row1 of {frame}");
            Assert.AreEqual(frac, frame.z, 1e-5f, $"frac of {frame}");
            Assert.AreEqual(0f, frame.w);
        }

        private static VatClip Loop(int frameCount)
        {
            return new VatClip("Loop", StartRow, frameCount, 1f, frameCount, loop: true);
        }

        private static VatClip OneShot(int frameCount)
        {
            return new VatClip("OneShot", StartRow, frameCount, 1f, frameCount - 1, loop: false);
        }

        private static float Shown(Vector4 frame)
        {
            return frame.x - StartRow + (frame.y - frame.x) * frame.z;
        }

        private static float Pose(Vector4 frame, int frameCount)
        {
            var a = Mathf.Sin(2f * Mathf.PI * (frame.x - StartRow) / frameCount);
            var b = Mathf.Sin(2f * Mathf.PI * (frame.y - StartRow) / frameCount);
            return Mathf.Lerp(a, b, frame.z);
        }
    }
}
