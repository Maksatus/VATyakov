using System;
using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatTimingTests
    {
        private const float Fps = 30f;
        private const float HugeLength = 1e9f;
        private const float HugeFps = 1e6f;
        private const double RateTolerance = 1e-4;
        private const double TimeTolerance = 1e-9;

        [TestCase(0.8333334f, Fps, 25)]
        [TestCase(1f, Fps, 30)]
        [TestCase(0.01f, Fps, 1)]
        public void LoopFrameCount_RoundsLengthTimesFps(float length, float fps, int expected)
        {
            Assert.AreEqual(expected, VatTiming.LoopFrameCount(length, fps), "loop frame count");
        }

        [TestCase(0.8333334f, Fps, 26)]
        [TestCase(1f, Fps, 31)]
        [TestCase(1f, 10f, 11)]
        [TestCase(0.01f, Fps, 1)]
        [TestCase(0.02f, Fps, 2)]
        public void OneShotFrameCount_AddsTheEndFrame(float length, float fps, int expected)
        {
            Assert.AreEqual(expected, VatTiming.OneShotFrameCount(length, fps), "one-shot frame count");
        }

        [TestCase(0f, Fps)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, Fps)]
        [TestCase(float.NaN, Fps)]
        [TestCase(1f, float.PositiveInfinity)]
        public void FrameCount_RejectsInvalidTiming(float length, float fps)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VatTiming.LoopFrameCount(length, fps));
            Assert.Throws<ArgumentOutOfRangeException>(() => VatTiming.OneShotFrameCount(length, fps));
        }

        [Test]
        public void FrameCount_OfAHugeClip_DoesNotWrapAroundInt()
        {
            Assert.Greater(VatTiming.LoopFrameCount(HugeLength, HugeFps), VatMath.MaxTextureSize, "loop frame count stays positive");
            Assert.Greater(VatTiming.OneShotFrameCount(HugeLength, HugeFps), VatMath.MaxTextureSize, "one-shot frame count stays positive");
        }

        [Test]
        public void Loop_LastFrameDoesNotRepeatTheFirst([Values(0.01f, 0.8333334f, 1f)] float length)
        {
            var frameCount = VatTiming.LoopFrameCount(length, Fps);
            var rate = VatTiming.FrameRate(frameCount, length, isLooping: true);

            Assert.AreEqual(frameCount / (double)length, rate, RateTolerance, "fps_eff = F / L");
            Assert.AreEqual(0.0, VatTiming.FrameTime(0, frameCount, length, isLooping: true), 0.0, "frame 0 is the clip start");
            Assert.AreEqual(length - length / frameCount, VatTiming.FrameTime(frameCount - 1, frameCount, length, isLooping: true), TimeTolerance,
                "the last frame is one step before the end");
            Assert.AreEqual(length, VatTiming.FrameTime(frameCount, frameCount, length, isLooping: true), TimeTolerance, "frame F is the next cycle");
        }

        [Test]
        public void OneShot_LastFrameIsTheClipEnd([Values(0.02f, 0.8333334f, 1f)] float length)
        {
            var frameCount = VatTiming.OneShotFrameCount(length, Fps);
            var rate = VatTiming.FrameRate(frameCount, length, isLooping: false);

            Assert.AreEqual((frameCount - 1) / (double)length, rate, RateTolerance, "fps_eff = (F − 1) / L");
            Assert.AreEqual(length, VatTiming.FrameTime(frameCount - 1, frameCount, length, isLooping: false), TimeTolerance, "the last frame is the clip end");
            Assert.AreEqual(rate * length, frameCount - 1, RateTolerance, "f reaches F − 1 at t = L");
        }

        [Test]
        public void OneShotOfOneFrame_DoesNotMove()
        {
            Assert.AreEqual(0f, VatTiming.FrameRate(1, 0.01f, isLooping: false), 0f, "a single frame has no frame rate");
            Assert.AreEqual(0.0, VatTiming.FrameTime(0, 1, 0.01f, isLooping: false), 0.0, "a single frame stays at the start");
        }
    }
}
