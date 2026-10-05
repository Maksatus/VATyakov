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
            "(see the package README). Use Mode = Vertex for meshes that change topology.";

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
            var nodes = Array.ConvertAll(_nodes, node => new VatRigidNode(Track(node, clip.FrameCount, inner.Count)));
            try
            {
                for (var frame = 0; frame < clip.FrameCount; frame++)
                {
                    VatBakeProgress.Report(clip, frame);
                    var time = clip.FrameTime(frame);
                    _copy.Player.UpdateImmediately((float)time);
                    RequireTopology(time);
                    Sample(nodes, frame, time);
                }

                for (var sample = 0; sample < inner.Count; sample++)
                {
                    _copy.Player.UpdateImmediately((float)inner.Times[sample]);
                    Sample(nodes, clip.FrameCount + sample, inner.Times[sample]);
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
            if (Array.Exists(_nodes, node => node.sharedMesh.uv.Length != node.sharedMesh.vertexCount))
            {
                _warnings.Add("some pieces have no UV, use a triplanar template.");
            }
        }

        private static VatRigidTrack Track(MeshFilter node, int frameCount, int innerCount)
        {
            var mesh = node.sharedMesh;
            var local = new VatFrame(mesh.vertexCount);
            var normals = mesh.normals;
            var tangents = mesh.tangents;
            mesh.vertices.CopyTo(local.Positions, 0);
            for (var vertex = 0; vertex < local.Positions.Length; vertex++)
            {
                local.Normals[vertex] = normals.Length == local.Positions.Length ? normals[vertex] : VatFrame.MissingNormal;
                local.Tangents[vertex] = tangents.Length == local.Positions.Length ? tangents[vertex] : VatFrame.MissingTangent;
            }

            return new VatRigidTrack(node.name, VatSourceMesh.Read(mesh), local, frameCount, innerCount);
        }

        private void RequireTopology(double time)
        {
            var difference = _topology.Difference(VatAlembicTopology.Capture(_nodes));
            if (difference != null)
            {
                throw new VatBakeException(FormattableString.Invariant($"'{_name}': topology changes at {time:0.###} s ({difference}). {RecipeHint}"));
            }
        }

        private void Sample(VatRigidNode[] nodes, int sample, double time)
        {
            for (var index = 0; index < _nodes.Length; index++)
            {
                var node = _nodes[index];
                node.sharedMesh.GetVertices(_positions);
                nodes[index].Sample(sample, _positions, node.transform.localToWorldMatrix, node.gameObject.activeInHierarchy, time);
            }
        }
    }
}
#endif
