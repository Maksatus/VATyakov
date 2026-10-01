using System;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.2: rest = frame 0 of clip 0 = mesh vertices. Normal and tangent are the orthonormal frame-0 basis,
    // tangent.w keeps the source sign.
    sealed class VatRestPose
    {
        public readonly Vector3[] Positions;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;
        public readonly Vector3 Centroid; // drift d = frame centroid − rest centroid (§2.2)

        VatRestPose(VatFrame frame, VatTangentFrames basis)
        {
            Positions = (Vector3[])frame.Positions.Clone();
            Normals = (Vector3[])basis.Normals.Clone();
            Centroid = VatCentroid.Of(Positions);
            Tangents = new Vector4[Positions.Length];
            for (int v = 0; v < Tangents.Length; v++)
                Tangents[v] = new Vector4(basis.Tangents[v].x, basis.Tangents[v].y, basis.Tangents[v].z, frame.Tangents[v].w);
        }

        public static VatRestPose Capture(int clip, int frame, VatFrame data, VatTangentFrames basis)
        {
            if (clip != 0 || frame != 0)
                throw new InvalidOperationException("Frame 0 of clip 0 defines the rest pose and must come first.");
            return new VatRestPose(data, basis);
        }
    }
}
