namespace VATyakov.Editor
{
    internal sealed class VatProfileModel
    {
        public readonly VatTrigger Changed = new();
        public readonly VatProperty<bool> HasProblems = new();
        public readonly VatProperty<bool> IsEstimateWithinLimits = new(true);
    }
}
