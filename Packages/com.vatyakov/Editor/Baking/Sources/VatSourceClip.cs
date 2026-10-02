namespace VATyakov.Editor
{
    internal readonly struct VatSourceClip
    {
        public readonly string Name;
        public readonly float Length;

        public VatSourceClip(string name, float length)
        {
            Name = name;
            Length = length;
        }
    }
}
