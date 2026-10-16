using System;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Editor
{
    internal static class VatClipEvents
    {
        private const string EventsHint = "the bake drops them, drive such code from ClipFinished or GetNormalizedTime of VatAnimator.";

        public static string Warning(AnimationClip[] clips)
        {
            var withEvents = Array.FindAll(clips, clip => AnimationUtility.GetAnimationEvents(clip).Length > 0);
            if (withEvents.Length == 0)
            {
                return null;
            }

            var names = string.Join(", ", Array.ConvertAll(withEvents, clip => $"'{clip.name}'"));
            return $"Clips {names} have Animation Events: {EventsHint}";
        }
    }
}
