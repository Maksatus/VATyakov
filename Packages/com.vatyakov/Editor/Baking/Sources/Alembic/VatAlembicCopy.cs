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

        public VatAlembicCopy(GameObject alembic)
        {
            Root = Object.Instantiate(alembic);
            Root.hideFlags = HideFlags.HideAndDontSave;
            Root.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Root.transform.localScale = Vector3.one;
            Player = Root.GetComponent<AlembicStreamPlayer>();
        }

        public MeshFilter[] Meshes()
        {
            return Array.FindAll(Root.GetComponentsInChildren<MeshFilter>(true), node => node.sharedMesh != null);
        }

        public void Dispose()
        {
            Object.DestroyImmediate(Root);
        }
    }
}
#endif
