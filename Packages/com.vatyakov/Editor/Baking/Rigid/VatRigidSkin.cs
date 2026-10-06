using System;

namespace VATyakov.Editor
{
    internal static class VatRigidSkin
    {
        public static VatBoneSkin Build(string name, VatRigidPiece[] pieces)
        {
            var source = VatSourceMeshes.MergeSlots(name, Array.ConvertAll(pieces, piece => piece.Source));
            var frame = new VatFrame(source.VertexCount);
            var influences = new VatBoneInfluence[source.VertexCount];
            var offset = 0;
            for (var index = 0; index < pieces.Length; index++)
            {
                pieces[index].WriteRest(frame, offset);
                Array.Fill(influences, VatBoneInfluence.Single(index), offset, pieces[index].VertexCount);
                offset += pieces[index].VertexCount;
            }

            var basis = new VatTangentFrames(source.VertexCount);
            basis.Build(0, frame);
            return new VatBoneSkin(name, source, influences, new VatRestPose(frame, basis));
        }
    }
}
