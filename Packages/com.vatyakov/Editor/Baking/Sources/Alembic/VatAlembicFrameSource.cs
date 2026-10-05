#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicFrameSource : IVatFrameSource
    {
        private const string ConstantTopologyHint =
            "Vertex mode bakes constant topology only: stable point order, no remeshing. Changing topology arrives in patch 2.";

        private readonly string _name;
        private readonly VatSourceClip[] _clips;
        private readonly List<string> _warnings = new();

        private VatAlembicCopy _copy;
        private VatAlembicReader _reader;
        private VatAlembicTopology _topology;
        private VatVertexJumps _jumps;

        public VatSourceMesh Mesh { get; private set; }
        public IReadOnlyList<VatSourceClip> Clips => _clips;
        public IReadOnlyList<string> Warnings => _jumps.Warning == null ? _warnings : _warnings.Append(_jumps.Warning).ToList();

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
        }

        public void Dispose()
        {
            _copy?.Dispose();
        }

        private void Open(GameObject alembic)
        {
            _copy = new VatAlembicCopy(alembic);
            _copy.Player.UpdateImmediately(0f);
            var meshes = _copy.Meshes();
            if (meshes.Length == 0)
            {
                throw new VatBakeException($"'{_name}' has no meshes, nothing to bake. Check that Meshes are enabled in the .abc importer.");
            }

            _reader = new VatAlembicReader(meshes);
            _topology = VatAlembicTopology.Capture(meshes);
            Mesh = _reader.SourceMesh(_name);
            _jumps = new VatVertexJumps(Mesh.VertexCount);
            if (Mesh.Uv0 == null)
            {
                _warnings.Add($"the mesh has no UV, use the triplanar template '{VatBaker.TriplanarShaderName}'.");
            }
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
