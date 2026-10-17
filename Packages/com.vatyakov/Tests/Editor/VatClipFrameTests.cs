using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatClipFrameTests
    {
        private const int StartRow = 100;

        [TestCase(0.0, 0f)]
        [TestCase(0.25, 0.25f)]
        [TestCase(1.0, 0f)]
        [TestCase(1.5, 0.5f)]
        [TestCase(-0.5, 0.5f)]
        public void LoopOfTwoFrames_WrapsToTheStart(double position, float fraction)
        {
            VatTestUtil.AssertFrame(Loop(2).Frame(position), StartRow, 0, 1, fraction);
        }

        [TestCase(-1.0, 0f)]
        [TestCase(0.0, 0f)]
        [TestCase(0.5, 0.5f)]
        [TestCase(1.0, 1f)]
        [TestCase(9.0, 1f)]
        public void OneShotOfTwoFrames_ClampsToBothEnds(double position, float fraction)
        {
            VatTestUtil.AssertFrame(OneShot(2).Frame(position), StartRow, 0, 1, fraction);
        }

        [Test]
        public void OneShot_ShowsTheClampedPosition([Values(2, 3, 26, 4096)] int frameCount)
        {
            var clip = OneShot(frameCount);
            for (var position = -2.0; position <= frameCount + 2.0; position += 0.125)
            {
                var frame = clip.Frame(position);
                Assert.AreEqual(Mathf.Clamp((float)position, 0f, frameCount - 1f), Shown(frame), 1e-6f * frameCount, $"position = {position}");
                Assert.AreEqual(frame.x + 1f, frame.y, "neighbouring frames");
            }

            VatTestUtil.AssertFrame(clip.Frame(frameCount - 1), StartRow, frameCount - 2, frameCount - 1, 1f);
        }

        [Test]
        public void Loop_BlendsNeighbouringFrames_AndItsSeamIsContinuous([Values(2, 3, 26, 31)] int frameCount)
        {
            var clip = Loop(frameCount);
            var intervals = frameCount - 1;
            for (var position = -2.0; position <= intervals * 2.0; position += 0.125)
            {
                var frame = clip.Frame(position);
                Assert.AreEqual(frame.x + 1f, frame.y, $"neighbouring frames at {position}");
            }

            var seam = Pose(clip.Frame(intervals), intervals);
            Assert.AreEqual(seam, Pose(clip.Frame(intervals - 1e-3), intervals), 1e-2f, "just before the seam");
            Assert.AreEqual(seam, Pose(clip.Frame(intervals + 1e-3), intervals), 1e-2f, "just after the seam");
        }

        [Test]
        public void Loop_StaysInsideTheClip_AtCycleBoundaries([Values(2, 3, 30, 4096)] int frameCount,
            [Values(1.0, 14400.0 * 30.0, 1e9)] double cycles)
        {
            var clip = Loop(frameCount);
            var boundary = (frameCount - 1) * cycles;
            foreach (var position in new[] { boundary, boundary - 1e-9, boundary + 1e-9, -boundary, -boundary - 1e-9 })
            {
                var frame = clip.Frame(position);
                Assert.That(frame.x, Is.InRange(StartRow, StartRow + frameCount - 2), $"position = {position:R}");
                Assert.That(frame.y, Is.InRange(StartRow + 1, StartRow + frameCount - 1), $"position = {position:R}");
                Assert.That(frame.z, Is.InRange(0f, 1f), $"position = {position:R}");
            }
        }

        private static VatClip Loop(int frameCount)
        {
            return new VatClip("Loop", StartRow, frameCount, 1f, frameCount - 1, isLooping: true);
        }

        private static VatClip OneShot(int frameCount)
        {
            return new VatClip("OneShot", StartRow, frameCount, 1f, frameCount - 1, isLooping: false);
        }

        private static float Shown(Vector4 frame)
        {
            return frame.x - StartRow + (frame.y - frame.x) * frame.z;
        }

        private static float Pose(Vector4 frame, int intervals)
        {
            var pose0 = Mathf.Sin(2f * Mathf.PI * (frame.x - StartRow) / intervals);
            var pose1 = Mathf.Sin(2f * Mathf.PI * (frame.y - StartRow) / intervals);
            return Mathf.Lerp(pose0, pose1, frame.z);
        }
    }
}
