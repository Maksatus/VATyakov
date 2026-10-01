namespace VATyakov
{
    // §1.3: "clip finished" fires once, when a one-shot arrives at the end it plays towards. A pause or a zero speed
    // keeps the state, so a resume at the end does not fire again; leaving the end (reverse) arms it again.
    sealed class VatEndLatch
    {
        bool _atEnd;

        public void Reset() => _atEnd = false;

        public bool Update(VatClip clip, double position, float speed)
        {
            if (clip.Loop || speed == 0f)
                return false;
            bool atEnd = speed > 0f ? position >= clip.FrameCount - 1 : position <= 0.0;
            bool arrived = atEnd && !_atEnd;
            _atEnd = atEnd;
            return arrived;
        }
    }
}
