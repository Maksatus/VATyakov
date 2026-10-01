using System.Linq;
using VATyakov.Editor;
using UnityEngine;

namespace VATyakov.Tests
{
    // Bake pipeline without the asset writer: mesh and texture stay readable.
    sealed class VatInMemoryBake
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly Texture2D Position;
        public readonly Texture2D Rotation;
        public Vector3[] Rest { get; private set; }
        public Vector3[] RestNormals { get; private set; }
        public readonly string[] Warnings;

        public VatInMemoryBake(VatTestRig rig, float fps) : this(new SkinnedFrameSource(rig.Renderer, new[] { rig.Clip }), fps)
        {
        }

        // Takes ownership of the source.
        public VatInMemoryBake(IVatFrameSource source, float fps, bool loop = true)
        {
            using (source)
            {
                Layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, fps, loop));
                var encoder = new VertexEncoder(Layout, source.Mesh);
                EncodeAll(source, encoder);
                Mesh = encoder.BuildMesh("InMemory");
                Position = encoder.BuildPositionTexture("InMemory");
                Rotation = encoder.BuildRotationTexture("InMemory");
                Warnings = source.Warnings.ToArray();
            }
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Mesh);
            Object.DestroyImmediate(Position);
            Object.DestroyImmediate(Rotation);
        }

        void EncodeAll(IVatFrameSource source, VertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            var clip = Layout.Clips[0];
            for (int k = 0; k < clip.FrameCount; k++)
            {
                source.Sample(0, clip.FrameTime(k), frame);
                Rest ??= (Vector3[])frame.Positions.Clone();
                RestNormals ??= (Vector3[])frame.Normals.Clone();
                encoder.AddFrame(0, k, frame);
            }
        }
    }
}
