using System.Collections.Generic;

namespace VATyakov.Editor
{
    readonly struct VatClipRequest
    {
        public readonly string Name;
        public readonly float Length;
        public readonly float Fps;

        public VatClipRequest(string name, float length, float fps)
        {
            Name = name;
            Length = length;
            Fps = fps;
        }

        public static VatClipRequest[] From(IReadOnlyList<VatSourceClip> clips, float fps)
        {
            var requests = new VatClipRequest[clips.Count];
            for (int i = 0; i < requests.Length; i++)
                requests[i] = new VatClipRequest(clips[i].Name, clips[i].Length, fps);
            return requests;
        }
    }
}
