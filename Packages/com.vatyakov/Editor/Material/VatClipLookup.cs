using UnityEngine;

namespace VATyakov.Editor
{
    static class VatClipLookup
    {
        public static int Find(VatAsset asset, Vector4 state)
        {
            if (!float.IsFinite(state.x) || Mathf.Abs(state.x) < 1f)
                return -1;
            VatMath.UnpackClip(state.x, out int startRow, out int frameCount, out bool loop);
            for (int i = 0; i < asset.Clips.Count; i++)
                if (Matches(asset.Clips[i], startRow, frameCount, loop))
                    return i;
            return -1;
        }

        static bool Matches(VatClip clip, int startRow, int frameCount, bool loop) =>
            clip.StartRow == startRow && clip.FrameCount == frameCount && clip.Loop == loop;
    }
}
