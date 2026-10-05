namespace VATyakov.Editor
{
    internal sealed class VatUnionFind
    {
        private readonly int[] _parents;

        public VatUnionFind(int count)
        {
            _parents = new int[count];
            for (var item = 0; item < count; item++)
            {
                _parents[item] = item;
            }
        }

        public int Find(int item)
        {
            while (_parents[item] != item)
            {
                _parents[item] = _parents[_parents[item]];
                item = _parents[item];
            }

            return item;
        }

        public void Union(int a, int b)
        {
            var rootA = Find(a);
            var rootB = Find(b);
            if (rootA < rootB)
            {
                _parents[rootB] = rootA;
            }
            else
            {
                _parents[rootA] = rootB;
            }
        }
    }
}
