#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicFrameSource : IVatFrameSource
    {
        private const string ConstantTopologyHint =
            "Vertex mode bakes constant topology only: stable point order, no remeshing. Changing topology arrives in patch 2.";
        private const string HiddenNodesWarning =
            "some mesh nodes are hidden on some frames, and Vertex mode bakes them visible: use Mode = Rigid for pieces that appear or disappear.";

        private readonly string _name;
        private readonly VatSourceClip[] _clips;
        private readonly List<string> _warnings = new();

        private VatAlembicCopy _copy;
        private MeshFilter[] _nodes;
        private VatAlembicReader _reader;
        private VatAlembicTopology _topology;
        private VatVertexJumps _jumps;
        private bool _hasHiddenNodes;

        public VatSourceMesh Mesh { get; private set; }
        public IReadOnlyList<VatSourceClip> Clips => _clips;
        public IReadOnlyList<string> Warnings => CurrentWarnings();

        public VatAlembicFrameSource(GameObject alembic)
        {
            _name = alembic.name;
            _clips = new[] { new VatSourceClip(_name, VatAlembicDuration.Require(alembic)) };
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

        public void Sample(int clip, double time, VatFrame frame)
        {
            _copy.Player.UpdateImmediately((float)time);
            RequireTopology(time);
            _reader.Read(frame);
            _jumps.Check(frame.Positions, time);
            _hasHiddenNodes |= Array.Exists(_nodes, node => !node.gameObject.activeInHierarchy);
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

            _reader = new VatAlembicReader(_nodes);
            _topology = VatAlembicTopology.Capture(_nodes);
            Mesh = _reader.SourceMesh(_name);
            _jumps = new VatVertexJumps(Mesh.VertexCount);
            var withoutUv = Array.FindAll(_nodes, node => node.sharedMesh.uv.Length != node.sharedMesh.vertexCount).Length;
            if (withoutUv > 0)
            {
                _warnings.Add(FormattableString.Invariant(
                    $"{withoutUv} of {_nodes.Length} mesh nodes have no UV, use the triplanar template '{VatBaker.TriplanarShaderName}'."));
            }
        }

        private List<string> CurrentWarnings()
        {
            var warnings = new List<string>(_warnings);
            if (_hasHiddenNodes)
            {
                warnings.Add(HiddenNodesWarning);
            }

            if (_jumps.Warning != null)
            {
                warnings.Add(_jumps.Warning);
            }

            return warnings;
        }

        private void RequireTopology(double time)
        {
            var difference = _topology.Difference(VatAlembicTopology.Capture(_copy.Meshes()));
            if (difference != null)
            {
                throw new VatBakeException(
                    FormattableString.Invariant($"'{_name}': topology changes at {time:0.###} s ({difference}). {ConstantTopologyHint}"));
            }
        }
    }
}
#endif
