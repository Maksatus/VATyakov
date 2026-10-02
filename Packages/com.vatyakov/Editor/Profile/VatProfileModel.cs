namespace VATyakov.Editor
{
    internal sealed class VatProfileModel
    {
        public readonly Trigger Changed = new();
        public readonly Property<bool> HasProblems = new();
        public readonly Property<bool> EstimateFits = new(true);
    }
}
