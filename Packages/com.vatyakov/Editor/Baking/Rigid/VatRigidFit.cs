using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatRigidFit
    {
        private const int Axes = 3;

        public static Matrix4x4 Solve(IReadOnlyList<Vector3> rest, IReadOnlyList<Vector3> current, int[] vertices)
        {
            var restCenter = Center(rest, vertices);
            var currentCenter = Center(current, vertices);
            var covariance = new double[Axes, Axes];
            foreach (var vertex in vertices)
            {
                var from = rest[vertex];
                var to = current[vertex];
                for (var i = 0; i < Axes; i++)
                {
                    for (var j = 0; j < Axes; j++)
                    {
                        covariance[i, j] += (from[i] - restCenter[i]) * (to[j] - currentCenter[j]);
                    }
                }
            }

            return Compose(VatHornRotation.Solve(covariance), restCenter, currentCenter);
        }

        private static double[] Center(IReadOnlyList<Vector3> positions, int[] vertices)
        {
            var center = new double[Axes];
            foreach (var vertex in vertices)
            {
                for (var axis = 0; axis < Axes; axis++)
                {
                    center[axis] += positions[vertex][axis];
                }
            }

            for (var axis = 0; axis < Axes; axis++)
            {
                center[axis] /= vertices.Length;
            }

            return center;
        }

        private static Matrix4x4 Compose(double[,] rotation, double[] restCenter, double[] currentCenter)
        {
            var matrix = Matrix4x4.identity;
            for (var row = 0; row < Axes; row++)
            {
                var translation = currentCenter[row];
                for (var column = 0; column < Axes; column++)
                {
                    matrix[row, column] = (float)rotation[row, column];
                    translation -= rotation[row, column] * restCenter[column];
                }

                matrix[row, Axes] = (float)translation;
            }

            return matrix;
        }
    }
}
