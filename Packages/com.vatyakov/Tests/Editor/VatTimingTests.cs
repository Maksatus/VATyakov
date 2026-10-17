using NUnit.Framework;

namespace VATyakov.Tests
{
    public class VatTimingTests
    {
        private const float Fps = 30f;
        private const double RateTolerance = 1e-4;
        private const double TimeTolerance = 1e-9;

        [TestCase(0.8333334f, Fps, 26)]
        [TestCase(1f, Fps, 31)]
        [TestCase(1f, 10f, 11)]
        [TestCase(0.01f, Fps, 2)]
        [TestCase(0.02f, Fps, 2)]
        public void FrameCount_IsEveryFrameOfTheClip_WithTheEnd(float length, float fps, int expected)
        {
            Assert.AreEqual(expected, VatTiming.FrameCount(length, fps), "frame count");
        }

        [Test]
        public void LastFrame_IsTheClipEnd([Values(0.01f, 0.8333334f, 1f)] float length)
        {
            var frameCount = VatTiming.FrameCount(length, Fps);
            var rate = VatTiming.FrameRate(frameCount, length);

            Assert.AreEqual((frameCount - 1) / (double)length, rate, RateTolerance, "fps_eff = (F − 1) / L");
            Assert.AreEqual(0.0, VatTiming.FrameTime(0, frameCount, length), 0.0, "frame 0 is the clip start");
            Assert.AreEqual(length, VatTiming.FrameTime(frameCount - 1, frameCount, length), TimeTolerance, "the last frame is the clip end");
        }
    }
}
