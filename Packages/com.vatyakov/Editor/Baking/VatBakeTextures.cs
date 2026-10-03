using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBakeTextures
    {
        public readonly Texture2D Position;
        public readonly Texture2D Rotation;
        public readonly Texture2D Bone;

        private VatBakeTextures(Texture2D position, Texture2D rotation, Texture2D bone)
        {
            Position = position;
            Rotation = rotation;
            Bone = bone;
        }

        public static VatBakeTextures Build(VatVertexEncoder encoder, string name)
        {
            Texture2D position = null;
            try
            {
                position = encoder.BuildPositionTexture($"{name}{VatAssetPath.PositionSuffix}");
                var rotation = encoder.BuildRotationTexture($"{name}{VatAssetPath.RotationSuffix}");
                return new VatBakeTextures(position, rotation, null);
            }
            catch
            {
                DestroyImmediate(position);
                throw;
            }
        }

        public static VatBakeTextures Build(VatBoneEncoder encoder, string name)
        {
            return new VatBakeTextures(null, null, encoder.BuildTexture($"{name}{VatAssetPath.BoneSuffix}"));
        }

        public void Destroy()
        {
            DestroyImmediate(Position);
            DestroyImmediate(Rotation);
            DestroyImmediate(Bone);
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
