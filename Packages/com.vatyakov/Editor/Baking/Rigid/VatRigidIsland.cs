using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidIsland
    {
        public readonly int Index;
        public readonly int[] Vertices;
        public readonly Vector3 Center;
        public readonly Matrix4x4[] Fits;

        public float Residual { get; private set; }
        public double ResidualTime { get; private set; }

        public VatRigidIsland(int index, int[] vertices, Vector3[] rest, int sampleCount)
        {
            Index = index;
            Vertices = vertices;
            Center = Centroid(rest, vertices);
            Fits = new Matrix4x4[sampleCount];
        }

        public void Hold(int sample)
        {
            Fits[sample] = Matrix4x4.identity;
        }

        public void Fit(int sample, Vector3[] rest, IReadOnlyList<Vector3> current, Matrix4x4 localToWorld, double time)
        {
            var fit = VatRigidFit.Solve(rest, current, Vertices);
            Fits[sample] = fit;
            var residual = 0f;
            foreach (var vertex in Vertices)
            {
                residual = Mathf.Max(residual, localToWorld.MultiplyVector(fit.MultiplyPoint3x4(rest[vertex]) - current[vertex]).magnitude);
            }

            if (residual > Residual)
            {
                Residual = residual;
                ResidualTime = time;
            }
        }

        private static Vector3 Centroid(Vector3[] rest, int[] vertices)
        {
            var points = new Vector3[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                points[i] = rest[vertices[i]];
            }

            return VatCentroid.Of(points);
        }
    }
}
