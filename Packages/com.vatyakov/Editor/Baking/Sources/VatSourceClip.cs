using UnityEngine;

namespace VATyakov.Editor
{
    internal readonly struct VatSourceClip
    {
        public readonly string Name;
        public readonly float Length;
        public readonly bool IsLooping;

        public VatSourceClip(string name, float length, bool isLooping)
        {
            Name = name;
            Length = length;
            IsLooping = isLooping;
        }

        public static VatSourceClip Of(AnimationClip clip)
        {
            return new VatSourceClip(clip.name, clip.length, Loops(clip));
        }

        public static bool Loops(AnimationClip clip)
        {
            return clip.legacy ? clip.wrapMode == WrapMode.Loop : clip.isLooping;
        }
    }
}
