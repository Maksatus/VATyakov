using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    // What the inspector needs from an .abc without baking it. Opening one costs an instantiate and two samples,
    // so the last result is kept until the .abc is reimported; scene objects are probed every time.
    sealed class VatAlembicProbe
    {
        static VatAlembicProbe _last;

        readonly GameObject _alembic;
        readonly Hash128 _hash;

        public readonly string Problem;
        public readonly int VertexCount;
        public readonly VatSourceClip Clip;
        public readonly float LoopGap;

        VatAlembicProbe(GameObject alembic, Hash128 hash)
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
                _last = new VatAlembicProbe(alembic, hash);
            return _last;
        }

        static Hash128 Hash(GameObject alembic)
        {
            string path = AssetDatabase.GetAssetPath(alembic);
            return string.IsNullOrEmpty(path) ? default : AssetDatabase.GetAssetDependencyHash(path);
        }
    }
}
