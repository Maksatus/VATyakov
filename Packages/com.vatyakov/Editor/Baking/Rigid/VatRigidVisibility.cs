using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRigidVisibility
    {
        private const float MinDeterminant = 1e-12f;

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

        public static VatRigidVisibility Of(VatRigidTrack track)
        {
            var visible = new bool[track.Frames.Length];
            for (var frame = 0; frame < visible.Length; frame++)
            {
                visible[frame] = IsShown(track.Frames[frame], track.Visible[frame]);
            }

            return new VatRigidVisibility(visible);
        }

        public static bool IsShown(Matrix4x4 matrix, bool isActive)
        {
            return isActive && Mathf.Abs(matrix.determinant) > MinDeterminant;
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
