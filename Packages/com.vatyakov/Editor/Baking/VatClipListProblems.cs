using System;
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
                return FormattableString.Invariant($"Clip {index + 1} is not set.");
            }

            if (!(float.IsFinite(clip.length) && clip.length > 0f))
            {
                return $"Clip '{clip.name}' has zero length.";
            }

            if (IsListedBefore(clips, index))
            {
                return $"Clip '{clip.name}' is listed twice.";
            }

            return names.Add(clip.name) ? null : $"Two clips are named '{clip.name}': clip names in a VAT asset must be unique.";
        }

        private static bool IsListedBefore(IReadOnlyList<AnimationClip> clips, int index)
        {
            for (var i = 0; i < index; i++)
            {
                if (clips[i] == clips[index])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
