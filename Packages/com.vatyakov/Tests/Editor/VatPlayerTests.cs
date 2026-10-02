using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VATyakov.Tests
{
    public class VatPlayerTests
    {
        private const double FourHours = 4.0 * 3600.0;
        private const double Dt = 1.0 / 60.0;

        [Test]
        public void OneShot_PositiveSpeed_FinishesOnceOnTheLastFrame([Values(0.0, FourHours)] double start)
        {
            var player = new VatPlayer(1f);
            player.Play(OneShot(), start);

            var events = Run(player, start, 3.0);

            CollectionAssert.AreEqual(new[] { 60 }, events);
            Assert.AreEqual(1f, player.NormalizedTime(start + 3.0));
        }

        [Test]
        public void OneShot_NegativeSpeed_StartsAtTheEndAndFinishesOnceOnFrameZero([Values(0.0, FourHours)] double start)
        {
            var player = new VatPlayer(-1f);
            player.Play(OneShot(), start);

            Assert.AreEqual(1f, player.NormalizedTime(start));
            var events = Run(player, start, 3.0);

            CollectionAssert.AreEqual(new[] { 60 }, events);
            Assert.AreEqual(0f, player.NormalizedTime(start + 3.0));
        }

        [Test]
        public void Loop_NeverFinishes([Values(1f, -1f)] float speed)
        {
            var player = new VatPlayer(speed);
            player.Play(Loop(), 0.0);

            CollectionAssert.IsEmpty(Run(player, 0.0, 5.0));
        }

        [Test]
        public void PauseAndResumeAtTheEnd_DoNotFinishAgain([Values(1f, -1f)] float speed)
        {
            var player = new VatPlayer(speed);
            player.Play(OneShot(), 0.0);
            Run(player, 0.0, 2.0);

            player.Pause(2.0);
            CollectionAssert.IsEmpty(Run(player, 2.0, 1.0));
            player.Resume(3.0);
            player.SetSpeed(3.0, speed * 2f);

            CollectionAssert.IsEmpty(Run(player, 3.0, 2.0));
        }

        [Test]
        public void ReverseAfterTheEnd_FinishesOnceOnTheOtherEnd([Values(1f, -1f)] float speed)
        {
            var player = new VatPlayer(speed);
            player.Play(OneShot(), 0.0);
            Run(player, 0.0, 2.0);

            player.SetSpeed(2.0, -speed);

            CollectionAssert.AreEqual(new[] { 60 }, Run(player, 2.0, 3.0));
        }

        [Test]
        public void PlayAgain_FinishesAgain([Values(1f, -1f)] float speed)
        {
            var player = new VatPlayer(speed);
            player.Play(OneShot(), 0.0);
            Run(player, 0.0, 2.0);

            player.Play(OneShot(), 2.0);

            CollectionAssert.AreEqual(new[] { 60 }, Run(player, 2.0, 2.0));
        }

        [Test]
        public void Pause_HoldsTheFrame()
        {
            var player = new VatPlayer(1f);
            player.Play(Loop(), 0.0);
            player.Pause(0.5);

            player.Evaluate(0.5, out var paused);
            player.Evaluate(100.0, out var later);
            Assert.AreEqual(paused, later);
            Assert.IsTrue(player.IsPaused);

            player.Resume(100.0);
            Assert.AreEqual(0.75f, player.NormalizedTime(100.25), 1e-6f);
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(0.999f)]
        public void NormalizedTime_RoundTrips(float normalized)
        {
            Assert.AreEqual(normalized, Loop().NormalizedTime(Loop().Position(normalized)), 1e-6f);
            Assert.AreEqual(normalized, OneShot().NormalizedTime(OneShot().Position(normalized)), 1e-6f);
        }

        [Test]
        public void NormalizedTime_LoopIsThePhaseOfTheCycle()
        {
            var player = new VatPlayer(1f);
            player.Play(Loop(), 0.0, 0.25f);

            Assert.AreEqual(0.25f, player.NormalizedTime(0.0), 1e-6f);
            Assert.AreEqual(0.75f, player.NormalizedTime(2.5), 1e-6f);
            Assert.AreEqual(0f, player.NormalizedTime(0.75), 1e-6f);
        }

        private static List<int> Run(VatPlayer player, double from, double duration)
        {
            var events = new List<int>();
            var steps = (int)Math.Round(duration / Dt);
            for (var k = 0; k <= steps; k++)
            {
                if (player.Evaluate(from + k * Dt, out _))
                {
                    events.Add(k);
                }
            }

            return events;
        }

        private static VatClip OneShot()
        {
            return new VatClip("Fire", 0, 31, 1f, 30f, loop: false);
        }

        private static VatClip Loop()
        {
            return new VatClip("Idle", 31, 30, 1f, 30f, loop: true);
        }
    }
}
