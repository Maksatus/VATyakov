using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatMixerTests
    {
        private const float FrameRate = 30f;
        private const int OneShotFrameCount = 31;
        private const int LoopFrameCount = 30;
        private const int OtherStartRow = OneShotFrameCount + LoopFrameCount;
        private const float Tolerance = 1e-5f;

        [Test]
        public void CrossFade_RampsTheWeightFromZeroToOne()
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 1.0, 0.5f);

            Assert.AreEqual(0f, FrameB(mixer, 1.0).w, Tolerance, "starts at zero");
            Assert.AreEqual(0.5f, FrameB(mixer, 1.25).w, Tolerance, "half way");
            Assert.AreEqual(1f, FrameB(mixer, 1.5).w, Tolerance, "ends at one");
            Assert.AreEqual(1f, FrameB(mixer, 10.0).w, Tolerance, "stays at one");
            Assert.AreEqual("Other", mixer.Clip.Name, "the target is the current clip");
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void CrossFade_NonPositiveDuration_SwitchesAtOnceWithoutNaN(float duration)
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 1.0, duration);

            mixer.Evaluate(1.0, out var frame, out var frameB);
            Assert.AreEqual(1f, frameB.w, "the target takes the whole weight at once");
            AssertFinite(frame);
            AssertFinite(frameB);
        }

        [Test]
        public void CrossFade_AfterAFinishedTransition_TheTargetBecomesTheSource()
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 0.0, 0.5f);
            mixer.CrossFade(OneShot(), 1.0, 0.5f);

            mixer.Evaluate(1.0, out var frame, out var frameB);
            Assert.AreEqual("Other", mixer.SourceClip.Name, "the finished target is the new source");
            Assert.That(frame.x, Is.GreaterThanOrEqualTo(OtherStartRow), "the source rows are the old target's");
            Assert.AreEqual(0f, frameB.w, Tolerance, "the new target starts at zero");
        }

        [TestCase(0.25f, "Idle")]
        [TestCase(0.75f, "Other")]
        public void CrossFade_DuringATransition_TheDominantClipBecomesTheSource(float progress, string dominant)
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 0.0, 1f);
            mixer.CrossFade(OneShot(), progress, 1f);

            Assert.AreEqual(dominant, mixer.SourceClip.Name, "the dominant clip stays");
            Assert.AreEqual("Fire", mixer.TargetClip.Name, "the new clip is the target");
            Assert.AreEqual(0f, mixer.Weight(progress), Tolerance, "the new transition starts at zero");
        }

        [Test]
        public void SetWeight_RampsFromTheCurrentWeight()
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 0.0, 1f);

            mixer.SetWeight(0.5, 0f, 1f);
            Assert.AreEqual(0.5f, mixer.Weight(0.5), Tolerance, "starts from the current weight");
            Assert.AreEqual(0.25f, mixer.Weight(1.0), Tolerance, "half way down");
            Assert.AreEqual(0f, mixer.Weight(2.0), Tolerance, "reaches the new weight");

            mixer.SetWeight(2.0, 0.8f, 0f);
            Assert.AreEqual(0.8f, mixer.Weight(2.0), Tolerance, "zero duration sets the weight at once");
        }

        [Test]
        public void SetWeight_WithoutATarget_DoesNothing()
        {
            var mixer = PlayingLoop();

            mixer.SetWeight(0.0, 1f, 0f);
            Assert.AreEqual(Vector4.zero, FrameB(mixer, 0.0), "the second frame stays empty");
        }

        [Test]
        public void Play_DropsTheTarget()
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 0.0, 1f);
            mixer.Play(OneShot(), 0.5);

            Assert.IsNull(mixer.TargetClip, "no target after Play");
            Assert.AreEqual(Vector4.zero, FrameB(mixer, 0.5), "the second frame is empty");
            Assert.AreEqual("Fire", mixer.Clip.Name, "Play switches at once");
        }

        [Test]
        public void Pause_HoldsTheWeight()
        {
            var mixer = PlayingLoop();
            mixer.CrossFade(Other(), 0.0, 1f);
            mixer.Pause(0.25);

            Assert.AreEqual(0.25f, mixer.Weight(5.0), Tolerance, "the weight does not move while paused");
            mixer.Resume(5.0);
            Assert.AreEqual(0.75f, mixer.Weight(5.5), Tolerance, "the ramp continues after resume");
        }

        [Test]
        public void CrossFade_OnlyTheTargetFinishes()
        {
            var mixer = new VatMixer(1f);
            mixer.Play(OneShot(), 0.0);
            mixer.CrossFade(OneShot(), 0.5, 2f);

            var finished = 0;
            for (var time = 0.5; time <= 5.0; time += 1.0 / 60.0)
            {
                finished += mixer.Evaluate(time, out _, out _) ? 1 : 0;
            }

            Assert.AreEqual(1, finished, "the source reaches its end silently, the target finishes once");
        }

        [Test]
        public void CrossFade_WithNothingPlaying_Plays()
        {
            var mixer = new VatMixer(1f);
            mixer.CrossFade(Other(), 0.0, 1f);

            Assert.AreEqual("Other", mixer.Clip.Name, "the clip plays");
            Assert.AreEqual(Vector4.zero, FrameB(mixer, 0.0), "no transition from nothing");
        }

        private static VatMixer PlayingLoop()
        {
            var mixer = new VatMixer(1f);
            mixer.Play(Loop(), 0.0);
            return mixer;
        }

        private static Vector4 FrameB(VatMixer mixer, double time)
        {
            mixer.Evaluate(time, out _, out var frameB);
            return frameB;
        }

        private static void AssertFinite(Vector4 value)
        {
            for (var i = 0; i < 4; i++)
            {
                Assert.IsFalse(float.IsNaN(value[i]) || float.IsInfinity(value[i]), $"component {i} is {value[i]}");
            }
        }

        private static VatClip OneShot()
        {
            return new VatClip("Fire", 0, OneShotFrameCount, 1f, FrameRate, isLooping: false);
        }

        private static VatClip Loop()
        {
            return new VatClip("Idle", OneShotFrameCount, LoopFrameCount, 1f, FrameRate, isLooping: true);
        }

        private static VatClip Other()
        {
            return new VatClip("Other", OtherStartRow, LoopFrameCount, 1f, FrameRate, isLooping: true);
        }
    }
}
