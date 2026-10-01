using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    // Clip names identify clips in a VatAsset (default clip, playback by name), so they must be unique.
    static class VatClipListProblems
    {
        public static IEnumerable<string> Find(IReadOnlyList<AnimationClip> clips)
        {
            if (clips.Count == 0)
            {
                yield return "Clips is empty: add at least one animation clip.";
                yield break;
            }

            var names = new HashSet<string>();
            for (int i = 0; i < clips.Count; i++)
            {
                string problem = Problem(clips, i, names);
                if (problem != null)
                    yield return problem;
            }
        }

        static string Problem(IReadOnlyList<AnimationClip> clips, int index, HashSet<string> names)
        {
            var clip = clips[index];
            if (clip == null)
                return $"Clip {index + 1} is not set.";
            if (!(float.IsFinite(clip.length) && clip.length > 0f))
                return $"Clip '{clip.name}' has zero length.";
            if (IndexOf(clips, clip) < index)
                return $"Clip '{clip.name}' is listed twice.";
            return names.Add(clip.name) ? null : $"Two clips are named '{clip.name}': clip names in a VAT asset must be unique.";
        }

        static int IndexOf(IReadOnlyList<AnimationClip> clips, AnimationClip clip)
        {
            for (int i = 0; i < clips.Count; i++)
                if (clips[i] == clip)
                    return i;
            return -1;
        }
    }
}
