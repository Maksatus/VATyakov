using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRotationSigns
    {
        private readonly Vector4[] _previous;

        public VatRotationSigns(int count)
        {
            _previous = new Vector4[count];
        }

        public Vector4 Align(int index, int frame, Vector4 rotation)
        {
            if (frame > 0 && Vector4.Dot(_previous[index], rotation) < 0f)
            {
                rotation = -rotation;
            }

            _previous[index] = rotation;
            return rotation;
        }
    }
}
