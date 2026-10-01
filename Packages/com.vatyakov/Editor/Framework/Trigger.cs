using System;

namespace VATyakov.Editor
{
    sealed class Trigger
    {
        public event Action OnCall;

        public void Call() => OnCall?.Invoke();
    }
}
