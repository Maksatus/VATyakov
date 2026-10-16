#if VAT_ALEMBIC
using System;
using UnityEngine;
using UnityEngine.Formats.Alembic.Importer;
using Object = UnityEngine.Object;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicCopy : IDisposable
    {
        public readonly GameObject Root;
        public readonly AlembicStreamPlayer Player;
        public readonly MeshFilter[] Nodes;

        private readonly string _name;
        private readonly VatAlembicTopology _topology;

        public VatAlembicCopy(GameObject alembic)
        {
            _name = alembic.name;
            Root = Object.Instantiate(alembic);
            Root.hideFlags = HideFlags.HideAndDontSave;
            Root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Root.transform.localScale = Vector3.one;
            Player = Root.GetComponent<AlembicStreamPlayer>();
            try
            {
                Player.UpdateImmediately(0f);
                Nodes = RequireMeshes();
                _topology = VatAlembicTopology.Capture(Nodes);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Sample(double time)
        {
            Player.UpdateImmediately((float)time);
            var difference = _topology.Difference(VatAlembicTopology.Capture(Meshes()));
            if (difference != null)
            {
                throw new VatBakeException(FormattableString.Invariant($"'{_name}' at {time:0.###} s: {difference}"));
            }
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Root);
        }

        private MeshFilter[] RequireMeshes()
        {
            var nodes = Meshes();
            if (nodes.Length == 0)
            {
                throw new VatBakeException($"'{_name}' has no meshes, nothing to bake. Check that Meshes are enabled in the .abc importer.");
            }

            return nodes;
        }

        private MeshFilter[] Meshes()
        {
            return Array.FindAll(Root.GetComponentsInChildren<MeshFilter>(true), node => node.sharedMesh != null);
        }
    }
}
#endif
