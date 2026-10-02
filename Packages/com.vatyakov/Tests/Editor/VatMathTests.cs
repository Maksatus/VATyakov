using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace VATyakov.Tests
{
    public class VatMathTests
    {
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
    }
}
