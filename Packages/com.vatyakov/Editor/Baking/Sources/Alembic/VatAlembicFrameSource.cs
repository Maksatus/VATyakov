#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicFrameSource : IVatFrameSource
    {
        private const string HiddenNodesWarning =
            "some mesh nodes are hidden on some frames, and Vertex mode bakes them visible: use Mode = Rigid for pieces that appear or disappear.";

        private readonly string _name;
        private readonly VatSourceClip[] _clips;
        private readonly List<string> _warnings = new();

        private VatAlembicCopy _copy;
        private MeshFilter[] _nodes;
        private VatAlembicReader _reader;
        private VatVertexJumps _jumps;
        private bool _hasHiddenNodes;

        public VatSourceMesh Mesh { get; private set; }
        public IReadOnlyList<VatSourceClip> Clips => _clips;
        public IReadOnlyList<string> Warnings => CurrentWarnings();

        public VatAlembicFrameSource(GameObject alembic)
        {
            _name = alembic.name;
            _clips = new[] { new VatSourceClip(_name, VatAlembicDuration.Require(alembic), false) };
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
            _copy.Sample(time);
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
            _nodes = _copy.Nodes;
            _reader = new VatAlembicReader(_nodes);
            Mesh = _reader.SourceMesh(_name);
            _jumps = new VatVertexJumps(Mesh.VertexCount);
            var withoutUv = Array.FindAll(_nodes, node => node.sharedMesh.uv.Length != node.sharedMesh.vertexCount).Length;
            if (withoutUv > 0)
            {
                _warnings.Add(FormattableString.Invariant(
                    $"{withoutUv} of {_nodes.Length} mesh nodes have no UV, use the triplanar template '{VatBaker.TriplanarShaderName}'."));
            }

            var lost = VatLostAttributes.Warning(_name, Array.ConvertAll(_nodes, node => node.sharedMesh));
            if (lost != null)
            {
                _warnings.Add(lost);
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
    }
}
#endif
