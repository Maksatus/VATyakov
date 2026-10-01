namespace VATyakov.Editor
{
    sealed class VatProfileModel
    {
        public readonly Trigger Changed = new Trigger();
        public readonly Property<bool> HasProblems = new Property<bool>();
        public readonly Property<bool> EstimateFits = new Property<bool>(true);
    }
}
