namespace VATyakov.Editor
{
    // §2.2: the baker turns drift on by itself; there is no profile setting.
    static class VatDriftPolicy
    {
        public const float Distance = 2f; // meters the centroid travels from rest
        public const float Error = 1e-3f; // half error without drift, meters

        public static bool IsNeeded(float maxDrift, float errorWithoutDrift) => maxDrift > Distance || errorWithoutDrift > Error;
    }
}
