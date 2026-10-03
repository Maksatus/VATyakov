using System.Linq;
using UnityEngine;
using VATyakov.Editor;

namespace VATyakov.Tests
{
    internal sealed class VatInMemoryBake
    {
        public readonly VatLayout Layout;
        public readonly Mesh Mesh;
        public readonly VatBakeTextures Textures;
        public readonly Vector3[] Drift;
        public readonly VatPrecision Precision;
        public readonly string[] Warnings;

        public Vector3[] Rest { get; private set; }
        public Vector3[] RestNormals { get; private set; }
        public Texture2D Position => Textures.Position;
        public Texture2D Rotation => Textures.Rotation;

        public VatInMemoryBake(VatTestRig rig, float fps) : this(rig, fps, rig.Clip)
        {
        }

        public VatInMemoryBake(VatTestRig rig, float fps, params AnimationClip[] clips) : this(new VatSkinnedFrameSource(rig.Renderer, clips), fps)
        {
        }

        public VatInMemoryBake(IVatFrameSource source, float fps, bool isLooping = true)
        {
            using (source)
            {
                var layout = VatLayout.ForVertex(source.Mesh.VertexCount, VatClipRequest.From(source.Clips, fps, isLooping));
                var encoder = new VatVertexEncoder(layout, source.Mesh);
                EncodeAll(source, layout, encoder);
                Layout = encoder.Layout;
                Drift = encoder.Drift;
                Precision = encoder.Precision;
                Mesh = encoder.BuildMesh("InMemory");
                Textures = VatBakeTextures.Build(encoder, "InMemory");
                Warnings = source.Warnings.ToArray();
            }
        }

        public void Destroy()
        {
            Object.DestroyImmediate(Mesh);
            Textures.Destroy();
        }

        private void EncodeAll(IVatFrameSource source, VatLayout layout, VatVertexEncoder encoder)
        {
            var frame = new VatFrame(source.Mesh.VertexCount);
            for (var c = 0; c < layout.Clips.Length; c++)
            {
                for (var k = 0; k < layout.Clips[c].FrameCount; k++)
                {
                    source.Sample(c, layout.Clips[c].FrameTime(k), frame);
                    Rest ??= (Vector3[])frame.Positions.Clone();
                    RestNormals ??= (Vector3[])frame.Normals.Clone();
                    encoder.AddFrame(c, k, frame);
                }
            }
        }
    }
}
