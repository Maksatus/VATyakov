using System.Linq;
using VATyakov.Editor;
using UnityEngine;

namespace VATyakov.Tests
{
    // Bake pipeline without the asset writer: mesh and texture stay readable.
    sealed class VatInMemoryBake
    {
        public readonly VatLayout Layout; // with the drift decision of the encoder
        public readonly Mesh Mesh;
        public readonly VatBakeTextures Textures;
        public readonly VatPrecision Precision;
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
                var layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, fps, loop));
                var encoder = new VertexEncoder(layout, source.Mesh);
                EncodeAll(source, layout, encoder);
                Layout = encoder.Layout;
                Precision = encoder.Precision;
                Mesh = encoder.BuildMesh("InMemory");
                Textures = VatBakeTextures.Build(encoder, "InMemory");
                Warnings = source.Warnings.ToArray();
            }
        }

        public Texture2D Position => Textures.Position;

        public Texture2D Rotation => Textures.Rotation;

        public Texture2D Drift => Textures.Drift;

        public void Destroy()
        {
            Object.DestroyImmediate(Mesh);
            Textures.Destroy();
        }

        void EncodeAll(IVatFrameSource source, VatLayout layout, VertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            var clip = layout.Clips[0];
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
