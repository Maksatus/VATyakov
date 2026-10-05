using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal static class VatRigidPipeline
    {
        private const string NothingVisible = "No piece is visible on any baked frame: nothing to bake. Check the visibility in the .abc.";

        public static VatBakeResult Run(IVatRigidSource source, float fps, bool isLooping, string name)
        {
            var requests = VatClipRequest.From(new[] { source.Clip }, fps, isLooping);
            var clip = VatLayout.ForRigid(source.PieceCount, requests).Clips[0];
            var inner = VatRigidInnerTimes.Between(clip, source.SampleTimes);
            var warnings = new List<string>(source.Warnings);
            var pieces = VisiblePieces(source.Extract(clip, inner), warnings);
            var encoder = new VatRigidEncoder(VatLayout.ForRigid(pieces.Length, requests), pieces, VatAssetPath.MeshName(name));
            encoder.EncodeAll();
            var fpsWarning = new VatRigidFpsCheck(encoder, inner).Warning(fps);
            if (fpsWarning != null)
            {
                warnings.Add(fpsWarning);
            }

            return Build(encoder, name, warnings);
        }

        private static VatRigidPiece[] VisiblePieces(VatRigidTrack[] tracks, List<string> warnings)
        {
            var pieces = new List<VatRigidPiece>();
            var hidden = new List<string>();
            foreach (var track in tracks)
            {
                var visibility = new VatRigidVisibility(track.Visible);
                if (visibility.IsNeverVisible)
                {
                    hidden.Add(track.Name);
                    continue;
                }

                pieces.Add(new VatRigidPiece(track, visibility));
            }

            if (pieces.Count == 0)
            {
                throw new VatBakeException(NothingVisible);
            }

            if (hidden.Count > 0)
            {
                warnings.Add(FormattableString.Invariant($"{hidden.Count} pieces are hidden on every baked frame and are left out (first: '{hidden[0]}')."));
            }

            return pieces.ToArray();
        }

        private static VatBakeResult Build(VatRigidEncoder encoder, string name, IReadOnlyList<string> warnings)
        {
            var mesh = encoder.BuildMesh(VatAssetPath.MeshName(name));
            try
            {
                return new VatBakeResult(encoder, mesh, VatBakeTextures.Build(encoder, name), warnings);
            }
            catch
            {
                Object.DestroyImmediate(mesh);
                throw;
            }
        }
    }
}
