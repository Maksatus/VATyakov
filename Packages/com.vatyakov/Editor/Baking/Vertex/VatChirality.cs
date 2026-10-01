namespace VATyakov.Editor
{
    // §2.2: the bitangent sign is static, from the rest tangent.w. Vertices whose frames flip it are reported.
    sealed class VatChirality
    {
        readonly bool[] _flipped;

        public VatChirality(int vertexCount)
        {
            _flipped = new bool[vertexCount];
        }

        public int Count { get; private set; }

        public string First { get; private set; }

        public void Check(VatRestPose rest, VatFrame data, string clip, int frame)
        {
            for (int v = 0; v < _flipped.Length; v++)
                if (!_flipped[v] && (data.Tangents[v].w < 0f) != (rest.Tangents[v].w < 0f))
                    Flip(v, clip, frame);
        }

        void Flip(int vertex, string clip, int frame)
        {
            _flipped[vertex] = true;
            Count++;
            First ??= $"вертекс {vertex}, клип '{clip}', кадр {frame}";
        }
    }
}
