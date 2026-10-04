using System;

namespace VATyakov.Dev
{
    internal sealed class VatThrottlingException : Exception
    {
        public VatThrottlingException(string message) : base(message)
        {
        }
    }
}
