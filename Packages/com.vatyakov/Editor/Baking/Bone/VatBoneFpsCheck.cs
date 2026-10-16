using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatBoneFpsCheck
    {
        public const float Tolerance = 1e-2f;

        private const float MillimetersPerMeter = 1000f;

        private static readonly float[] _fractions = { 0.25f, 0.5f, 0.75f };

        private readonly Matrix4x4[][] _skins;

        private VatClip _worstClip;
        private double _worstTime;

        public float MaxError { get; private set; }

        public VatBoneFpsCheck(int boneCount)
        {
            _skins = Array.ConvertAll(_fractions, _ => new Matrix4x4[boneCount]);
        }

        public void SampleBefore(VatSkinnedFrameSource source, VatBoneRig rig, int clipIndex, VatClip clip, int frame)
        {
            for (var i = 0; i < _fractions.Length; i++)
            {
                source.Pose(clipIndex, Time(clip, frame, _fractions[i]));
                rig.ReadSkin(source.Copy.Root.transform, _skins[i]);
            }
        }

        public void Measure(VatBoneEncoder encoder, VatClip clip, int frame)
        {
            for (var i = 0; i < _fractions.Length; i++)
            {
                var error = encoder.InnerError(_skins[i], _fractions[i]);
                if (error > MaxError)
                {
                    MaxError = error;
                    _worstClip = clip;
                    _worstTime = Time(clip, frame, _fractions[i]);
                }
            }
        }

        public string Warning()
        {
            if (!(MaxError > Tolerance))
            {
                return null;
            }

            var error = FormattableString.Invariant($"{MaxError * MillimetersPerMeter:0.#} mm at {_worstTime:0.###} s");
            var threshold = FormattableString.Invariant($"{Tolerance * MillimetersPerMeter:0.#} mm");
            var clip = FormattableString.Invariant($"Clip '{_worstClip.Name}' moves too fast for {_worstClip.FrameRate:0.###} fps");
            return $"{clip}: between baked frames the mesh differs by up to {error}, over {threshold}. Raise Frames Per Second.";
        }

        private static double Time(VatClip clip, int frame, float fraction)
        {
            var start = clip.FrameTime(frame - 1);
            return start + (clip.FrameTime(frame) - start) * fraction;
        }
    }
}
