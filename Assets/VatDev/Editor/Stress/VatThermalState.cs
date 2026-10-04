namespace VATyakov.Dev
{
    internal sealed class VatThermalState
    {
        public const int UnknownStatus = -1;
        public const int NoThrottling = 0;

        public static readonly VatThermalState Unknown = new(double.NaN, UnknownStatus, double.NaN, double.NaN);

        public readonly double Battery;
        public readonly int Status;
        public readonly double Soc;
        public readonly double Skin;

        public bool IsThrottled => Status > NoThrottling;

        public VatThermalState(double battery, int status, double soc, double skin)
        {
            Battery = battery;
            Status = status;
            Soc = soc;
            Skin = skin;
        }
    }
}
