using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBakeTextures
    {
        public Texture2D Position { get; private set; }
        public Texture2D Rotation { get; private set; }
        public Texture2D Drift { get; private set; }

        public static VatBakeTextures Build(VertexEncoder encoder, string name)
        {
            var textures = new VatBakeTextures();
            try
            {
                textures.Position = encoder.BuildPositionTexture(name + "_pos");
                textures.Rotation = encoder.BuildRotationTexture(name + "_rot");
                textures.Drift = encoder.BuildDriftTexture(name + "_drift");
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

        private static void DestroyImmediate(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target);
            }
        }
    }
}
