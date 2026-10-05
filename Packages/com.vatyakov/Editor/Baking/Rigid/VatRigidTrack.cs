using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidTrack
    {
        public readonly string Name;
        public readonly VatSourceMesh Source;
        public readonly VatFrame Local;
        public readonly Matrix4x4[] Frames;
        public readonly bool[] Visible;
        public readonly Matrix4x4[] Inner;
        public readonly bool[] InnerVisible;

        public int SampleCount => Frames.Length + Inner.Length;

        public VatRigidTrack(string name, VatSourceMesh source, VatFrame local, int frameCount, int innerCount)
        {
            Name = name;
            Source = source;
            Local = local;
            Frames = new Matrix4x4[frameCount];
            Visible = new bool[frameCount];
            Inner = new Matrix4x4[innerCount];
            InnerVisible = new bool[innerCount];
        }

        public Matrix4x4 Sample(int sample)
        {
            return sample < Frames.Length ? Frames[sample] : Inner[sample - Frames.Length];
        }

        public bool IsVisibleAt(int sample)
        {
            return sample < Frames.Length ? Visible[sample] : InnerVisible[sample - Frames.Length];
        }

        public void SetSample(int sample, Matrix4x4 matrix, bool isVisible)
        {
            if (sample < Frames.Length)
            {
                Frames[sample] = matrix;
                Visible[sample] = isVisible;
            }
            else
            {
                Inner[sample - Frames.Length] = matrix;
                InnerVisible[sample - Frames.Length] = isVisible;
            }
        }
    }
}
