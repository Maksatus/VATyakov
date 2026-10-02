using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VATyakov.Editor
{
    internal sealed class VatClipPlayer : IDisposable
    {
        private readonly GameObject _root;
        private readonly Animator _animator;
        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;
        private AnimationClip _graphClip;

        public VatClipPlayer(GameObject root, Animator animator)
        {
            _root = root;
            _animator = animator;
        }

        public void Sample(AnimationClip clip, double time)
        {
            time = BeforeWrap(clip, time);
            if (clip.legacy)
            {
                clip.SampleAnimation(_root, (float)time);
            }
            else
            {
                Evaluate(clip, time);
            }
        }

        public void Stop()
        {
            DestroyGraph();
        }

        public void Dispose()
        {
            DestroyGraph();
        }

        private static double BeforeWrap(AnimationClip clip, double time)
        {
            var guard = Math.Max(1e-5, clip.length * 1e-6);
            return Wraps(clip) && time > clip.length - guard ? clip.length - guard : time;
        }

        private static bool Wraps(AnimationClip clip)
        {
            return clip.legacy ? clip.wrapMode == WrapMode.Loop : clip.isLooping;
        }

        private void Evaluate(AnimationClip clip, double time)
        {
            UseGraph(clip);
            _playable.SetTime(time);
            _graph.Evaluate();
        }

        private void UseGraph(AnimationClip clip)
        {
            if (_graphClip == clip)
            {
                return;
            }

            DestroyGraph();
            CreateGraph(clip);
        }

        private void CreateGraph(AnimationClip clip)
        {
            _graph = PlayableGraph.Create("VatBake");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetApplyFootIK(false);
            _playable.SetApplyPlayableIK(false);
            output.SetSourcePlayable(_playable);
            _graphClip = clip;
        }

        private void DestroyGraph()
        {
            if (_graph.IsValid())
            {
                _graph.Destroy();
            }

            _graphClip = null;
        }
    }
}
