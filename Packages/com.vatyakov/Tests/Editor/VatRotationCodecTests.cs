using System;
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

        [Test]
        public void Bytes_RoundTripEveryValue()
        {
            for (var b = 0; b < 256; b++)
            {
                var bytes = new Color32((byte)b, (byte)(255 - b), (byte)(b * 37 + 11), (byte)(b * 101 + 7));
                Assert.AreEqual(bytes, VatMath.RotationBytes((Color)bytes), $"byte {b}");
            }
        }

        [Test]
        public void Fields_RoundTripEveryValueAndIndex([Values(0, 1, 2, 3)] int index)
        {
            for (var n = 0; n < 1024; n++)
            {
                var b = (n * 389 + 17) & 1023;
                var c = 1023 - n;
                var fields = VatMath.RotationFields(VatSmallestThree.Pack(n, b, c, index));
                Assert.AreEqual((n, b, c, index), fields, $"n = {n}");
            }
        }

        [Test]
        public void Encode_EveryIndexAndSign_DecodesToTheSameRotation([Values(0, 1, 2, 3)] int index, [Values(1f, -1f)] float sign)
        {
            var random = new Random(index * 2 + (sign > 0f ? 0 : 1));
            for (var i = 0; i < 2000; i++)
            {
                var q = WithLargest(RandomRotation(random), index) * sign;
                var bytes = VatSmallestThree.Encode(q);
                Assert.AreEqual(index, bytes.a >> 6, $"index of {q}");
                AssertSameRotation(q, VatMath.DecodeRotation((Color)bytes));
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
        public void Encode_BoundaryComponents(float x, float y, float z, float w, int index)
        {
            var q = new Vector4(x, y, z, w);
            var bytes = VatSmallestThree.Encode(q);
            Assert.AreEqual(index, bytes.a >> 6, "ties go to the lower index");
            var fields = VatMath.RotationFields(bytes);
            foreach (var n in new[] { fields.A, fields.B, fields.C })
            {
                Assert.That(n, Is.InRange(0, 1023));
            }

            AssertSameRotation(q, VatMath.DecodeRotation((Color)bytes));
        }

        [TestCase(0, -HalfSqrt2)]
        [TestCase(1023, HalfSqrt2)]
        public void Decode_ExtremeFields_AreMinusAndPlusOneOverSqrt2(int n, float component)
        {
            var q = VatMath.DecodeRotation((Color)VatSmallestThree.Pack(n, 512, 512, 3));
            Assert.AreEqual(component, q.x, 1e-6f);
            Assert.AreEqual(1f, q.magnitude, 2e-3f);
        }

        [Test]
        public void Nlerp_AlignsTheSign()
        {
            var random = new Random(7);
            for (var i = 0; i < 200; i++)
            {
                var q0 = RandomRotation(random);
                var q1 = RandomRotation(random);
                AssertSameRotation(q0, VatMath.Nlerp(q0, -q0, 0.5f));
                Assert.AreEqual(VatMath.Nlerp(q0, q1, 0.3f), VatMath.Nlerp(q0, -q1, 0.3f));
                Assert.AreEqual(1f, VatMath.Nlerp(q0, q1, 0.5f).magnitude, 1e-5f);
            }
        }

        [Test]
        public void FrameAxes_MatchUnityRotation_AndTheBakedFrame()
        {
            var random = new Random(11);
            for (var i = 0; i < 200; i++)
            {
                var v = RandomRotation(random);
                var q = new Quaternion(v.x, v.y, v.z, v.w);
                AssertClose(q * Vector3.forward, VatMath.FrameNormal(v));
                AssertClose(q * Vector3.right, VatMath.FrameTangent(v));

                var n = q * Vector3.forward;
                var t = q * Vector3.right;
                var frame = VatTangentFrames.Rotation(n, t);
                AssertClose(n, VatMath.FrameNormal(frame));
                AssertClose(t, VatMath.FrameTangent(frame));
            }
        }

        internal static void AssertSameRotation(Vector4 expected, Vector4 actual)
        {
            Assert.AreEqual(1f, actual.magnitude, 5e-3f, $"|{actual}|");
            var dot = Mathf.Abs(Vector4.Dot(expected.normalized, actual.normalized));
            var angle = 2f * Mathf.Acos(Mathf.Min(1f, dot)) * Mathf.Rad2Deg;
            Assert.Less(angle, MaxAngle, $"{expected} → {actual}");
        }

        internal static Vector4 RandomRotation(Random random)
        {
            Vector4 q;
            do
            {
                q = new Vector4(Next(random), Next(random), Next(random), Next(random));
            } while (q.sqrMagnitude < 1e-2f || q.sqrMagnitude > 1f);
            return q.normalized;
        }

        private static void AssertClose(Vector3 expected, Vector3 actual)
        {
            Assert.Less((expected - actual).magnitude, 1e-5f, $"{expected} vs {actual}");
        }

        private static Vector4 WithLargest(Vector4 q, int index)
        {
            var largest = 0;
            for (var i = 1; i < 4; i++)
            {
                if (Math.Abs(q[i]) > Math.Abs(q[largest]))
                {
                    largest = i;
                }
            }

            (q[index], q[largest]) = (q[largest], q[index]);
            return q;
        }

        private static float Next(Random random)
        {
            return (float)(random.NextDouble() * 2.0 - 1.0);
        }
    }
}
