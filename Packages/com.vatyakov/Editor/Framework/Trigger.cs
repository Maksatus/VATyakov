using System;

namespace VATyakov.Editor
{
    internal sealed class Trigger
    {
        public event Action OnCall;

        public void Call()
        {
            OnCall?.Invoke();
        }
    }
}
