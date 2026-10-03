using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatMatrix3d
    {
        private const int MaxPolarIterations = 32;
        private const double PolarTolerance = 1e-12;
        private const double Half = 0.5;
        private const double Quarter = 0.25;

        private readonly double _m00;
        private readonly double _m01;
        private readonly double _m02;
        private readonly double _m10;
        private readonly double _m11;
        private readonly double _m12;
        private readonly double _m20;
        private readonly double _m21;
        private readonly double _m22;

        public double Determinant =>
            _m00 * (_m11 * _m22 - _m12 * _m21) - _m01 * (_m10 * _m22 - _m12 * _m20) + _m02 * (_m10 * _m21 - _m11 * _m20);

        public double FrobeniusNorm =>
            Math.Sqrt(_m00 * _m00 + _m01 * _m01 + _m02 * _m02 + _m10 * _m10 + _m11 * _m11 + _m12 * _m12 + _m20 * _m20 + _m21 * _m21 + _m22 * _m22);

        private VatMatrix3d(double m00, double m01, double m02, double m10, double m11, double m12, double m20, double m21, double m22)
        {
            _m00 = m00;
            _m01 = m01;
            _m02 = m02;
            _m10 = m10;
            _m11 = m11;
            _m12 = m12;
            _m20 = m20;
            _m21 = m21;
            _m22 = m22;
        }

        public static VatMatrix3d Linear(Matrix4x4 matrix)
        {
            return new VatMatrix3d(matrix.m00, matrix.m01, matrix.m02, matrix.m10, matrix.m11, matrix.m12, matrix.m20, matrix.m21, matrix.m22);
        }

        public static VatMatrix3d operator +(VatMatrix3d a, VatMatrix3d b)
        {
            return new VatMatrix3d(a._m00 + b._m00, a._m01 + b._m01, a._m02 + b._m02, a._m10 + b._m10, a._m11 + b._m11, a._m12 + b._m12,
                a._m20 + b._m20, a._m21 + b._m21, a._m22 + b._m22);
        }

        public static VatMatrix3d operator -(VatMatrix3d a, VatMatrix3d b)
        {
            return a + b * -1.0;
        }

        public static VatMatrix3d operator *(VatMatrix3d a, double k)
        {
            return new VatMatrix3d(a._m00 * k, a._m01 * k, a._m02 * k, a._m10 * k, a._m11 * k, a._m12 * k, a._m20 * k, a._m21 * k, a._m22 * k);
        }

        public Vector3 Transform(Vector3 vector)
        {
            return new Vector3(
                (float)(_m00 * vector.x + _m01 * vector.y + _m02 * vector.z),
                (float)(_m10 * vector.x + _m11 * vector.y + _m12 * vector.z),
                (float)(_m20 * vector.x + _m21 * vector.y + _m22 * vector.z));
        }

        public VatMatrix3d PolarRotation()
        {
            var rotation = this * (1.0 / Math.Cbrt(Determinant));
            for (var i = 0; i < MaxPolarIterations; i++)
            {
                var next = (rotation + rotation.InverseTranspose()) * Half;
                var step = (next - rotation).FrobeniusNorm;
                rotation = next;
                if (step < PolarTolerance)
                {
                    break;
                }
            }

            return rotation;
        }

        public Vector4 Quaternion()
        {
            var trace = _m00 + _m11 + _m22;
            if (trace > 0.0)
            {
                var s = Math.Sqrt(trace + 1.0) * 2.0;
                return Normalized(Quarter * s, (_m21 - _m12) / s, (_m02 - _m20) / s, (_m10 - _m01) / s);
            }

            if (_m00 > _m11 && _m00 > _m22)
            {
                var s = Math.Sqrt(1.0 + _m00 - _m11 - _m22) * 2.0;
                return Normalized((_m21 - _m12) / s, Quarter * s, (_m01 + _m10) / s, (_m02 + _m20) / s);
            }

            if (_m11 > _m22)
            {
                var s = Math.Sqrt(1.0 + _m11 - _m00 - _m22) * 2.0;
                return Normalized((_m02 - _m20) / s, (_m01 + _m10) / s, Quarter * s, (_m12 + _m21) / s);
            }

            var t = Math.Sqrt(1.0 + _m22 - _m00 - _m11) * 2.0;
            return Normalized((_m10 - _m01) / t, (_m02 + _m20) / t, (_m12 + _m21) / t, Quarter * t);
        }

        private VatMatrix3d InverseTranspose()
        {
            var inverseDeterminant = 1.0 / Determinant;
            return new VatMatrix3d(
                (_m11 * _m22 - _m12 * _m21) * inverseDeterminant,
                (_m12 * _m20 - _m10 * _m22) * inverseDeterminant,
                (_m10 * _m21 - _m11 * _m20) * inverseDeterminant,
                (_m02 * _m21 - _m01 * _m22) * inverseDeterminant,
                (_m00 * _m22 - _m02 * _m20) * inverseDeterminant,
                (_m01 * _m20 - _m00 * _m21) * inverseDeterminant,
                (_m01 * _m12 - _m02 * _m11) * inverseDeterminant,
                (_m02 * _m10 - _m00 * _m12) * inverseDeterminant,
                (_m00 * _m11 - _m01 * _m10) * inverseDeterminant);
        }

        private static Vector4 Normalized(double w, double x, double y, double z)
        {
            var length = Math.Sqrt(w * w + x * x + y * y + z * z);
            return new Vector4((float)(x / length), (float)(y / length), (float)(z / length), (float)(w / length));
        }
    }
}
