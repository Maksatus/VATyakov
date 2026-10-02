using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatRotationCodecTests
    {
        private const float HalfSqrt2 = 0.70710678f;
        private const float MaxAngle = 0.25f;
        private const int ByteCount = 256;
        private const int FieldCount = 1024;
        private const int MaxField = FieldCount - 1;
        private const int MidField = FieldCount / 2;
        private const int IndexShift = 6;
        private const int RandomRotationCount = 2000;
        private const int RandomPairCount = 200;

        [Test]
        public void Bytes_RoundTripEveryValue()
        {
            for (var value = 0; value < ByteCount; value++)
            {
                var bytes = new Color32((byte)value, (byte)(255 - value), (byte)(value * 37 + 11), (byte)(value * 101 + 7));
                Assert.AreEqual(bytes, VatMath.RotationBytes((Color)bytes), $"byte {value}");
            }
        }

        [Test]
        public void Fields_RoundTripEveryValueAndIndex([Values(0, 1, 2, 3)] int index)
        {
            for (var fieldA = 0; fieldA < FieldCount; fieldA++)
            {
                var fieldB = (fieldA * 389 + 17) & MaxField;
                var fieldC = MaxField - fieldA;
                var fields = VatMath.RotationFields(VatSmallestThree.Pack(fieldA, fieldB, fieldC, index));
                Assert.AreEqual((fieldA, fieldB, fieldC, index), fields, $"field A = {fieldA}");
            }
        }

        [Test]
        public void Encode_EveryIndexAndSign_DecodesToTheSameRotation([Values(0, 1, 2, 3)] int index, [Values(1f, -1f)] float sign)
        {
            var random = new Random(index * 2 + (sign > 0f ? 0 : 1));
            for (var i = 0; i < RandomRotationCount; i++)
            {
                var rotation = WithLargest(VatTestUtil.RandomRotation(random), index) * sign;
                var bytes = VatSmallestThree.Encode(rotation);
                Assert.AreEqual(index, bytes.a >> IndexShift, $"index of {rotation}");
                AssertSameRotation(rotation, VatMath.DecodeRotation((Color)bytes));
            }
        }

        [TestCase(1f, 0f, 0f, 0f, 0)]
        [TestCase(0f, 0f, 0f, 1f, 3)]
        [TestCase(0f, 0f, 0f, -1f, 3)]
        [TestCase(0f, -1f, 0f, 0f, 1)]
        [TestCase(HalfSqrt2, HalfSqrt2, 0f, 0f, 0)]
        [TestCase(HalfSqrt2, -HalfSqrt2, 0f, 0f, 0)]
        [TestCase(0f, 0f, -HalfSqrt2, HalfSqrt2, 2)]
        [TestCase(0.5f, 0.5f, 0.5f, 0.5f, 0)]
        [TestCase(-0.5f, 0.5f, -0.5f, 0.5f, 0)]
        public void Encode_BoundaryComponents_PicksTheLowerIndexAndDecodes(float x, float y, float z, float w, int index)
        {
            var rotation = new Vector4(x, y, z, w);
            var bytes = VatSmallestThree.Encode(rotation);
            Assert.AreEqual(index, bytes.a >> IndexShift, "ties go to the lower index");
            var fields = VatMath.RotationFields(bytes);
            foreach (var field in new[] { fields.A, fields.B, fields.C })
            {
                Assert.That(field, Is.InRange(0, MaxField), "field fits 10 bits");
            }

            AssertSameRotation(rotation, VatMath.DecodeRotation((Color)bytes));
        }

        [TestCase(0, -HalfSqrt2)]
        [TestCase(MaxField, HalfSqrt2)]
        public void Decode_ExtremeFields_AreMinusAndPlusOneOverSqrt2(int field, float component)
        {
            var rotation = VatMath.DecodeRotation((Color)VatSmallestThree.Pack(field, MidField, MidField, 3));
            Assert.AreEqual(component, rotation.x, 1e-6f, "extreme component");
            Assert.AreEqual(1f, rotation.magnitude, 2e-3f, "decoded rotation is unit length");
        }

        [Test]
        public void Nlerp_AlignsTheSign()
        {
            var random = new Random(7);
            for (var i = 0; i < RandomPairCount; i++)
            {
                var q0 = VatTestUtil.RandomRotation(random);
                var q1 = VatTestUtil.RandomRotation(random);
                AssertSameRotation(q0, VatMath.Nlerp(q0, -q0, 0.5f));
                Assert.AreEqual(VatMath.Nlerp(q0, q1, 0.3f), VatMath.Nlerp(q0, -q1, 0.3f), "the sign of q1 does not change the result");
                Assert.AreEqual(1f, VatMath.Nlerp(q0, q1, 0.5f).magnitude, 1e-5f, "nlerp is unit length");
            }
        }

        [Test]
        public void FrameAxes_MatchUnityRotation_AndTheBakedFrame()
        {
            var random = new Random(11);
            for (var i = 0; i < RandomPairCount; i++)
            {
                var packed = VatTestUtil.RandomRotation(random);
                var rotation = new Quaternion(packed.x, packed.y, packed.z, packed.w);
                AssertClose(rotation * Vector3.forward, VatMath.FrameNormal(packed));
                AssertClose(rotation * Vector3.right, VatMath.FrameTangent(packed));

                var normal = rotation * Vector3.forward;
                var tangent = rotation * Vector3.right;
                var frame = VatTangentFrames.Rotation(normal, tangent);
                AssertClose(normal, VatMath.FrameNormal(frame));
                AssertClose(tangent, VatMath.FrameTangent(frame));
            }
        }

        private static void AssertSameRotation(Vector4 expected, Vector4 actual)
        {
            Assert.AreEqual(1f, actual.magnitude, 5e-3f, $"|{actual}|");
            var dot = Mathf.Abs(Vector4.Dot(expected.normalized, actual.normalized));
            var angle = 2f * Mathf.Acos(Mathf.Min(1f, dot)) * Mathf.Rad2Deg;
            Assert.Less(angle, MaxAngle, $"{expected} → {actual}");
        }

        private static void AssertClose(Vector3 expected, Vector3 actual)
        {
            Assert.Less((expected - actual).magnitude, 1e-5f, $"{expected} vs {actual}");
        }

        private static Vector4 WithLargest(Vector4 rotation, int index)
        {
            var largest = 0;
            for (var i = 1; i < 4; i++)
            {
                if (Mathf.Abs(rotation[i]) > Mathf.Abs(rotation[largest]))
                {
                    largest = i;
                }
            }

            (rotation[index], rotation[largest]) = (rotation[largest], rotation[index]);
            return rotation;
        }
    }
}
