using System;

namespace VATyakov.Editor
{
    internal sealed class VatChirality
    {
        private readonly bool[] _flipped;

        public int Count { get; private set; }
        public string First { get; private set; }

        public VatChirality(int vertexCount)
        {
            _flipped = new bool[vertexCount];
        }

        public void Check(VatRestPose rest, VatFrame data, string clip, int frame)
        {
            for (var vertex = 0; vertex < _flipped.Length; vertex++)
            {
                if (!_flipped[vertex] && (data.Tangents[vertex].w < 0f) != (rest.Tangents[vertex].w < 0f))
                {
                    Flip(vertex, clip, frame);
                }
            }
        }

        private void Flip(int vertex, string clip, int frame)
        {
            _flipped[vertex] = true;
            Count++;
            First ??= FormattableString.Invariant($"vertex {vertex}, clip '{clip}', frame {frame}");
        }
    }
}
