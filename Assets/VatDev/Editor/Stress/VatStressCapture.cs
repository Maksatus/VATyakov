using System;
using System.Collections.Generic;

namespace VATyakov.Dev
{
    internal sealed class VatStressCapture
    {
        public readonly int Index;
        public readonly string Variant;
        public readonly DateTime StartedAt;
        public readonly VatStressStats Stats;
        public readonly IReadOnlyDictionary<string, string> Info;
        public readonly VatThermalState ThermalBefore;
        public readonly VatThermalState ThermalAfter;
        public readonly string ProfileFile;

        public VatStressCapture(int index, string variant, DateTime startedAt, VatStressStats stats, IReadOnlyDictionary<string, string> info,
            VatThermalState thermalBefore, VatThermalState thermalAfter, string profileFile)
        {
            Index = index;
            Variant = variant;
            StartedAt = startedAt;
            Stats = stats;
            Info = info;
            ThermalBefore = thermalBefore;
            ThermalAfter = thermalAfter;
            ProfileFile = profileFile;
        }
    }
}
