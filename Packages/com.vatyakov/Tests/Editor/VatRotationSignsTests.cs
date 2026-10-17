using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatRotationSignsTests
    {
        private const int SpinFrames = 48;
        private const float SpinStep = 30f;

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

        private static Vector4 Spin(float degrees)
        {
            var rotation = Quaternion.AngleAxis(degrees, new Vector3(1f, 2f, 3f).normalized);
            return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
        }
    }
}
