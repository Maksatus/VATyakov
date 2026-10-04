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

        private VatBakeCopy _copy;
        private VatClipPlayer _player;
        private VatFrameReader _reader;
        private VatPoseSnapshot _pose;
        private int? _clip;

        public VatSourceMesh Mesh { get; }
        public IReadOnlyList<VatSourceClip> Clips => _clipInfos;
        public IReadOnlyList<string> Warnings => Array.Empty<string>();
        public VatBakeCopy Copy => _copy;

        public VatSkinnedFrameSource(SkinnedMeshRenderer source, IReadOnlyList<AnimationClip> clips) : this(source, clips, Array.Empty<Renderer>())
        {
        }

        public VatSkinnedFrameSource(SkinnedMeshRenderer source, IReadOnlyList<AnimationClip> clips, IReadOnlyList<Renderer> extras)
        {
            _clips = clips.ToArray();
            _clipInfos = Array.ConvertAll(_clips, clip => new VatSourceClip(clip.name, clip.length));
            Mesh = VatSourceMesh.Read(source.sharedMesh);
            try
            {
                Create(source, extras);
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

        private void Create(SkinnedMeshRenderer source, IReadOnlyList<Renderer> extras)
        {
            _copy = new VatBakeCopy(source, extras);
            _player = new VatClipPlayer(_copy.Root, NeedsAnimator() ? _copy.PrepareAnimator() : null);
            _reader = new VatFrameReader(Mesh.VertexCount);
            _pose = new VatPoseSnapshot(_copy.Root);
        }

        private bool NeedsAnimator()
        {
            return Array.Exists(_clips, clip => !clip.legacy);
        }
    }
}
