namespace VATyakov.Dev
{
    public sealed class VatStressVariant
    {
        public const int AutoLod = -1;

        public static readonly VatStressVariant Empty = new("empty", VatStressMode.Empty, false, AutoLod);
        public static readonly VatStressVariant Smr = new("smr", VatStressMode.Smr, false, AutoLod);
        public static readonly VatStressVariant Vat = new("vat", VatStressMode.Vat, false, AutoLod);
        public static readonly VatStressVariant SmrCrossFade = new("smr_crossfade", VatStressMode.Smr, true, AutoLod);
        public static readonly VatStressVariant VatCrossFade = new("vat_crossfade", VatStressMode.Vat, true, AutoLod);
        public static readonly VatStressVariant SmrLod0 = new("smr_lod0", VatStressMode.Smr, false, 0);
        public static readonly VatStressVariant VatLod0 = new("vat_lod0", VatStressMode.Vat, false, 0);

        public static readonly VatStressVariant[] All = { Empty, Smr, Vat, SmrCrossFade, VatCrossFade, SmrLod0, VatLod0 };

        public readonly string Name;
        public readonly VatStressMode Mode;
        public readonly bool IsCrossFadeEveryFrame;
        public readonly int ForcedLod;

        private VatStressVariant(string name, VatStressMode mode, bool isCrossFadeEveryFrame, int forcedLod)
        {
            Name = name;
            Mode = mode;
            IsCrossFadeEveryFrame = isCrossFadeEveryFrame;
            ForcedLod = forcedLod;
        }

        public static VatStressVariant Find(string name)
        {
            foreach (var variant in All)
            {
                if (variant.Name == name)
                {
                    return variant;
                }
            }

            return null;
        }
    }
}
