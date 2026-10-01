using System;

namespace VATyakov.Editor
{
    // Message is for the user; existing assets stay untouched.
    public sealed class VatBakeException : Exception
    {
        public VatBakeException(string message) : base(message)
        {
        }
    }
}
