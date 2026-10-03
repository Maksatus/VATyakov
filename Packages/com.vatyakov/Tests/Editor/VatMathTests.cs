using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatMathTests
    {
        private const float UrpFloatMin = 1.175494351e-38f;

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
