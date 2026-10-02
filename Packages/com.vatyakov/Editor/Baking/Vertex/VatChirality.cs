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
            for (var v = 0; v < _flipped.Length; v++)
            {
                if (!_flipped[v] && (data.Tangents[v].w < 0f) != (rest.Tangents[v].w < 0f))
                {
                    Flip(v, clip, frame);
                }
            }
        }

        private void Flip(int vertex, string clip, int frame)
        {
            _flipped[vertex] = true;
            Count++;
            First ??= $"vertex {vertex}, clip '{clip}', frame {frame}";
        }
    }
}
