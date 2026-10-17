using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    public class VatCoreHlslTests
    {
        private const string CorePath = "Packages/com.vatyakov/Shaders/VatCore.hlsl";
        private const int ByteBits = 8;
        private const int ByteMask = 0xFF;
        private const float Rounding = 0.5f;

        [Test]
        public void Defines_MatchVatMath()
        {
            var defines = Defines();

            Assert.AreEqual(VatMath.ByteMax / VatMath.RotationUnit, defines["VAT_ROTATION_SCALE"], 1e-6, "VAT_ROTATION_SCALE");
            Assert.AreEqual(VatMath.RotationZero / VatMath.RotationUnit, defines["VAT_ROTATION_BIAS"], 1e-6, "VAT_ROTATION_BIAS");
            Assert.AreEqual(VatMath.TexelsPerBone, defines["VAT_TEXELS_PER_BONE"], "VAT_TEXELS_PER_BONE");
            Assert.AreEqual(VatMath.BonePivotRows, defines["VAT_BONE_PIVOT_ROWS"], "VAT_BONE_PIVOT_ROWS");
            Assert.AreEqual(VatMath.ByteMax, defines["VAT_BYTE_MAX"], "VAT_BYTE_MAX");
            Assert.AreEqual(VatMath.ByteSteps, defines["VAT_BYTE_STEPS"], "VAT_BYTE_STEPS");
            Assert.AreEqual(VatMath.BoneWeightMax, defines["VAT_BONE_WEIGHT_MAX"], "VAT_BONE_WEIGHT_MAX");
        }

        [Test]
        public void PieceTexel_InHalfArithmetic_DecodesEveryPiece()
        {
            for (var piece = 0; piece < VatRigidFormat.MaxPieces; piece++)
            {
                var high = Byte(Half((piece >> ByteBits) / VatMath.ByteMax));
                var low = Byte(Half((piece & ByteMask) / VatMath.ByteMax));
                var texel = Half(Half(high * VatMath.ByteSteps * VatMath.TexelsPerBone) + Half(low * VatMath.TexelsPerBone));

                Assert.AreEqual(piece * VatMath.TexelsPerBone, (int)texel, $"piece {piece}");
            }
        }

        private static float Byte(float uv)
        {
            return Mathf.Floor(Half(Half(uv * VatMath.ByteMax) + Rounding));
        }

        private static float Half(float value)
        {
            return Mathf.HalfToFloat(Mathf.FloatToHalf(value));
        }

        private static Dictionary<string, double> Defines()
        {
            var defines = new Dictionary<string, double>();
            foreach (Match match in Regex.Matches(File.ReadAllText(CorePath), @"#define (VAT_\w+) \(?([\d.]+)(?: / ([\d.]+))?\)?"))
            {
                var value = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
                var divisor = match.Groups[3].Success ? double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture) : 1.0;
                defines[match.Groups[1].Value] = value / divisor;
            }

            return defines;
        }
    }
}
