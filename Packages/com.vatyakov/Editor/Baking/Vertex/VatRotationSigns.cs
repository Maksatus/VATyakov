using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRotationSigns
    {
        public const string Vertex = "vertex";
        public const string Bone = "bone";
        public const string Piece = "piece";

        private readonly string _element;
        private readonly Vector4[] _first;
        private readonly Vector4[] _previous;

        public int SeamCount { get; private set; }
        public string FirstSeam { get; private set; }

        public VatRotationSigns(int count, string element = Vertex)
        {
            _element = element;
            _first = new Vector4[count];
            _previous = new Vector4[count];
        }

        public Vector4 Align(int index, int frame, Vector4 rotation)
        {
            if (frame == 0)
            {
                _first[index] = rotation;
            }
            else if (Vector4.Dot(_previous[index], rotation) < 0f)
            {
                rotation = -rotation;
            }

            _previous[index] = rotation;
            return rotation;
        }

        public void CloseLoop(string clip)
        {
            for (var index = 0; index < _first.Length; index++)
            {
                if (Vector4.Dot(_previous[index], _first[index]) < 0f)
                {
                    SeamCount++;
                    FirstSeam ??= FormattableString.Invariant($"{_element} {index}, clip '{clip}'");
                }
            }
        }
    }
}
