using System;

namespace VATyakov.Editor
{
    internal static class VatHornRotation
    {
        private const int Size = 4;
        private const int MaxSweeps = 50;
        private const double Epsilon = 1e-15;

        public static double[,] Solve(double[,] covariance)
        {
            var quaternion = LargestEigenvector(Horn(covariance));
            return Rotation(quaternion[0], quaternion[1], quaternion[2], quaternion[3]);
        }

        private static double[,] Horn(double[,] s)
        {
            var xx = s[0, 0];
            var xy = s[0, 1];
            var xz = s[0, 2];
            var yx = s[1, 0];
            var yy = s[1, 1];
            var yz = s[1, 2];
            var zx = s[2, 0];
            var zy = s[2, 1];
            var zz = s[2, 2];
            return new[,]
            {
                { xx + yy + zz, yz - zy, zx - xz, xy - yx },
                { yz - zy, xx - yy - zz, xy + yx, zx + xz },
                { zx - xz, xy + yx, yy - xx - zz, yz + zy },
                { xy - yx, zx + xz, yz + zy, zz - xx - yy },
            };
        }

        private static double[] LargestEigenvector(double[,] matrix)
        {
            var vectors = new double[Size, Size];
            for (var i = 0; i < Size; i++)
            {
                vectors[i, i] = 1.0;
            }

            var scale = Epsilon * Math.Max(Norm(matrix), Epsilon);
            for (var sweep = 0; sweep < MaxSweeps && OffDiagonal(matrix) > scale; sweep++)
            {
                for (var p = 0; p < Size - 1; p++)
                {
                    for (var q = p + 1; q < Size; q++)
                    {
                        Rotate(matrix, vectors, p, q);
                    }
                }
            }

            var largest = 0;
            for (var i = 1; i < Size; i++)
            {
                if (matrix[i, i] > matrix[largest, largest])
                {
                    largest = i;
                }
            }

            var vector = new double[Size];
            for (var i = 0; i < Size; i++)
            {
                vector[i] = vectors[i, largest];
            }

            return vector;
        }

        private static void Rotate(double[,] a, double[,] v, int p, int q)
        {
            if (a[p, q] == 0.0)
            {
                return;
            }

            var theta = (a[q, q] - a[p, p]) / (2.0 * a[p, q]);
            var t = (theta >= 0.0 ? 1.0 : -1.0) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1.0));
            var c = 1.0 / Math.Sqrt(t * t + 1.0);
            var s = t * c;
            for (var k = 0; k < Size; k++)
            {
                var kp = a[k, p];
                var kq = a[k, q];
                a[k, p] = c * kp - s * kq;
                a[k, q] = s * kp + c * kq;
            }

            for (var k = 0; k < Size; k++)
            {
                var pk = a[p, k];
                var qk = a[q, k];
                a[p, k] = c * pk - s * qk;
                a[q, k] = s * pk + c * qk;
            }

            for (var k = 0; k < Size; k++)
            {
                var kp = v[k, p];
                var kq = v[k, q];
                v[k, p] = c * kp - s * kq;
                v[k, q] = s * kp + c * kq;
            }
        }

        private static double OffDiagonal(double[,] matrix)
        {
            var sum = 0.0;
            for (var i = 0; i < Size; i++)
            {
                for (var j = 0; j < Size; j++)
                {
                    if (i != j)
                    {
                        sum += matrix[i, j] * matrix[i, j];
                    }
                }
            }

            return Math.Sqrt(sum);
        }

        private static double Norm(double[,] matrix)
        {
            var sum = 0.0;
            foreach (var value in matrix)
            {
                sum += value * value;
            }

            return Math.Sqrt(sum);
        }

        private static double[,] Rotation(double w, double x, double y, double z)
        {
            var length = Math.Sqrt(w * w + x * x + y * y + z * z);
            w /= length;
            x /= length;
            y /= length;
            z /= length;
            return new[,]
            {
                { 1.0 - 2.0 * (y * y + z * z), 2.0 * (x * y - w * z), 2.0 * (x * z + w * y) },
                { 2.0 * (x * y + w * z), 1.0 - 2.0 * (x * x + z * z), 2.0 * (y * z - w * x) },
                { 2.0 * (x * z - w * y), 2.0 * (y * z + w * x), 1.0 - 2.0 * (x * x + y * y) },
            };
        }
    }
}
