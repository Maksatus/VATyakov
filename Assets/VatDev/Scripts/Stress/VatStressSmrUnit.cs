using UnityEngine;

namespace VATyakov.Dev
{
    internal sealed class VatStressSmrUnit : IVatStressUnit
    {
        private const int Layer = 0;

        private readonly Animator _animator;
        private readonly int[] _stateHashes;

        public GameObject Root => _animator.gameObject;

        public VatStressSmrUnit(Animator animator, int[] stateHashes)
        {
            _animator = animator;
            _stateHashes = stateHashes;
        }

        public void Play(int clipIndex, float normalizedTime)
        {
            _animator.Play(_stateHashes[clipIndex], Layer, normalizedTime);
        }

        public void CrossFade(int clipIndex, float duration)
        {
            _animator.CrossFadeInFixedTime(_stateHashes[clipIndex], duration, Layer);
        }
    }
}
