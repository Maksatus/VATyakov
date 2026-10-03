using UnityEngine;

namespace VATyakov.Editor
{
    internal interface IVatPositionTexels
    {
        Vector3 Write(int element, int row, Vector3 delta);

        Texture2D Build(string name);
    }
}
