using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatRotationCodecTests
    {
        private const float MaxAngle = 1f;
        private const float MaxInterpolationAngle = 0.1f;
        private const float FrameStep = 24f;
        private const int RandomRotationCount = 2000;
        private const int RandomPairCount = 200;

        [Test]
        public void Encode_RandomRotationsOfBothSigns_DecodeWithinOneDegree([Values(1f, -1f)] float sign)
        {
            var random = new Random(sign > 0f ? 1 : 2);
            for (var i = 0; i < RandomRotationCount; i++)
            {
                var rotation = VatTestUtil.RandomRotation(random) * sign;
                var decoded = VatMath.DecodeRotation((Color)VatRotationCodec.Encode(rotation));
                Assert.Greater(Vector4.Dot(rotation, decoded), 0f, "the sign is kept");
                Assert.Less(RotationAngle(rotation, decoded), MaxAngle, $"{rotation} → {decoded}");
            }
        }

        [TestCase(0f, 0f, 0f, 1f)]
        [TestCase(0f, 0f, 0f, -1f)]
        [TestCase(1f, 0f, 0f, 0f)]
        [TestCase(0f, -1f, 0f, 0f)]
        public void Encode_ZeroAndUnitComponents_DecodeExactly(float x, float y, float z, float w)
        {
            var rotation = new Vector4(x, y, z, w);
            var decoded = VatMath.DecodeRotation((Color)VatRotationCodec.Encode(rotation));
            Assert.Less((rotation - decoded).magnitude, 1e-6f, $"{rotation} → {decoded}");
        }

        [Test]
        public void FrameAxes_OfAScaledRotation_AreTheRotatedAxesTimesTheSquaredLength([Values(0.9f, 1f, 1.1f)] float scale)
        {
            var random = new Random(11);
            for (var i = 0; i < RandomPairCount; i++)
            {
                var packed = VatTestUtil.RandomRotation(random);
                var rotation = new Quaternion(packed.x, packed.y, packed.z, packed.w);
                AssertClose(rotation * Vector3.forward * (scale * scale), VatMath.FrameNormal(packed * scale));
                AssertClose(rotation * Vector3.right * (scale * scale), VatMath.FrameTangent(packed * scale));
            }
        }

        [Test]
        public void FrameAxes_MatchTheBakedFrame()
        {
            var random = new Random(13);
            for (var i = 0; i < RandomPairCount; i++)
            {
                var normal = VatTestUtil.RandomDirection(random);
                VatTangentFrames.TryOrthogonalize(normal, VatTestUtil.RandomDirection(random), out var tangent);
                var frame = VatTangentFrames.Rotation(normal, tangent);
                AssertClose(normal, VatMath.FrameNormal(frame));
                AssertClose(tangent, VatMath.FrameTangent(frame));
            }
        }

        [Test]
        public void LerpOfAlignedFrames_FollowsSlerpWithoutNormalization([Values(0.25f, 0.5f, 0.75f)] float t)
        {
            var random = new Random(17);
            for (var i = 0; i < RandomPairCount; i++)
            {
                var packed = VatTestUtil.RandomRotation(random);
                var q0 = new Quaternion(packed.x, packed.y, packed.z, packed.w);
                var q1 = q0 * Quaternion.AngleAxis(FrameStep, VatTestUtil.RandomDirection(random));
                var lerp = Vector4.Lerp(packed, Packed(q1), t);
                var slerp = Quaternion.Slerp(q0, q1, t);
                Assert.Less(Vector3.Angle(slerp * Vector3.forward, VatMath.FrameNormal(lerp)), MaxInterpolationAngle, "normal, degrees");
                Assert.Less(Vector3.Angle(slerp * Vector3.right, VatMath.FrameTangent(lerp)), MaxInterpolationAngle, "tangent, degrees");
            }
        }

        private static Vector4 Packed(Quaternion rotation)
        {
            return new Vector4(rotation.x, rotation.y, rotation.z, rotation.w);
        }

        private static float RotationAngle(Vector4 expected, Vector4 actual)
        {
            var dot = Mathf.Abs(Vector4.Dot(expected.normalized, actual.normalized));
            return 2f * Mathf.Acos(Mathf.Min(1f, dot)) * Mathf.Rad2Deg;
        }

        private static void AssertClose(Vector3 expected, Vector3 actual)
        {
            Assert.Less((expected - actual).magnitude, 1e-5f, $"{expected} vs {actual}");
        }
    }
}
