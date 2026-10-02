#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace VATyakov.Editor
{
    internal sealed class AlembicFrameSource : IVatFrameSource
    {
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

        public AlembicFrameSource(GameObject alembic)
        {
            _name = alembic != null ? alembic.name : throw new ArgumentNullException(nameof(alembic));
            _clips = new[] { new VatSourceClip(_name, RequireDuration(alembic)) };
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
                throw new VatBakeException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}': topology changes at {1:0.###} s ({2}). Vertex mode bakes constant topology only: " +
                    "stable point order, no remeshing. Changing topology arrives in patch 2.", _name, time, difference));
            }
        }

        private float RequireDuration(GameObject alembic)
        {
            var player = alembic.GetComponent<AlembicStreamPlayer>();
            if (player == null)
            {
                throw new VatBakeException($"'{_name}' is not an Alembic: no AlembicStreamPlayer on the root. Assign an .abc from the project.");
            }

            var duration = player.Duration;
            if (!float.IsFinite(duration) || duration <= 0f)
            {
                throw new VatBakeException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' has a duration of {1} s, nothing to bake. Check the Time Range of the .abc.", _name, duration));
            }

            return duration;
        }
    }
}
#endif
