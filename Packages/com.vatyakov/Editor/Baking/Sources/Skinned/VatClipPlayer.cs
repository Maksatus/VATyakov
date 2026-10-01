using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace VATyakov.Editor
{
    // §2.1: Generic and Humanoid through a manual PlayableGraph, legacy through SampleAnimation.
    // 1.6 adds a pose and blend shape reset before each clip.
    // A looping clip (loopTime, legacy WrapMode.Loop) wraps t = L to the pose at 0, so the last one-shot frame
    // is sampled just before the end. The guard is far above the float ulp of clip time and far below visible motion.
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
            time = BeforeWrap(clip, time);
            if (clip.legacy)
                clip.SampleAnimation(_root, (float)time);
            else
                Evaluate(clip, time);
        }

        public void Dispose() => DestroyGraph();

        static double BeforeWrap(AnimationClip clip, double time)
        {
            double guard = Math.Max(1e-5, clip.length * 1e-6);
            return Wraps(clip) && time > clip.length - guard ? clip.length - guard : time;
        }

        static bool Wraps(AnimationClip clip) => clip.legacy ? clip.wrapMode == WrapMode.Loop : clip.isLooping;

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
