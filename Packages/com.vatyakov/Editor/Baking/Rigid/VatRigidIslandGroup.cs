using System.Collections.Generic;

namespace VATyakov.Editor
{
    internal sealed class VatRigidIslandGroup
    {
        public readonly VatRigidIsland Representative;

        private readonly List<VatRigidIsland> _members = new();

        public int FirstIndex { get; private set; }

        public VatRigidIslandGroup(VatRigidIsland representative)
        {
            Representative = representative;
            FirstIndex = representative.Index;
            _members.Add(representative);
        }

        public void Add(VatRigidIsland island)
        {
            _members.Add(island);
            if (island.Index < FirstIndex)
            {
                FirstIndex = island.Index;
            }
        }

        public int[] Vertices()
        {
            var vertices = new List<int>();
            foreach (var member in _members)
            {
                vertices.AddRange(member.Vertices);
            }

            vertices.Sort();
            return vertices.ToArray();
        }
    }
}
