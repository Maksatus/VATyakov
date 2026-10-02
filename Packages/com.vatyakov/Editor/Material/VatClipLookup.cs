using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatClipLookup
    {
        public static bool TryFind(VatAsset asset, Vector4 frame, out int clipIndex)
        {
            clipIndex = default;
            if (!float.IsFinite(frame.x))
            {
                return false;
            }

            var row = (int)frame.x;
            for (var i = 0; i < asset.Clips.Count; i++)
            {
                if (row >= asset.Clips[i].StartRow && row < asset.Clips[i].StartRow + asset.Clips[i].FrameCount)
                {
                    clipIndex = i;
                    return true;
                }
            }

            return false;
        }
    }
}
