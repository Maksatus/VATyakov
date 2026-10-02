using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatClipLookup
    {
        public static int Find(VatAsset asset, Vector4 frame)
        {
            if (!float.IsFinite(frame.x))
            {
                return -1;
            }

            var row = (int)frame.x;
            for (var i = 0; i < asset.Clips.Count; i++)
            {
                if (row >= asset.Clips[i].StartRow && row < asset.Clips[i].StartRow + asset.Clips[i].FrameCount)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
