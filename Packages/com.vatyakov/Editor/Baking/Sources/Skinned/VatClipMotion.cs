using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class VatClipMotion
    {
        private const string StillHint =
            "a Humanoid clip needs a model with a Humanoid avatar, a Generic or legacy clip needs the bone paths of this model.";

        private readonly IReadOnlyList<VatSourceClip> _clips;
        private readonly VatPoseSnapshot[] _starts;
        private readonly bool[] _isSampledLater;
        private readonly bool[] _isMoving;

        public VatClipMotion(IReadOnlyList<VatSourceClip> clips)
        {
            _clips = clips;
            _starts = new VatPoseSnapshot[clips.Count];
            _isSampledLater = new bool[clips.Count];
            _isMoving = new bool[clips.Count];
        }

        public void Add(int clip, double time, VatBakeCopy copy)
        {
            if (time == 0.0)
            {
                _starts[clip] = new VatPoseSnapshot(copy.Root);
                return;
            }

            _isSampledLater[clip] = true;
            if (!_isMoving[clip])
            {
                _isMoving[clip] = !_starts[clip].Matches();
            }
        }

        public List<string> Warnings()
        {
            var warnings = new List<string>();
            for (var clip = 0; clip < _clips.Count; clip++)
            {
                if (_isSampledLater[clip] && !_isMoving[clip])
                {
                    warnings.Add($"Clip '{_clips[clip].Name}' doesn't move the mesh: {StillHint}");
                }
            }

            return warnings;
        }
    }
}
