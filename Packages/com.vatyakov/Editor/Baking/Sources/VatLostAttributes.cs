using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace VATyakov.Editor
{
    internal static class VatLostAttributes
    {
        private const string LostHint = "the VAT mesh keeps only UV0 of the vertex attributes, these are not baked.";

        private static readonly VertexAttribute[] _lost =
        {
            VertexAttribute.Color, VertexAttribute.TexCoord1, VertexAttribute.TexCoord2, VertexAttribute.TexCoord3,
        };

        public static string Warning(string owner, Mesh[] meshes)
        {
            var lost = Array.FindAll(_lost, attribute => Array.Exists(meshes, mesh => mesh.HasVertexAttribute(attribute)));
            return lost.Length == 0 ? null : $"'{owner}' has {string.Join(", ", lost)}: {LostHint}";
        }
    }
}
