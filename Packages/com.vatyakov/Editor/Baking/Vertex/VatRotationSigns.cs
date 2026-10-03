using System;
using UnityEngine;

namespace VATyakov.Editor
{
    internal sealed class VatRotationSigns
    {
        private readonly Vector4[] _first;
        private readonly Vector4[] _previous;

        public int SeamCount { get; private set; }
        public string FirstSeam { get; private set; }

        public VatRotationSigns(int vertexCount)
        {
            _first = new Vector4[vertexCount];
            _previous = new Vector4[vertexCount];
        }

        public Vector4 Align(int vertex, int frame, Vector4 rotation)
        {
            if (frame == 0)
            {
                _first[vertex] = rotation;
            }
            else if (Vector4.Dot(_previous[vertex], rotation) < 0f)
            {
                rotation = -rotation;
            }

            _previous[vertex] = rotation;
            return rotation;
        }

        public void CloseLoop(string clip)
        {
            for (var vertex = 0; vertex < _first.Length; vertex++)
            {
                if (Vector4.Dot(_previous[vertex], _first[vertex]) < 0f)
                {
                    SeamCount++;
                    FirstSeam ??= FormattableString.Invariant($"vertex {vertex}, clip '{clip}'");
                }
            }
        }
    }
}
