using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;

namespace VATyakov.Editor
{
    // §2.2, §2.3: an .abc with constant topology, all mesh nodes as one mesh. Time k / fps goes straight to
    // UpdateImmediately: it counts from StartTime and is clamped to [0, Duration], absolute file time is wrong there.
    sealed class AlembicFrameSource : IVatFrameSource
    {
        readonly string _name;
        readonly VatSourceClip[] _clips;
        readonly List<string> _warnings = new List<string>();
        VatAlembicCopy _copy;
        VatAlembicReader _reader;
        VatAlembicTopology _topology;
        VatVertexJumps _jumps;

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

        public VatSourceMesh Mesh { get; private set; }

        public IReadOnlyList<VatSourceClip> Clips => _clips;

        public IReadOnlyList<string> Warnings => _jumps.Warning == null ? _warnings : _warnings.Append(_jumps.Warning).ToList();

        public void Sample(int clip, double time, VatFrame frame)
        {
            _copy.Player.UpdateImmediately((float)time);
            RequireTopology(time);
            _reader.Read(frame);
            _jumps.Check(frame.Positions, time);
        }

        public void Dispose() => _copy?.Dispose();

        void Open(GameObject alembic)
        {
            _copy = new VatAlembicCopy(alembic);
            _copy.Player.UpdateImmediately(0f);
            var meshes = _copy.Meshes();
            if (meshes.Length == 0)
                throw new VatBakeException($"'{_name}' has no meshes, nothing to bake. Check that Meshes are enabled in the .abc importer.");
            _reader = new VatAlembicReader(meshes);
            _topology = VatAlembicTopology.Capture(meshes);
            Mesh = _reader.SourceMesh(_name);
            _jumps = new VatVertexJumps(Mesh.VertexCount);
            if (Mesh.Uv0 == null)
                _warnings.Add($"the mesh has no UV, use the triplanar template '{VatBaker.TriplanarShaderName}'.");
        }

        void RequireTopology(double time)
        {
            string difference = _topology.Difference(VatAlembicTopology.Capture(_copy.Meshes()));
            if (difference != null)
                throw new VatBakeException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}': topology changes at {1:0.###} s ({2}). Vertex mode bakes constant topology only: " +
                    "stable point order, no remeshing. Changing topology arrives in patch 2.", _name, time, difference));
        }

        float RequireDuration(GameObject alembic)
        {
            var player = alembic.GetComponent<AlembicStreamPlayer>();
            if (player == null)
                throw new VatBakeException($"'{_name}' is not an Alembic: no AlembicStreamPlayer on the root. Assign an .abc from the project.");
            float duration = player.Duration;
            if (!float.IsFinite(duration) || duration <= 0f)
                throw new VatBakeException(string.Format(CultureInfo.InvariantCulture,
                    "'{0}' has a duration of {1} s, nothing to bake. Check the Time Range of the .abc.", _name, duration));
            return duration;
        }
    }
}
