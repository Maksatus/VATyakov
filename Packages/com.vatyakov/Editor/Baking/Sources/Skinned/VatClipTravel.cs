using System;
using System.Collections.Generic;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatClipTravel
    {
        private const float MaxTravel = 0.25f;
        private const string RootMotionHint =
            "root motion is baked into the pose (legacy clip, Generic with Root node = None or Bake Into Pose). Use an in-place clip if code moves the unit.";

        private readonly IReadOnlyList<VatSourceClip> _clips;
        private readonly Vector3[] _start;
        private readonly Vector3[] _end;

        public VatClipTravel(IReadOnlyList<VatSourceClip> clips)
        {
            _clips = clips;
            _start = new Vector3[clips.Count];
            _end = new Vector3[clips.Count];
        }

        public void Add(int clip, double time, VatBakeCopy copy)
        {
            var renderer = copy.Renderer;
            var body = renderer.rootBone != null ? renderer.rootBone : renderer.transform;
            var point = copy.Root.transform.InverseTransformPoint(body.position);
            if (time == 0.0)
            {
                _start[clip] = point;
            }

            _end[clip] = point;
        }

        public List<string> Warnings()
        {
            var warnings = new List<string>();
            for (var clip = 0; clip < _clips.Count; clip++)
            {
                var travel = new Vector2(_end[clip].x - _start[clip].x, _end[clip].z - _start[clip].z).magnitude;
                if (travel > MaxTravel)
                {
                    var distance = FormattableString.Invariant($"{travel:0.##}");
                    warnings.Add($"Clip '{_clips[clip].Name}' moves the body {distance} m away from its start: {RootMotionHint}");
                }
            }

            return warnings;
        }
    }
}
