using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatSimilarity
    {
        private static readonly Vector4 _identity = new(0f, 0f, 0f, 1f);

        public readonly double Determinant;
        public readonly float Scale;
        public readonly Vector4 Rotation;
        public readonly double Residual;

        private readonly VatMatrix3d _remainder;

        public bool IsProper => Determinant > 0.0;

        private VatSimilarity(double determinant, float scale, Vector4 rotation, double residual, VatMatrix3d remainder)
        {
            Determinant = determinant;
            Scale = scale;
            Rotation = rotation;
            Residual = residual;
            _remainder = remainder;
        }

        public static VatSimilarity Of(Matrix4x4 skin)
        {
            var linear = VatMatrix3d.Linear(skin);
            var determinant = linear.Determinant;
            if (!(determinant > 0.0))
            {
                return new VatSimilarity(determinant, 1f, _identity, double.PositiveInfinity, linear);
            }

            var scale = Math.Cbrt(determinant);
            var rotation = linear.PolarRotation();
            var remainder = linear - rotation * scale;
            return new VatSimilarity(determinant, (float)scale, rotation.Quaternion(), remainder.FrobeniusNorm / scale, remainder);
        }

        public float Error(Vector3 offset)
        {
            return _remainder.Transform(offset).magnitude;
        }
    }
}
