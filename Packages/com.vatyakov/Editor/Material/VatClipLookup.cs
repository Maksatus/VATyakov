using UnityEngine;

namespace VATyakov.Editor
{
    static class VatClipLookup
    {
        // The clip whose rows contain row0 of _VatFrame.
        public static int Find(VatAsset asset, Vector4 frame)
        {
            if (!float.IsFinite(frame.x))
                return -1;
            int row = (int)frame.x;
            for (int i = 0; i < asset.Clips.Count; i++)
                if (row >= asset.Clips[i].StartRow && row < asset.Clips[i].StartRow + asset.Clips[i].FrameCount)
                    return i;
            return -1;
        }
    }
}
