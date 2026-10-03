using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using VATyakov.Editor;
using Object = UnityEngine.Object;

namespace VATyakov.Tests
{
    public class VatAssetMemoryTests
    {
        private const int Vertices = 4999;
        private const int Width = 2500;
        private const int Blocks = 2;
        private const int ShortFrames = 10;
        private const int LongFrames = 20;
        private const int TotalRows = ShortFrames + LongFrames;
        private const int Height = Blocks * TotalRows;
        private const long HalfTexelBytes = 8;
        private const long ByteTexelBytes = 4;
        private const long TexelBytes = HalfTexelBytes + ByteTexelBytes;
        private const long ProfilerOverhead = 4096;

        private VatAsset _asset;

        [SetUp]
        public void SetUp()
        {
            var clips = new[]
            {
                new VatClip("Short", 0, ShortFrames, 1f, ShortFrames, isLooping: true),
                new VatClip("Long", ShortFrames, LongFrames, 1f, LongFrames, isLooping: false),
            };
            var position = CreateTexture(GraphicsFormat.R16G16B16A16_SFloat);
            var rotation = CreateTexture(GraphicsFormat.R8G8B8A8_UNorm);
            _asset = ScriptableObject.CreateInstance<VatAsset>();
            _asset.SetData(new VatLayoutInfo(Vertices, Width, Blocks, TotalRows), null, position, rotation, VatPositionFormat.Half, VatPositionRange.Identity,
                new Vector3[TotalRows], clips, default, string.Empty);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_asset.PositionTexture);
            Object.DestroyImmediate(_asset.RotationTexture);
            Object.DestroyImmediate(_asset);
        }

        [Test]
        public void Textures_AreWidthTimesHeightTimesTexelBytes()
        {
            var memory = new VatAssetMemory(_asset);

            Assert.AreEqual(Width * Height * HalfTexelBytes, memory.Position, "half position texture");
            Assert.AreEqual(Width * Height * ByteTexelBytes, memory.Rotation, "RGBA8 rotation texture");
            Assert.AreEqual(memory.Position + memory.Rotation, memory.Total, "total");
        }

        [Test]
        public void TwoBlocks_PaddingIsTheRemainderOfTheLastBlock_AndClipsCoverTheTextures()
        {
            var memory = new VatAssetMemory(_asset);

            Assert.AreEqual(Blocks * Width - Vertices, memory.PaddingTexels, "empty texels per row");
            Assert.AreEqual(TotalRows * TexelBytes, memory.Padding, "one texel in every row of the last block, both textures");
            Assert.AreEqual(Blocks * Width * ShortFrames * TexelBytes, memory.Clip(_asset.Clips[0]), "rows of the short clip in both blocks");
            Assert.AreEqual(memory.Total, memory.Clip(_asset.Clips[0]) + memory.Clip(_asset.Clips[1]), "the clips add up to the textures");
        }

        [Test]
        public void Textures_MatchTheProfiler()
        {
            var memory = new VatAssetMemory(_asset);

            AssertProfiler(_asset.PositionTexture, memory.Position);
            AssertProfiler(_asset.RotationTexture, memory.Rotation);
        }

        [Test]
        public void Bytes_PicksTheUnit()
        {
            Assert.AreEqual("512 B", VatText.Bytes(512));
            Assert.AreEqual("1.5 KB", VatText.Bytes(1536));
            Assert.AreEqual("3.17 MB", VatText.Bytes(2 * 1663200));
        }

        private static Texture2D CreateTexture(GraphicsFormat format)
        {
            var texture = new Texture2D(Width, Height, format, TextureCreationFlags.None);
            texture.Apply(false, true);
            return texture;
        }

        private static void AssertProfiler(Texture2D texture, long bytes)
        {
            var extra = Profiler.GetRuntimeMemorySizeLong(texture) - bytes;
            Assert.That(extra, Is.InRange(0L, ProfilerOverhead), $"{texture.graphicsFormat}: the profiler adds only the object header");
        }
    }
}
