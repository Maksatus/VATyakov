#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidPieceExtractor : IVatRigidSource
    {
        private const string RecipeHint =
            "Rigid mode bakes pieces with a constant topology: export from Houdini with Transform Geometry and s@path, or with Deform Geometry " +
            "(see the package README). No mode bakes a changing topology yet: keep every piece in the .abc " +
            "and hide it with the visibility instead of deleting it.";

        private const string SampleTimesMissing =
            "couldn't read the sample times of the .abc from the Alembic package: the fps check takes 3 points between frames, " +
            "and deforming meshes are fitted on every baked frame.";

        private readonly string _name;
        private readonly List<string> _warnings = new();
        private readonly List<Vector3> _positions = new();

        private VatAlembicCopy _copy;
        private MeshFilter[] _nodes;
        private VatAlembicTopology _topology;

        public VatSourceClip Clip { get; }
        public int PieceCount => _nodes.Length;
        public IReadOnlyList<double> SampleTimes { get; private set; }
        public IReadOnlyList<string> Warnings => _warnings;

        public VatRigidPieceExtractor(GameObject alembic)
        {
            _name = alembic.name;
            Clip = new VatSourceClip(_name, VatAlembicDuration.Require(alembic));
            try
            {
                Open(alembic);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public VatRigidTrack[] Extract(VatClip clip, VatRigidInnerTimes inner)
        {
            var nodes = Array.ConvertAll(_nodes, filter => new VatRigidNode(Track(filter, clip.FrameCount, inner.Count)));
            try
            {
                for (var frame = 0; frame < clip.FrameCount; frame++)
                {
                    VatBakeProgress.Report(clip, frame);
                    var time = clip.FrameTime(frame);
                    _copy.Player.UpdateImmediately((float)time);
                    RequireTopology(time);
                    Sample(nodes, frame, time, IsSourceTime(time));
                }

                for (var sample = 0; sample < inner.Count; sample++)
                {
                    VatBakeProgress.ReportBetween(clip, sample, inner.Count);
                    var time = inner.Times[sample];
                    _copy.Player.UpdateImmediately((float)time);
                    RequireTopology(time);
                    Sample(nodes, clip.FrameCount + sample, time, SampleTimes != null);
                }
            }
            finally
            {
                VatBakeProgress.Clear();
            }

            return VatRigidSplit.Pieces(nodes, _name);
        }

        public void Dispose()
        {
            _copy?.Dispose();
        }

        private void Open(GameObject alembic)
        {
            _copy = new VatAlembicCopy(alembic);
            _copy.Player.UpdateImmediately(0f);
            _nodes = _copy.Meshes();
            if (_nodes.Length == 0)
            {
                throw new VatBakeException($"'{_name}' has no meshes, nothing to bake. Check that Meshes are enabled in the .abc importer.");
            }

            _topology = VatAlembicTopology.Capture(_nodes);
            SampleTimes = VatAlembicSampleTimes.Read(_copy.Player);
            if (SampleTimes == null)
            {
                _warnings.Add(SampleTimesMissing);
            }

            if (Array.Exists(_nodes, node => node.sharedMesh.uv.Length != node.sharedMesh.vertexCount))
            {
                _warnings.Add("some pieces have no UV, use a triplanar template.");
            }
        }

        private static VatRigidTrack Track(MeshFilter filter, int frameCount, int innerCount)
        {
            var mesh = filter.sharedMesh;
            var local = new VatFrame(mesh.vertexCount);
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            mesh.vertices.CopyTo(local.Positions, 0);
            for (var vertex = 0; vertex < local.Positions.Length; vertex++)
            {
                local.Normals[vertex] = normals.Length == local.Positions.Length ? normals[vertex] : VatFrame.MissingNormal;
                local.Tangents[vertex] = tangents.Length == local.Positions.Length ? tangents[vertex] : VatFrame.MissingTangent;
            }

            return new VatRigidTrack(filter.name, VatSourceMesh.Read(mesh), local, frameCount, innerCount);
        }

        private void RequireTopology(double time)
        {
            var difference = _topology.Difference(VatAlembicTopology.Capture(_nodes));
            if (difference != null)
            {
                throw new VatBakeException(FormattableString.Invariant($"'{_name}': topology changes at {time:0.###} s ({difference}). {RecipeHint}"));
            }
        }

        private bool IsSourceTime(double time)
        {
            return SampleTimes == null || VatRigidInnerTimes.Contains(SampleTimes, time);
        }

        private void Sample(VatRigidNode[] nodes, int sample, double time, bool isSourceSample)
        {
            for (var index = 0; index < _nodes.Length; index++)
            {
                var filter = _nodes[index];
                filter.sharedMesh.GetVertices(_positions);
                nodes[index].Sample(sample, _positions, filter.transform.localToWorldMatrix, filter.gameObject.activeInHierarchy, time, isSourceSample);
            }
        }
    }
}
#endif
