using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBakeTextures
    {
        public readonly Texture2D Position;
        public readonly Texture2D Rotation;
        public readonly Texture2D Drift;

        private VatBakeTextures(Texture2D position, Texture2D rotation, Texture2D drift)
        {
            Position = position;
            Rotation = rotation;
            Drift = drift;
        }

        public static VatBakeTextures Build(VatVertexEncoder encoder, string name)
        {
            Texture2D position = null;
            Texture2D rotation = null;
            try
            {
                position = encoder.BuildPositionTexture($"{name}{VatAssetPath.PositionSuffix}");
                rotation = encoder.BuildRotationTexture($"{name}{VatAssetPath.RotationSuffix}");
                var drift = encoder.BuildDriftTexture($"{name}{VatAssetPath.DriftSuffix}");
                return new VatBakeTextures(position, rotation, drift);
            }
            catch
            {
                DestroyImmediate(position);
                DestroyImmediate(rotation);
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
