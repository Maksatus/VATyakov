using System;

namespace VATyakov.Editor
{
    internal sealed class VatRigidVisibility
    {
        public readonly int First;

        private readonly bool[] _visible;
        private readonly int[] _poseFrames;

        public bool IsNeverVisible => First < 0;

        public VatRigidVisibility(bool[] visible)
        {
            _visible = visible;
            First = Array.IndexOf(visible, true);
            _poseFrames = new int[visible.Length];
            var last = First;
            for (var frame = 0; frame < visible.Length; frame++)
            {
                if (visible[frame])
                {
                    last = frame;
                }

                _poseFrames[frame] = last;
            }
        }

        public bool IsVisible(int frame)
        {
            return _visible[frame];
        }

        public int PoseFrame(int frame)
        {
            return _poseFrames[frame];
        }
    }
}
