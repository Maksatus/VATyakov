#if VAT_ALEMBIC
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidPieceExtractor : IVatRigidSource
    {
        private const float MaxDeformation = 1e-5f;
        private const float MillimetersPerMeter = 1000f;
        private const string RecipeHint =
            "Rigid mode bakes pieces moved by xform nodes: export from Houdini with Transform Geometry and s@path (see the package README). " +
            "Use Mode = Vertex for deforming meshes.";

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
            var tracks = Array.ConvertAll(_nodes, node => Track(node, clip.FrameCount, inner.Count));
            try
            {
                for (var frame = 0; frame < clip.FrameCount; frame++)
                {
                    VatBakeProgress.Report(clip, frame);
                    var time = clip.FrameTime(frame);
                    _copy.Player.UpdateImmediately((float)time);
                    RequireRigid(tracks, time);
                    ReadFrame(tracks, frame);
                }

                for (var sample = 0; sample < inner.Count; sample++)
                {
                    _copy.Player.UpdateImmediately((float)inner.Times[sample]);
                    ReadInner(tracks, sample);
                }
            }
            finally
            {
                VatBakeProgress.Clear();
            }

            return tracks;
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

        private void RequireRigid(VatRigidTrack[] tracks, double time)
        {
            var difference = _topology.Difference(VatAlembicTopology.Capture(_nodes));
            if (difference != null)
            {
                throw new VatBakeException(FormattableString.Invariant($"'{_name}': topology changes at {time:0.###} s ({difference}). {RecipeHint}"));
            }

            for (var index = 0; index < _nodes.Length; index++)
            {
                var deformation = Deformation(_nodes[index].sharedMesh, tracks[index].Local.Positions);
                if (deformation > MaxDeformation)
                {
                    var amount = FormattableString.Invariant($"{deformation * MillimetersPerMeter:0.###} mm at {time:0.###} s");
                    throw new VatBakeException($"Piece '{tracks[index].Name}' deforms by {amount}. {RecipeHint}");
                }
            }
        }

        private float Deformation(Mesh mesh, Vector3[] rest)
        {
            mesh.GetVertices(_positions);
            var deformation = 0f;
            for (var vertex = 0; vertex < rest.Length; vertex++)
            {
                deformation = Mathf.Max(deformation, Vector3.Distance(_positions[vertex], rest[vertex]));
            }

            return deformation;
        }

        private void ReadFrame(VatRigidTrack[] tracks, int frame)
        {
            for (var index = 0; index < _nodes.Length; index++)
            {
                tracks[index].Frames[frame] = _nodes[index].transform.localToWorldMatrix;
                tracks[index].Visible[frame] = _nodes[index].gameObject.activeInHierarchy;
            }
        }

        private void ReadInner(VatRigidTrack[] tracks, int sample)
        {
            for (var index = 0; index < _nodes.Length; index++)
            {
                tracks[index].Inner[sample] = _nodes[index].transform.localToWorldMatrix;
                tracks[index].InnerVisible[sample] = _nodes[index].gameObject.activeInHierarchy;
            }
        }
    }
}
#endif
