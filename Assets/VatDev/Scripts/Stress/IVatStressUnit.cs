using UnityEngine;

namespace VATyakov.Dev
{
    internal interface IVatStressUnit
    {
        GameObject Root { get; }

        void Play(int clipIndex, float normalizedTime);
        void CrossFade(int clipIndex, float duration);
    }
}
