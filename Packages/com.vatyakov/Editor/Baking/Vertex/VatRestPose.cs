using System;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: rest = frame 0 of clip 0 = mesh vertices.
    sealed class VatRestPose
    {
        public readonly Vector3[] Positions;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;

        VatRestPose(VatFrame frame)
        {
            Positions = (Vector3[])frame.Positions.Clone();
            Normals = (Vector3[])frame.Normals.Clone();
            Tangents = (Vector4[])frame.Tangents.Clone();
        }

        public static VatRestPose Capture(int clip, int frame, VatFrame data)
        {
            if (clip != 0 || frame != 0)
                throw new InvalidOperationException("Frame 0 of clip 0 defines the rest pose and must come first.");
            return new VatRestPose(data);
        }
    }
}
