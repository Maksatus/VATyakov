using System;
using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatTimingTests
    {
        [TestCase(0.8333334f, 30f, 25)]
        [TestCase(1f, 30f, 30)]
        [TestCase(0.01f, 30f, 1)]
        public void LoopFrameCount_RoundsLengthTimesFps(float length, float fps, int expected)
        {
            Assert.AreEqual(expected, VatTiming.LoopFrameCount(length, fps));
        }

        [TestCase(0.8333334f, 30f, 26)]
        [TestCase(1f, 30f, 31)]
        [TestCase(1f, 10f, 11)]
        [TestCase(0.01f, 30f, 1)]
        [TestCase(0.02f, 30f, 2)]
        public void OneShotFrameCount_AddsTheEndFrame(float length, float fps, int expected)
        {
            Assert.AreEqual(expected, VatTiming.OneShotFrameCount(length, fps));
        }

        [TestCase(0f, 30f)]
        [TestCase(1f, 0f)]
        [TestCase(-1f, 30f)]
        [TestCase(float.NaN, 30f)]
        [TestCase(1f, float.PositiveInfinity)]
        public void FrameCount_RejectsInvalidTiming(float length, float fps)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => VatTiming.LoopFrameCount(length, fps));
            Assert.Throws<ArgumentOutOfRangeException>(() => VatTiming.OneShotFrameCount(length, fps));
        }

        [Test]
        public void FrameCount_OfAHugeClip_DoesNotWrapAroundInt()
        {
            Assert.Greater(VatTiming.LoopFrameCount(1e9f, 1e6f), VatMath.MaxTextureSize);
            Assert.Greater(VatTiming.OneShotFrameCount(1e9f, 1e6f), VatMath.MaxTextureSize);
        }

        [Test]
        public void Loop_LastFrameDoesNotRepeatTheFirst([Values(0.01f, 0.8333334f, 1f)] float length)
        {
            var frameCount = VatTiming.LoopFrameCount(length, 30f);
            var rate = VatTiming.FrameRate(frameCount, length, loop: true);

            Assert.AreEqual(frameCount / (double)length, rate, 1e-4);
            Assert.AreEqual(0.0, VatTiming.FrameTime(0, frameCount, length, true));
            Assert.AreEqual(length - length / frameCount, VatTiming.FrameTime(frameCount - 1, frameCount, length, true), 1e-9);
            Assert.AreEqual(length, VatTiming.FrameTime(frameCount, frameCount, length, true), 1e-9, "frame F is the next cycle");
        }

        [Test]
        public void OneShot_LastFrameIsTheClipEnd([Values(0.02f, 0.8333334f, 1f)] float length)
        {
            var frameCount = VatTiming.OneShotFrameCount(length, 30f);
            var rate = VatTiming.FrameRate(frameCount, length, loop: false);

            Assert.AreEqual((frameCount - 1) / (double)length, rate, 1e-4);
            Assert.AreEqual(length, VatTiming.FrameTime(frameCount - 1, frameCount, length, false), 1e-9);
            Assert.AreEqual(rate * length, frameCount - 1, 1e-4, "f reaches F − 1 at t = L");
        }

        [Test]
        public void OneShotOfOneFrame_DoesNotMove()
        {
            Assert.AreEqual(0f, VatTiming.FrameRate(1, 0.01f, loop: false));
            Assert.AreEqual(0.0, VatTiming.FrameTime(0, 1, 0.01f, loop: false));
        }
    }
}
