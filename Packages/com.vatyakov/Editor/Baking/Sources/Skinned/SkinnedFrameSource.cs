using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VATyakov.Editor
{
    // §2.1: a SkinnedMeshRenderer sampled through a bake copy.
    sealed class SkinnedFrameSource : IVatFrameSource
    {
        readonly AnimationClip[] _clips;
        readonly VatSourceClip[] _clipInfos;
        VatBakeCopy _copy;
        VatClipPlayer _player;
        VatFrameReader _reader;

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

        public VatSourceMesh Mesh { get; }

        public IReadOnlyList<VatSourceClip> Clips => _clipInfos;

        public IReadOnlyList<string> Warnings => Array.Empty<string>();

        public void Sample(int clip, double time, VatFrame frame)
        {
            _player.Sample(_clips[clip], time);
            _reader.Read(_copy, frame);
        }

        public void Dispose()
        {
            _player?.Dispose();
            _reader?.Dispose();
            _copy?.Dispose();
        }

        void Create(SkinnedMeshRenderer source)
        {
            _copy = new VatBakeCopy(source);
            _player = new VatClipPlayer(_copy.Root, NeedsAnimator() ? _copy.PrepareAnimator() : null);
            _reader = new VatFrameReader(Mesh.VertexCount);
        }

        bool NeedsAnimator() => Array.Exists(_clips, c => !c.legacy);

        static void RequireMesh(SkinnedMeshRenderer source)
        {
            if (source == null || source.sharedMesh == null)
                throw new ArgumentException("Source SkinnedMeshRenderer with a mesh is required.", nameof(source));
        }
    }
}
