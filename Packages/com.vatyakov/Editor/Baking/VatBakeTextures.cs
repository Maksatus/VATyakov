using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBakeTextures
    {
        public readonly Texture2D Position;
        public readonly Texture2D Rotation;

        private VatBakeTextures(Texture2D position, Texture2D rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static VatBakeTextures Build(VatVertexEncoder encoder, string name)
        {
            Texture2D position = null;
            try
            {
                position = encoder.BuildPositionTexture($"{name}{VatAssetPath.PositionSuffix}");
                var rotation = encoder.BuildRotationTexture($"{name}{VatAssetPath.RotationSuffix}");
                return new VatBakeTextures(position, rotation);
            }
            catch
            {
                DestroyImmediate(position);
                throw;
            }
        }

        public void Destroy()
        {
            DestroyImmediate(Position);
            DestroyImmediate(Rotation);
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
