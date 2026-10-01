using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VATyakov.Editor
{
    // §2.1: Generic and Humanoid through a manual PlayableGraph, legacy through SampleAnimation.
    // 1.6 adds a pose and blend shape reset before each clip.
    sealed class VatClipPlayer : IDisposable
    {
        readonly GameObject _root;
        readonly Animator _animator;
        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        AnimationClip _graphClip;

        public VatClipPlayer(GameObject root, Animator animator)
        {
            _root = root;
            _animator = animator;
        }

        public void Sample(AnimationClip clip, double time)
        {
            if (clip.legacy)
                clip.SampleAnimation(_root, (float)time);
            else
                Evaluate(clip, time);
        }

        public void Dispose() => DestroyGraph();

        void Evaluate(AnimationClip clip, double time)
        {
            UseGraph(clip);
            _playable.SetTime(time);
            _graph.Evaluate();
        }

        void UseGraph(AnimationClip clip)
        {
            if (_graphClip == clip)
                return;
            DestroyGraph();
            CreateGraph(clip);
        }

        void CreateGraph(AnimationClip clip)
        {
            _graph = PlayableGraph.Create("VatBake");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            var output = AnimationPlayableOutput.Create(_graph, "out", _animator);
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetApplyFootIK(false); // on by default for a new playable
            _playable.SetApplyPlayableIK(false);
            output.SetSourcePlayable(_playable);
            _graphClip = clip;
        }

        void DestroyGraph()
        {
            if (_graph.IsValid())
                _graph.Destroy();
            _graphClip = null;
        }
    }
}
