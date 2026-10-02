using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal readonly struct VatClipRequest
    {
        public readonly string Name;
        public readonly float Length;
        public readonly float Fps;
        public readonly bool Loop;

        public VatClipRequest(string name, float length, float fps, bool loop = true)
        {
            Name = name;
            Length = length;
            Fps = fps;
            Loop = loop;
        }

        public static VatClipRequest[] From(IReadOnlyList<VatSourceClip> clips, float fps, bool loop)
        {
            var requests = new VatClipRequest[clips.Count];
            for (var i = 0; i < requests.Length; i++)
            {
                requests[i] = new VatClipRequest(clips[i].Name, clips[i].Length, fps, loop);
            }

            return requests;
        }
    }
}
