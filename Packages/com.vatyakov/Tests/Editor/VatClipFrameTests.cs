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
            Assert.AreEqual(StartRow, frame.x, "row0 is frame 0");
            Assert.AreEqual(StartRow, frame.y, "row1 is frame 0");
        }

        [TestCase(0.0, 0, 1, 0f)]
        [TestCase(0.25, 0, 1, 0.25f)]
        [TestCase(1.0, 1, 0, 0f)]
        [TestCase(1.5, 1, 0, 0.5f)]
        [TestCase(2.0, 0, 1, 0f)]
        [TestCase(-0.5, 1, 0, 0.5f)]
        public void LoopOfTwoFrames_WrapsTheSecondFrame(double position, int frame0, int frame1, float fraction)
        {
            VatTestUtil.AssertFrame(Loop(2).Frame(position), StartRow, frame0, frame1, fraction);
        }

        [Test]
        public void OneShotOfOneFrame_AlwaysShowsFrameZero([Values(-5.0, 0.0, 0.5, 7.0)] double position)
        {
            VatTestUtil.AssertFrame(OneShot(1).Frame(position), StartRow, 0, 0, 0f);
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
                Assert.AreEqual(frame.x + 1f, frame.y, "neighbouring frames, no wrap");
            }

            VatTestUtil.AssertFrame(clip.Frame(frameCount - 1), StartRow, frameCount - 2, frameCount - 1, 1f);
        }

        [Test]
        public void Loop_SeamIsContinuous([Values(2, 3, 25, 30)] int frameCount)
        {
            var clip = Loop(frameCount);
            var seam = Pose(clip.Frame(frameCount), frameCount);
            Assert.AreEqual(seam, Pose(clip.Frame(frameCount - 1e-3), frameCount), 1e-2f, "just before the seam");
            Assert.AreEqual(seam, Pose(clip.Frame(frameCount + 1e-3), frameCount), 1e-2f, "just after the seam");
            Assert.AreEqual(Pose(clip.Frame(0.0), frameCount), seam, 1e-6f, "the seam is frame 0");
        }

        [Test]
        public void Loop_StaysInsideTheClip_AtCycleBoundaries([Values(1, 3, 30, 4096)] int frameCount,
            [Values(1.0, 14400.0 * 30.0, 1e9)] double cycles)
        {
            var clip = Loop(frameCount);
            var boundary = frameCount * cycles;
            foreach (var position in new[] { boundary, boundary - 1e-9, boundary + 1e-9, -boundary, -boundary - 1e-9 })
            {
                var frame = clip.Frame(position);
                Assert.That(frame.x, Is.InRange(StartRow, StartRow + frameCount - 1), $"position = {position:R}");
                Assert.That(frame.y, Is.InRange(StartRow, StartRow + frameCount - 1), $"position = {position:R}");
                Assert.That(frame.z, Is.InRange(0f, 1f), $"position = {position:R}");
            }
        }

        private static VatClip Loop(int frameCount)
        {
            return new VatClip("Loop", StartRow, frameCount, 1f, frameCount, isLooping: true);
        }

        private static VatClip OneShot(int frameCount)
        {
            return new VatClip("OneShot", StartRow, frameCount, 1f, frameCount - 1, isLooping: false);
        }

        private static float Shown(Vector4 frame)
        {
            return frame.x - StartRow + (frame.y - frame.x) * frame.z;
        }

        private static float Pose(Vector4 frame, int frameCount)
        {
            var pose0 = Mathf.Sin(2f * Mathf.PI * (frame.x - StartRow) / frameCount);
            var pose1 = Mathf.Sin(2f * Mathf.PI * (frame.y - StartRow) / frameCount);
            return Mathf.Lerp(pose0, pose1, frame.z);
        }
    }
}
