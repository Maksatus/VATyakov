using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatAlembicProbe
    {
        private static VatAlembicProbe _last;

        private readonly GameObject _alembic;
        private readonly Hash128 _hash;

        public readonly string Problem;
        public readonly int VertexCount;
        public readonly VatSourceClip Clip;
        public readonly float LoopGap;

        private VatAlembicProbe(GameObject alembic, Hash128 hash)
        {
            _alembic = alembic;
            _hash = hash;
            try
            {
                using var source = VatAlembic.Open(alembic);
                VertexCount = source.Mesh.VertexCount;
                Clip = source.Clips[0];
                LoopGap = VatLoopGap.Measure(source, 0);
            }
            catch (VatBakeException e)
            {
                Problem = e.Message;
            }
        }

        public static VatAlembicProbe For(GameObject alembic)
        {
            var hash = Hash(alembic);
            if (_last == null || _last._alembic != alembic || _last._hash != hash || !hash.isValid)
            {
                _last = new VatAlembicProbe(alembic, hash);
            }

            return _last;
        }

        private static Hash128 Hash(GameObject alembic)
        {
            var path = AssetDatabase.GetAssetPath(alembic);
            return string.IsNullOrEmpty(path) ? default : AssetDatabase.GetAssetDependencyHash(path);
        }
    }
}
