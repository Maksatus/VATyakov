using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidPivot
    {
        private const double MinConditioning = 1e-6;
        private const double MinTrace = 1e-12;
        private const int Size = 3;

        private readonly double[,] _normal = new double[Size, Size];
        private readonly double[] _right = new double[Size];

        public static Vector3 Solve(Matrix4x4[] motions, VatRigidVisibility visibility, Bounds rest)
        {
            var pivot = new VatRigidPivot();
            for (var frame = 1; frame < motions.Length - 1; frame++)
            {
                if (visibility.IsVisible(frame - 1) && visibility.IsVisible(frame) && visibility.IsVisible(frame + 1))
                {
                    pivot.Add(motions[frame - 1], motions[frame], motions[frame + 1]);
                }
            }

            return pivot.TrySolve(out var point) && Vector3.Distance(point, rest.center) <= rest.extents.magnitude ? point : rest.center;
        }

        private void Add(Matrix4x4 previous, Matrix4x4 current, Matrix4x4 next)
        {
            var acceleration = new double[Size, Size + 1];
            for (var row = 0; row < Size; row++)
            {
                for (var column = 0; column <= Size; column++)
                {
                    acceleration[row, column] = (double)next[row, column] - 2.0 * current[row, column] + previous[row, column];
                }
            }

            for (var j = 0; j < Size; j++)
            {
                for (var i = 0; i < Size; i++)
                {
                    _right[j] += acceleration[i, j] * acceleration[i, Size];
                    for (var k = 0; k < Size; k++)
                    {
                        _normal[j, k] += acceleration[i, j] * acceleration[i, k];
                    }
                }
            }
        }

        private bool TrySolve(out Vector3 point)
        {
            point = Vector3.zero;
            var trace = _normal[0, 0] + _normal[1, 1] + _normal[2, 2];
            var determinant = Determinant();
            var mean = trace / Size;
            if (!(trace > MinTrace) || !(determinant > MinConditioning * mean * mean * mean))
            {
                return false;
            }

            for (var axis = 0; axis < Size; axis++)
            {
                point[axis] = (float)(-ReplacedDeterminant(axis) / determinant);
            }

            return float.IsFinite(point.x) && float.IsFinite(point.y) && float.IsFinite(point.z);
        }

        private double Determinant()
        {
            return Determinant3(Column(0), Column(1), Column(2));
        }

        private double ReplacedDeterminant(int axis)
        {
            return Determinant3(axis == 0 ? _right : Column(0), axis == 1 ? _right : Column(1), axis == 2 ? _right : Column(2));
        }

        private double[] Column(int column)
        {
            return new[] { _normal[0, column], _normal[1, column], _normal[2, column] };
        }

        private static double Determinant3(double[] a, double[] b, double[] c)
        {
            return a[0] * (b[1] * c[2] - b[2] * c[1]) - b[0] * (a[1] * c[2] - a[2] * c[1]) + c[0] * (a[1] * b[2] - a[2] * b[1]);
        }
    }
}
