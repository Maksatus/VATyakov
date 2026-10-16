using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatPlaybackTests
    {
        private const double FourHours = 4.0 * 3600.0;
        private const int StartRow = 100;
        private const float FrameRate = 30f;

        [Test]
        public void ZeroSpeed_HoldsTheOffsetFrame([Values] bool isLooping, [Values(0.0, 1.0, FourHours, 1e9)] double time)
        {
            var playback = new VatPlayback(Clip(26, isLooping), 3.0, speed: 0f, offset: 7.25);
            VatTestUtil.AssertFrame(Frame(playback, time), StartRow, 7, 8, 0.25f);
        }

        [Test]
        public void NegativeSpeed_Loop_WrapsToTheLastFrame()
        {
            var playback = new VatPlayback(Clip(30, isLooping: true), 0.0, speed: -1f);
            VatTestUtil.AssertFrame(Frame(playback, 0.01), StartRow, 29, 0, 0.7f);
        }

        [Test]
        public void NegativeSpeed_OneShot_StopsOnFrameZero()
        {
            var playback = new VatPlayback(Clip(31, isLooping: false), 10.0, speed: -1f, offset: 30.0);

            VatTestUtil.AssertFrame(Frame(playback, 10.0), StartRow, 29, 30, 1f);
            VatTestUtil.AssertFrame(Frame(playback, 10.5), StartRow, 15, 16, 0f);
            VatTestUtil.AssertFrame(Frame(playback, 11.0), StartRow, 0, 1, 0f);
            VatTestUtil.AssertFrame(Frame(playback, FourHours), StartRow, 0, 1, 0f);
        }

        [Test]
        public void OneShot_StopsOnTheLastFrame([Values(0.0, FourHours)] double start)
        {
            var playback = new VatPlayback(Clip(31, isLooping: false), start);

            VatTestUtil.AssertFrame(Frame(playback, start - 1.0), StartRow, 0, 1, 0f);
            VatTestUtil.AssertFrame(Frame(playback, start + 1.0), StartRow, 29, 30, 1f);
            VatTestUtil.AssertFrame(Frame(playback, start + 100.0), StartRow, 29, 30, 1f);
        }

        [Test]
        public void FourHourSession_KeepsTheFramePosition([Values(0.0, FourHours)] double start, [Values] bool isLooping)
        {
            var playback = new VatPlayback(Clip(30, isLooping), start);
            for (var k = 0; k < 29; k++)
            {
                var frame = Frame(playback, FourHours + (k + 0.5) / FrameRate);
                if (isLooping || start > 0.0)
                {
                    VatTestUtil.AssertFrame(frame, StartRow, k, k + 1, 0.5f);
                }
                else
                {
                    VatTestUtil.AssertFrame(frame, StartRow, 28, 29, 1f);
                }
            }
        }

        [Test]
        public void SetSpeed_KeepsTheCurrentFrame([Values] bool isLooping, [Values(2.5, FourHours)] double time,
            [Values(0f, 0.5f, 2f, -1f)] float speed)
        {
            var clip = Clip(30, isLooping);
            var playback = new VatPlayback(clip, 1.25, offset: 3.5);
            var before = Frame(playback, time);

            playback.SetSpeed(time, speed);

            Assert.AreEqual(speed, playback.Speed, "speed is stored as is");
            Assert.AreEqual(before, Frame(playback, time), "the frame does not jump");
            Assert.AreEqual(clip.Wrap(playback.Position(time) + 0.1 * clip.FrameRate * speed),
                clip.Wrap(playback.Position(time + 0.1)), 1e-6, "plays on at the new speed");
        }

        [Test]
        public void SetSpeed_OneShotPastTheEnd_PlaysBackFromTheLastFrame()
        {
            var clip = Clip(31, isLooping: false);
            var playback = new VatPlayback(clip, 0.0);

            playback.SetSpeed(100.0, -1f);

            VatTestUtil.AssertFrame(Frame(playback, 100.0), StartRow, 29, 30, 1f);
            VatTestUtil.AssertFrame(Frame(playback, 100.5), StartRow, 15, 16, 0f);
        }

        private static Vector4 Frame(VatPlayback playback, double time)
        {
            return playback.Clip.Frame(playback.Position(time));
        }

        private static VatClip Clip(int frameCount, bool isLooping)
        {
            return new VatClip("Clip", StartRow, frameCount, 1f, FrameRate, isLooping);
        }
    }
}
