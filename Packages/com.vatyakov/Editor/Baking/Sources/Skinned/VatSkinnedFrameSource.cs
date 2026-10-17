using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatSkinnedFrameSource : IVatFrameSource
    {
        private readonly AnimationClip[] _clips;
        private readonly VatSourceClip[] _clipInfos;
        private readonly VatClipTravel _travel;
        private readonly VatClipMotion _motion;
        private readonly List<string> _sourceWarnings = new();

        private VatBakeCopy _copy;
        private VatClipPlayer _player;
        private VatFrameReader _reader;
        private VatPoseSnapshot _pose;
        private int? _clip;

        public VatSourceMesh Mesh { get; }
        public IReadOnlyList<VatSourceClip> Clips => _clipInfos;
        public IReadOnlyList<string> Warnings => _sourceWarnings.Concat(_travel.Warnings()).Concat(_motion.Warnings()).ToList();
        public VatBakeCopy Copy => _copy;

        public VatSkinnedFrameSource(SkinnedMeshRenderer source, IReadOnlyList<AnimationClip> clips)
        {
            _clips = clips.ToArray();
            _clipInfos = Array.ConvertAll(_clips, VatSourceClip.Of);
            _travel = new VatClipTravel(_clipInfos);
            _motion = new VatClipMotion(_clipInfos);
            try
            {
                Create(source);
                Mesh = VatSourceWinding.Oriented(VatSourceMesh.Read(source.sharedMesh), _copy.RendererToRoot);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Sample(int clip, double time, VatFrame frame)
        {
            Pose(clip, time);
            _reader.Read(_copy, frame);
        }

        public void Pose(int clip, double time)
        {
            if (clip != _clip)
            {
                StartClip(clip);
            }

            _player.Sample(_clips[clip], time);
            _travel.Add(clip, time, _copy);
            _motion.Add(clip, time, _copy);
        }

        public void Rewind()
        {
            _clip = null;
        }

        public void Dispose()
        {
            _player?.Dispose();
            _reader?.Dispose();
            _copy?.Dispose();
        }

        private void StartClip(int clip)
        {
            _player.Stop();
            _pose.Restore();
            _clip = clip;
        }

        private void Create(SkinnedMeshRenderer source)
        {
            _copy = new VatBakeCopy(source);
            _player = new VatClipPlayer(_copy.LegacyRoot(), NeedsAnimator() ? _copy.PrepareAnimator() : null);
            _reader = new VatFrameReader(source.sharedMesh.vertexCount);
            _pose = new VatPoseSnapshot(_copy.Root);
            AddWarning(VatLostAttributes.Warning(source.name, new[] { source.sharedMesh }));
            AddWarning(VatConstraints.Warning(_copy.Root));
            AddWarning(VatClipEvents.Warning(_clips));
        }

        private void AddWarning(string warning)
        {
            if (warning != null)
            {
                _sourceWarnings.Add(warning);
            }
        }

        private bool NeedsAnimator()
        {
            return Array.Exists(_clips, clip => !clip.legacy);
        }
    }
}
