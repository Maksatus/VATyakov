using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicProbe
    {
        private static readonly Dictionary<GameObject, VatAlembicProbe> _probes = new();

        private readonly Hash128 _hash;

        public readonly string Problem;
        public readonly int VertexCount;
        public readonly VatSourceClip Clip;

        private VatAlembicProbe(GameObject alembic, Hash128 hash)
        {
            _hash = hash;
            try
            {
                using var source = VatAlembic.Open(alembic);
                VertexCount = source.Mesh.VertexCount;
                Clip = source.Clips[0];
                source.Sample(0, Clip.Length, new VatFrame(VertexCount));
            }
            catch (VatBakeException e)
            {
                Problem = e.Message;
            }
        }

        public static VatAlembicProbe For(GameObject alembic)
        {
            var hash = Hash(alembic);
            if (!hash.isValid || !_probes.TryGetValue(alembic, out var probe) || probe._hash != hash)
            {
                probe = new VatAlembicProbe(alembic, hash);
                _probes[alembic] = probe;
            }

            return probe;
        }

        private static Hash128 Hash(GameObject alembic)
        {
            var path = AssetDatabase.GetAssetPath(alembic);
            return string.IsNullOrEmpty(path) ? default : AssetDatabase.GetAssetDependencyHash(path);
        }
    }
}
