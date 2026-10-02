using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal readonly struct VatClipRequest
    {
        public readonly string Name;
        public readonly float Length;
        public readonly float Fps;
        public readonly bool IsLooping;

        public VatClipRequest(string name, float length, float fps, bool isLooping = true)
        {
            Name = name;
            Length = length;
            Fps = fps;
            IsLooping = isLooping;
        }

        public static VatClipRequest[] From(IReadOnlyList<VatSourceClip> clips, float fps, bool isLooping)
        {
            var requests = new VatClipRequest[clips.Count];
            for (var i = 0; i < requests.Length; i++)
            {
                requests[i] = new VatClipRequest(clips[i].Name, clips[i].Length, fps, isLooping);
            }

            return requests;
        }
    }
}
