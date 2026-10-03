using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace VATyakov.Tests
{
    public class VatMathTests
    {
        private const float UrpFloatMin = 1.175494351e-38f;
        private const int RotateSeed = 5;
        private const int RotateSamples = 100;
        private const float HalfWeightTolerance = 1e-3f;

        [TestCase(1, 1, 1)]
        [TestCase(2079, 1, 2079)]
        [TestCase(4096, 1, 4096)]
        [TestCase(4097, 2, 2049)]
        [TestCase(4529, 2, 2265)]
        [TestCase(8192, 2, 4096)]
        [TestCase(8193, 3, 2731)]
        [TestCase(100000, 25, 4000)]
        public void Width_IsElementsOverBlocks_NotAPowerOfTwo(int elements, int blocks, int width)
        {
            Assert.AreEqual(blocks, VatMath.BlockCount(elements), "block count");
            Assert.AreEqual(width, VatMath.TextureWidth(elements), "texture width");
            Assert.LessOrEqual(width, VatMath.MaxTextureSize, "width fits the texture size limit");
            Assert.GreaterOrEqual(blocks * width, elements, "every element has a texel");
            Assert.Less((blocks - 1) * width, elements, "no empty block");
        }

        [Test]
        public void Texel_MapsEveryElementAndRowToAUniqueTexel([Values(1, 2079, 4097, 8193)] int elements)
        {
            const int totalRows = 7;
            var width = VatMath.TextureWidth(elements);
            var height = VatMath.BlockCount(elements) * totalRows;
            var seen = new HashSet<Vector2Int>();
            for (var row = 0; row < totalRows; row++)
            {
                for (var element = 0; element < elements; element++)
                {
                    var texel = VatMath.Texel(element, width, totalRows, row);
                    Assert.That(texel.x, Is.InRange(0, width - 1), "texel x inside the texture");
                    Assert.That(texel.y, Is.InRange(0, height - 1), "texel y inside the texture");
                    Assert.AreEqual(row, texel.y % totalRows, "row inside the block");
                    Assert.IsTrue(seen.Add(texel), $"texel {texel} used twice");
                }
            }
        }

        [TestCase(0.5f)]
        [TestCase(0.4999f)]
        [TestCase(0.5001f)]
        public void OppositeFrames_NearHalfWeight_BlendStaysFiniteAfterUrpNormalization(float weight)
        {
            var frameA = new Vector4(0f, 0f, 0f, 1f);
            var frameB = new Vector4(0f, 1f, 0f, 0f);
            Assert.AreEqual(-VatMath.FrameNormal(frameA), VatMath.FrameNormal(frameB), "opposite normals");
            Assert.AreEqual(-VatMath.FrameTangent(frameA), VatMath.FrameTangent(frameB), "opposite tangents");

            var normal = UrpSafeNormalize(Vector3.Lerp(VatMath.FrameNormal(frameA), VatMath.FrameNormal(frameB), weight));
            var tangent = UrpSafeNormalize(Vector3.Lerp(VatMath.FrameTangent(frameA), VatMath.FrameTangent(frameB), weight));

            AssertFinite(normal);
            AssertFinite(tangent);
        }

        [Test]
        public void Rotate_IsTheQuaternionRotationTimesItsSquaredLength()
        {
            var random = new Random(RotateSeed);
            for (var i = 0; i < RotateSamples; i++)
            {
                var q = VatTestUtil.RandomRotation(random);
                var rotation = new Quaternion(q.x, q.y, q.z, q.w);
                var vector = VatTestUtil.NextSignedVector(random);
                var length = 0.5f + (float)random.NextDouble();

                Assert.Less(Vector3.Distance(rotation * vector, VatMath.Rotate(q, vector)), 1e-5f, $"unit quaternion {i}");
                Assert.Less(Vector3.Distance(length * length * (rotation * vector), VatMath.Rotate(q * length, vector)), 1e-4f, $"scaled quaternion {i}");
                Assert.Less(Vector3.Distance(VatMath.FrameNormal(q), VatMath.Rotate(q, Vector3.forward)), 1e-5f, $"frame normal {i}");
            }
        }

        [Test]
        public void BonePoint_LerpedQuaternion_KeepsTheDistanceToThePivot()
        {
            var pivot = new Vector3(0.3f, 1.2f, -0.4f);
            var rest = pivot + new Vector3(0.5f, 0.1f, 0.2f);
            var offsetScale = new Vector4(0.1f, -0.2f, 0.3f, 1.5f);
            var from = Quaternion.Euler(0f, 0f, -30f);
            var to = Quaternion.Euler(0f, 0f, 90f);
            var halfway = Vector4.Lerp(new Vector4(from.x, from.y, from.z, from.w), new Vector4(to.x, to.y, to.z, to.w), 0.5f);

            var point = VatMath.BonePoint(offsetScale, halfway, pivot, rest);

            var center = pivot + (Vector3)offsetScale;
            Assert.AreEqual(offsetScale.w * Vector3.Distance(rest, pivot), Vector3.Distance(point, center), 1e-5f, "s · |rest − pivot|");
            Assert.Less(Vector3.Distance(center + Quaternion.Euler(0f, 0f, 30f) * (rest - pivot) * offsetScale.w, point), 1e-5f, "nlerp halfway is 30°");
        }

        [Test]
        public void BoneIndex_SurvivesHalfPrecision_ForEveryBone()
        {
            for (var bone = 0; bone <= byte.MaxValue; bone++)
            {
                Assert.AreEqual(bone, VatMath.BoneIndex(HalfByte(bone)), $"bone {bone}");
            }
        }

        [Test]
        public void BoneWeight_SurvivesHalfPrecision_ForEvery16BitValue()
        {
            for (var bits = 0; bits <= ushort.MaxValue; bits++)
            {
                var weight = VatMath.BoneWeight(HalfByte(bits >> 8), HalfByte(bits & byte.MaxValue));
                if (Mathf.RoundToInt(weight * VatMath.BoneWeightMax) != bits)
                {
                    Assert.Fail($"weight bits {bits} decode to {weight}");
                }
            }
        }

        [Test]
        public void BoneWeight_InHalfArithmetic_StaysFiniteAndClose()
        {
            var high = Half(Half(byte.MaxValue / (float)byte.MaxValue) * byte.MaxValue + 0.5f);
            Assert.IsTrue(float.IsInfinity(Half(Mathf.Floor(high) * 256f + byte.MaxValue)), "control: hi·256 + lo overflows half at weight 1");
            for (var bits = 0; bits <= ushort.MaxValue; bits++)
            {
                var hi = Mathf.Floor(Half(HalfByte(bits >> 8) * byte.MaxValue + 0.5f));
                var lo = Mathf.Floor(Half(HalfByte(bits & byte.MaxValue) * byte.MaxValue + 0.5f));
                var weight = Half(Half(hi * Half(256f / VatMath.BoneWeightMax)) + Half(lo * Half(1f / VatMath.BoneWeightMax)));
                if (!float.IsFinite(weight) || Mathf.Abs(weight - bits / VatMath.BoneWeightMax) > HalfWeightTolerance)
                {
                    Assert.Fail($"weight bits {bits} decode to {weight} in half arithmetic");
                }
            }
        }

        private static float Half(float value)
        {
            return Mathf.HalfToFloat(Mathf.FloatToHalf(value));
        }

        private static float HalfByte(int value)
        {
            return Half(value / (float)byte.MaxValue);
        }

        private static Vector3 UrpSafeNormalize(Vector3 v)
        {
            return v / Mathf.Sqrt(Mathf.Max(UrpFloatMin, Vector3.Dot(v, v)));
        }

        private static void AssertFinite(Vector3 v)
        {
            for (var i = 0; i < 3; i++)
            {
                Assert.IsFalse(float.IsNaN(v[i]) || float.IsInfinity(v[i]), $"component {i} is {v[i]}");
            }
        }
    }
}
