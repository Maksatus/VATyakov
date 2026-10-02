using System;

namespace VATyakov.Editor
{
    internal sealed class VatTrigger
    {
        public event Action OnCall;

        public void Call()
        {
            OnCall?.Invoke();
        }
    }
}
