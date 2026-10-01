using UnityEngine;

namespace VATyakov.Editor
{
    // Vertex mode textures of one bake (§1.9).
    sealed class VatBakeTextures
    {
        public Texture2D Position { get; private set; }
        public Texture2D Rotation { get; private set; }
        public Texture2D Drift { get; private set; }

        // Destroys what was built when a later texture throws.
        public static VatBakeTextures Build(VertexEncoder encoder, string name)
        {
            var textures = new VatBakeTextures();
            try
            {
                textures.Position = encoder.BuildPositionTexture(name + "_Pos");
                textures.Rotation = encoder.BuildRotationTexture(name + "_Rot");
                textures.Drift = encoder.BuildDriftTexture(name + "_Drift");
                return textures;
            }
            catch
            {
                textures.Destroy();
                throw;
            }
        }

        public void Destroy()
        {
            DestroyImmediate(Position);
            DestroyImmediate(Rotation);
            DestroyImmediate(Drift);
        }

        static void DestroyImmediate(Object target)
        {
            if (target != null)
                Object.DestroyImmediate(target);
        }
    }
}
