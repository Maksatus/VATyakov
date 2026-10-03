using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatRotationSignsTests
    {
        private const int LoopFrames = 12;
        private const int SpinFrames = 48;
        private const float SpinStep = 30f;
        private const float WobbleAmplitude = 30f;

        [Test]
        public void Align_KeepsNeighbourFramesInOneHemisphere()
        {
            var signs = new VatRotationSigns(1);
            var random = new Random(3);
            var previous = Vector4.zero;
            for (var frame = 0; frame < SpinFrames; frame++)
            {
                var rotation = Spin(frame * SpinStep) * (random.Next(2) == 0 ? 1f : -1f);
                var aligned = signs.Align(0, frame, rotation);
                Assert.IsTrue(aligned == rotation || aligned == -rotation, "only the sign changes");
                if (frame > 0)
                {
                    Assert.Greater(Vector4.Dot(previous, aligned), 0f, $"frame {frame}");
                }

                previous = aligned;
            }
        }

        [Test]
        public void Align_FirstFrameOfAClip_KeepsItsSign()
        {
            var signs = new VatRotationSigns(1);
            var rotation = Spin(SpinStep);
            signs.Align(0, 0, rotation);
            signs.Align(0, 1, rotation);

            Assert.AreEqual(-rotation, signs.Align(0, 0, -rotation), "a clip does not follow the previous one");
        }

        [Test]
        public void CloseLoop_OneTurnIsASeam_TwoTurnsAndAWobbleAreNot()
        {
            var signs = new VatRotationSigns(3);
            for (var frame = 0; frame < LoopFrames; frame++)
            {
                var phase = (float)frame / LoopFrames;
                signs.Align(0, frame, Spin(phase * 360f));
                signs.Align(1, frame, Spin(phase * 720f));
                signs.Align(2, frame, Spin(WobbleAmplitude * Mathf.Sin(phase * 2f * Mathf.PI)));
            }

            signs.CloseLoop("Spin");

            Assert.AreEqual(1, signs.SeamCount, "only one turn per loop breaks the seam");
            StringAssert.Contains("vertex 0, clip 'Spin'", signs.FirstSeam);
        }

        private static Vector4 Spin(float degrees)
        {
            var rotation = Quaternion.AngleAxis(degrees, new Vector3(1f, 2f, 3f).normalized);
            return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
        }
    }
}
