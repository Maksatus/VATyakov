using NUnit.Framework;

namespace VATyakov.Tests
{
    // §1.3: time anchor, speed, pause and reverse on the CPU.
    public class VatPlaybackTests
    {
        const double FourHours = 4.0 * 3600.0;
        const int StartRow = 100;

        [Test]
        public void ZeroSpeed_HoldsTheOffsetFrame([Values] bool loop, [Values(0.0, 1.0, FourHours, 1e9)] double time)
        {
            var playback = new VatPlayback(Clip(26, loop), 3.0, speed: 0f, offset: 7.25);
            VatClipFrameTests.AssertFrame(playback.Frame(time), 7, 8, 0.25f);
        }

        [Test]
        public void NegativeSpeed_Loop_WrapsToTheLastFrame()
        {
            var playback = new VatPlayback(Clip(30, loop: true), 0.0, speed: -1f);
            VatClipFrameTests.AssertFrame(playback.Frame(0.01), 29, 0, 0.7f); // f = −0.3
        }

        [Test]
        public void NegativeSpeed_OneShot_StopsOnFrameZero()
        {
            var playback = new VatPlayback(Clip(31, loop: false), 10.0, speed: -1f, offset: 30.0);

            VatClipFrameTests.AssertFrame(playback.Frame(10.0), 29, 30, 1f);
            VatClipFrameTests.AssertFrame(playback.Frame(10.5), 15, 16, 0f);
            VatClipFrameTests.AssertFrame(playback.Frame(11.0), 0, 1, 0f);
            VatClipFrameTests.AssertFrame(playback.Frame(FourHours), 0, 1, 0f);
        }

        [Test]
        public void OneShot_StopsOnTheLastFrame([Values(0.0, FourHours)] double start)
        {
            var playback = new VatPlayback(Clip(31, loop: false), start);

            VatClipFrameTests.AssertFrame(playback.Frame(start - 1.0), 0, 1, 0f);
            VatClipFrameTests.AssertFrame(playback.Frame(start + 1.0), 29, 30, 1f);
            VatClipFrameTests.AssertFrame(playback.Frame(start + 100.0), 29, 30, 1f);
        }

        // Double time keeps the frame exact after 4 hours, whenever the clip was started.
        [Test]
        public void FourHourSession_KeepsTheFramePosition([Values(0.0, FourHours)] double start, [Values] bool loop)
        {
            var playback = new VatPlayback(Clip(30, loop), start);
            for (int k = 0; k < 29; k++)
            {
                var frame = playback.Frame(FourHours + (k + 0.5) / 30.0);
                if (loop || start > 0.0)
                    VatClipFrameTests.AssertFrame(frame, k, k + 1, 0.5f);
                else
                    VatClipFrameTests.AssertFrame(frame, 28, 29, 1f);
            }
        }

        // A speed change keeps the frame shown right now and plays on at the new rate.
        [Test]
        public void SetSpeed_KeepsTheCurrentFrame([Values] bool loop, [Values(2.5, FourHours)] double time,
            [Values(0f, 0.5f, 2f, -1f)] float speed)
        {
            var clip = Clip(30, loop);
            var playback = new VatPlayback(clip, 1.25, offset: 3.5);
            var before = playback.Frame(time);

            playback.SetSpeed(time, speed);

            Assert.AreEqual(speed, playback.Speed);
            Assert.AreEqual(before, playback.Frame(time));
            Assert.AreEqual(clip.Wrap(playback.Position(time) + 0.1 * clip.FrameRate * speed),
                clip.Wrap(playback.Position(time + 0.1)), 1e-6);
        }

        [Test]
        public void SetSpeed_OneShotPastTheEnd_PlaysBackFromTheLastFrame()
        {
            var clip = Clip(31, loop: false);
            var playback = new VatPlayback(clip, 0.0);

            playback.SetSpeed(100.0, -1f);

            VatClipFrameTests.AssertFrame(playback.Frame(100.0), 29, 30, 1f);
            VatClipFrameTests.AssertFrame(playback.Frame(100.5), 15, 16, 0f);
        }

        // fps_eff = 30: one frame per 1/30 s.
        static VatClip Clip(int frameCount, bool loop) => new VatClip("Clip", StartRow, frameCount, 1f, 30f, loop);
    }
}
