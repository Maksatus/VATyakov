using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatSimilarity
    {
        private const double MinNorm = 1e-6;

        public static readonly Vector4 Identity = new(0f, 0f, 0f, 1f);

        public readonly double Determinant;
        public readonly float Scale;
        public readonly Vector4 Rotation;

        private readonly VatMatrix3d _remainder;

        public bool IsPoint => Scale == 0f;
        public bool IsProper => Determinant > 0.0 || IsPoint;

        private VatSimilarity(double determinant, float scale, Vector4 rotation, VatMatrix3d remainder)
        {
            Determinant = determinant;
            Scale = scale;
            Rotation = rotation;
            _remainder = remainder;
        }

        public static VatSimilarity Of(Matrix4x4 skin)
        {
            var linear = VatMatrix3d.Linear(skin);
            if (linear.FrobeniusNorm < MinNorm)
            {
                return new VatSimilarity(0.0, 0f, Identity, linear);
            }

            var determinant = linear.Determinant;
            if (!(determinant > 0.0))
            {
                return new VatSimilarity(determinant, 1f, Identity, linear);
            }

            var scale = Math.Cbrt(determinant);
            var rotation = linear.PolarRotation();
            return new VatSimilarity(determinant, (float)scale, rotation.Quaternion(), linear - rotation * scale);
        }

        public Vector4 RotationAfter(Vector4 previous)
        {
            return IsPoint ? previous : Rotation;
        }

        public float Error(Vector3 offset)
        {
            return _remainder.Transform(offset).magnitude;
        }
    }
}
