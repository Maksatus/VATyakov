using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatClipListProblems
    {
        public static IEnumerable<string> Find(IReadOnlyList<AnimationClip> clips)
        {
            if (clips.Count == 0)
            {
                yield return "Clips is empty: add at least one animation clip.";
                yield break;
            }

            var names = new HashSet<string>();
            for (var i = 0; i < clips.Count; i++)
            {
                var problem = Problem(clips, i, names);
                if (problem != null)
                {
                    yield return problem;
                }
            }
        }

        private static string Problem(IReadOnlyList<AnimationClip> clips, int index, HashSet<string> names)
        {
            var clip = clips[index];
            if (clip == null)
            {
                return $"Clip {index + 1} is not set.";
            }

            if (!(float.IsFinite(clip.length) && clip.length > 0f))
            {
                return $"Clip '{clip.name}' has zero length.";
            }

            if (IndexOf(clips, clip) < index)
            {
                return $"Clip '{clip.name}' is listed twice.";
            }

            return names.Add(clip.name) ? null : $"Two clips are named '{clip.name}': clip names in a VAT asset must be unique.";
        }

        private static int IndexOf(IReadOnlyList<AnimationClip> clips, AnimationClip clip)
        {
            for (var i = 0; i < clips.Count; i++)
            {
                if (clips[i] == clip)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
