using UnityEngine;

namespace VATyakov.Dev
{
    internal sealed class VatStressVatUnit : IVatStressUnit
    {
        private readonly VatAnimator _animator;

        public GameObject Root => _animator.gameObject;

        public VatStressVatUnit(VatAnimator animator)
        {
            _animator = animator;
        }

        public void Play(int clipIndex, float normalizedTime)
        {
            _animator.Play(clipIndex, normalizedTime);
        }

        public void CrossFade(int clipIndex, float duration)
        {
            _animator.CrossFade(clipIndex, duration);
        }
    }
}
