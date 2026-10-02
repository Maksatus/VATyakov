using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRestPose
    {
        public readonly Vector3[] Positions;
        public readonly Vector3[] Normals;
        public readonly Vector4[] Tangents;
        public readonly Vector3 Centroid;

        public VatRestPose(VatFrame frame, VatTangentFrames basis)
        {
            Positions = (Vector3[])frame.Positions.Clone();
            Normals = (Vector3[])basis.Normals.Clone();
            Centroid = VatCentroid.Of(Positions);
            Tangents = new Vector4[Positions.Length];
            for (var vertex = 0; vertex < Tangents.Length; vertex++)
            {
                var tangent = basis.Tangents[vertex];
                Tangents[vertex] = new Vector4(tangent.x, tangent.y, tangent.z, frame.Tangents[vertex].w);
            }
        }
    }
}
