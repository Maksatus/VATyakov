namespace VATyakov
{
    internal sealed class VatEndLatch
    {
        private bool _atEnd;

        public void Reset()
        {
            _atEnd = false;
        }

        public bool Update(VatClip clip, double position, float speed)
        {
            if (clip.Loop || speed == 0f)
            {
                return false;
            }

            var atEnd = speed > 0f ? position >= clip.FrameCount - 1 : position <= 0.0;
            var arrived = atEnd && !_atEnd;
            _atEnd = atEnd;
            return arrived;
        }
    }
}
