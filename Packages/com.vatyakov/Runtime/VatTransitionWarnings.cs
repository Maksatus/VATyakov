using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace VATyakov
{
    internal static class VatTransitionWarnings
    {
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void CheckCrossFade(Object context, VatMixer mixer, VatClip clip, double time)
        {
            if (!mixer.IsInTransition(time))
            {
                return;
            }

            var weight = mixer.Weight(time);
            Debug.LogWarning(FormattableString.Invariant(
                $"{context.name}: CrossFade to '{clip.Name}' during an unfinished transition (weight {weight:0.###}): clip '{mixer.DroppedClip(time).Name}' is dropped with a pop."),
                context);
        }

        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        public static void ReportNoTransition(Object context)
        {
            Debug.LogWarning($"{context.name}: SetWeight needs a transition target: call CrossFade first.", context);
        }
    }
}
