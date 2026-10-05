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
    }
}
