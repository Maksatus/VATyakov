using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class SkinnedFrameSource : IVatFrameSource
    {
        private readonly AnimationClip[] _clips;
        private readonly VatSourceClip[] _clipInfos;
        private VatBakeCopy _copy;
        private VatClipPlayer _player;
        private VatFrameReader _reader;
        private VatPoseSnapshot _pose;
        private int _clip = -1;

        public VatSourceMesh Mesh { get; }
        public IReadOnlyList<VatSourceClip> Clips => _clipInfos;
        public IReadOnlyList<string> Warnings => Array.Empty<string>();

        public SkinnedFrameSource(SkinnedMeshRenderer source, IReadOnlyList<AnimationClip> clips)
        {
            RequireMesh(source);
            _clips = clips.ToArray();
            _clipInfos = Array.ConvertAll(_clips, c => new VatSourceClip(c.name, c.length));
            Mesh = VatSourceMesh.Read(source.sharedMesh);
            try
            {
                Create(source);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Sample(int clip, double time, VatFrame frame)
        {
            if (clip != _clip)
            {
                StartClip(clip);
            }

            _player.Sample(_clips[clip], time);
            _reader.Read(_copy, frame);
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
            _player = new VatClipPlayer(_copy.Root, NeedsAnimator() ? _copy.PrepareAnimator() : null);
            _reader = new VatFrameReader(Mesh.VertexCount);
            _pose = new VatPoseSnapshot(_copy.Root);
        }

        private bool NeedsAnimator()
        {
            return Array.Exists(_clips, c => !c.legacy);
        }

        private static void RequireMesh(SkinnedMeshRenderer source)
        {
            if (source == null || source.sharedMesh == null)
            {
                throw new ArgumentException("Source SkinnedMeshRenderer with a mesh is required.", nameof(source));
            }
        }
    }
}
