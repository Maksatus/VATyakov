using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Kefir.Vat.Editor
{
    /// <summary>
    /// Vertex mode encoder (§2.2), subversion 1.1: positions only. _VatPosTex holds Δ = pos − rest in RGBAHalf,
    /// rest = the first frame of the first clip = the mesh vertices. Normals and tangents are the rest ones.
    /// </summary>
    sealed class VertexEncoder
    {
        readonly VatLayout _layout;
        readonly VatSourceMesh _source;
        readonly ushort[] _texels;
        readonly bool[] _rowWritten;
        readonly int[][] _subMeshVertices;
        readonly Bounds[] _subMeshBounds;
        readonly bool[] _subMeshHasBounds;
        readonly Vector3[] _decoded;

        Vector3[] _rest;
        Vector3[] _restNormals;
        Vector4[] _restTangents;
        Bounds _bounds;
        bool _hasBounds;

        public VertexEncoder(VatLayout layout, VatSourceMesh source)
        {
            if (layout.Info.Mode != VatMode.Vertex || layout.Info.Elements != source.VertexCount)
                throw new ArgumentException("Layout does not match the source mesh.", nameof(layout));

            _layout = layout;
            _source = source;
            _texels = new ushort[layout.Info.Width * layout.Info.Height * 4];
            _rowWritten = new bool[layout.Info.TotalRows];
            _decoded = new Vector3[source.VertexCount];
            _subMeshBounds = new Bounds[source.SubMeshes.Length];
            _subMeshHasBounds = new bool[source.SubMeshes.Length];
            _subMeshVertices = new int[source.SubMeshes.Length][];
            for (int i = 0; i < _subMeshVertices.Length; i++)
                _subMeshVertices[i] = UniqueSorted(source.SubMeshes[i].Indices);
        }

        /// <summary>Largest offset from the rest pose, meters.</summary>
        public float MaxOffset { get; private set; }

        /// <summary>Largest error of rest + half(Δ) against the source position, meters.</summary>
        public float MaxQuantizationError { get; private set; }

        public void AddFrame(int clip, int frame, VatFrame data)
        {
            if (_rest == null)
            {
                if (clip != 0 || frame != 0)
                    throw new InvalidOperationException("Frame 0 of clip 0 defines the rest pose and must come first.");
                _rest = (Vector3[])data.Positions.Clone();
                _restNormals = (Vector3[])data.Normals.Clone();
                _restTangents = (Vector4[])data.Tangents.Clone();
            }

            var info = _layout.Info;
            int row = _layout.Clips[clip].StartRow + frame;
            for (int v = 0; v < _source.VertexCount; v++)
            {
                var position = data.Positions[v];
                var delta = position - _rest[v];
                if (!float.IsFinite(delta.x) || !float.IsFinite(delta.y) || !float.IsFinite(delta.z))
                    throw new VatBakeException($"Нечисловая позиция вертекса {v}: клип '{_layout.Clips[clip].Name}', кадр {frame}.");

                ushort x = Mathf.FloatToHalf(delta.x);
                ushort y = Mathf.FloatToHalf(delta.y);
                ushort z = Mathf.FloatToHalf(delta.z);
                var texel = VatMath.Texel(v, info.Width, info.TotalRows, row);
                int offset = (texel.y * info.Width + texel.x) * 4;
                _texels[offset] = x;
                _texels[offset + 1] = y;
                _texels[offset + 2] = z;
                _texels[offset + 3] = 0;

                // Bounds and error use what the shader reconstructs, not the source value.
                var decoded = _rest[v] + new Vector3(Mathf.HalfToFloat(x), Mathf.HalfToFloat(y), Mathf.HalfToFloat(z));
                _decoded[v] = decoded;
                MaxOffset = Mathf.Max(MaxOffset, delta.magnitude);
                MaxQuantizationError = Mathf.Max(MaxQuantizationError, (decoded - position).magnitude);
                if (_hasBounds)
                    _bounds.Encapsulate(decoded);
                else
                    _bounds = new Bounds(decoded, Vector3.zero);
                _hasBounds = true;
            }

            for (int s = 0; s < _subMeshVertices.Length; s++)
            {
                var vertices = _subMeshVertices[s];
                foreach (int v in vertices)
                {
                    if (_subMeshHasBounds[s])
                        _subMeshBounds[s].Encapsulate(_decoded[v]);
                    else
                        _subMeshBounds[s] = new Bounds(_decoded[v], Vector3.zero);
                    _subMeshHasBounds[s] = true;
                }
            }

            _rowWritten[row] = true;
        }

        public Mesh BuildMesh(string name)
        {
            EnsureComplete();
            int count = _source.VertexCount;
            var flags = MeshUpdateFlags.DontRecalculateBounds;
            var mesh = new Mesh { name = name };

            mesh.SetVertexBufferParams(count, VatLayout.VertexModeAttributes);
            mesh.SetVertexBufferData(_rest, 0, 0, count, 0, flags);

            var stream1 = new Stream1[count];
            for (int v = 0; v < count; v++)
            {
                var n = _restNormals[v];
                var t = _restTangents[v];
                var uv = _source.Uv0 != null ? _source.Uv0[v] : Vector2.zero;
                stream1[v] = new Stream1
                {
                    NormalX = Mathf.FloatToHalf(n.x), NormalY = Mathf.FloatToHalf(n.y), NormalZ = Mathf.FloatToHalf(n.z),
                    TangentX = Mathf.FloatToHalf(t.x), TangentY = Mathf.FloatToHalf(t.y), TangentZ = Mathf.FloatToHalf(t.z),
                    TangentW = Mathf.FloatToHalf(t.w < 0f ? -1f : 1f),
                    U = Mathf.FloatToHalf(uv.x), V = Mathf.FloatToHalf(uv.y),
                };
            }
            mesh.SetVertexBufferData(stream1, 0, 0, count, 1, flags);

            var subMeshes = _source.SubMeshes;
            int indexCount = 0;
            foreach (var subMesh in subMeshes)
                indexCount += subMesh.Indices.Length;

            bool wide = count > ushort.MaxValue + 1;
            mesh.SetIndexBufferParams(indexCount, wide ? IndexFormat.UInt32 : IndexFormat.UInt16);
            if (wide)
            {
                var indices = new int[indexCount];
                int at = 0;
                foreach (var subMesh in subMeshes)
                {
                    Array.Copy(subMesh.Indices, 0, indices, at, subMesh.Indices.Length);
                    at += subMesh.Indices.Length;
                }
                mesh.SetIndexBufferData(indices, 0, 0, indexCount, flags);
            }
            else
            {
                var indices = new ushort[indexCount];
                int at = 0;
                foreach (var subMesh in subMeshes)
                    foreach (int index in subMesh.Indices)
                        indices[at++] = (ushort)index;
                mesh.SetIndexBufferData(indices, 0, 0, indexCount, flags);
            }

            // baseVertex = 0 everywhere (§1.1): D3D and Metal exclude it from SV_VertexID, Vulkan and GLES do not.
            mesh.subMeshCount = subMeshes.Length;
            int start = 0;
            for (int s = 0; s < subMeshes.Length; s++)
            {
                var vertices = _subMeshVertices[s];
                var descriptor = new SubMeshDescriptor(start, subMeshes[s].Indices.Length, subMeshes[s].Topology)
                {
                    baseVertex = 0,
                    bounds = _subMeshBounds[s],
                    firstVertex = vertices.Length > 0 ? vertices[0] : 0,
                    vertexCount = vertices.Length > 0 ? vertices[vertices.Length - 1] - vertices[0] + 1 : 0,
                };
                mesh.SetSubMesh(s, descriptor, flags);
                start += subMeshes[s].Indices.Length;
            }

            mesh.bounds = _bounds;
            return mesh;
        }

        public Texture2D BuildPositionTexture(string name)
        {
            EnsureComplete();
            var info = _layout.Info;
            var texture = new Texture2D(info.Width, info.Height, VatLayout.PositionFormat, TextureCreationFlags.None)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
            };
            texture.SetPixelData(_texels, 0);
            texture.Apply(false, false); // stays readable until the writer has stored the data
            return texture;
        }

        static int[] UniqueSorted(int[] indices)
        {
            var set = new HashSet<int>(indices);
            var result = new int[set.Count];
            set.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        void EnsureComplete()
        {
            int missing = Array.IndexOf(_rowWritten, false);
            if (missing >= 0)
                throw new InvalidOperationException($"Row {missing} of the position texture was never written.");
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Stream1
        {
            public ushort NormalX, NormalY, NormalZ, NormalW;
            public ushort TangentX, TangentY, TangentZ, TangentW;
            public ushort U, V;
        }
    }
}
