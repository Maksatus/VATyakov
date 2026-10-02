namespace VATyakov
{
    internal sealed class VatEndLatch
    {
        private bool _isAtEnd;

        public void Reset()
        {
            _isAtEnd = false;
        }

        public bool Update(VatClip clip, double position, float speed)
        {
            if (clip.IsLooping || speed == 0f)
            {
                return false;
            }

            var isAtEnd = speed > 0f ? position >= clip.FrameCount - 1 : position <= 0.0;
            var hasArrived = isAtEnd && !_isAtEnd;
            _isAtEnd = isAtEnd;
            return hasArrived;
        }
    }
}
