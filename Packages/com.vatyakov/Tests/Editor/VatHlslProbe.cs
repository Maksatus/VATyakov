using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VATyakov.Tests
{
    internal sealed class VatHlslProbe : IDisposable
    {
        private const string ProbePath = "Packages/com.vatyakov/Tests/Editor/Fixtures/vat_hlsl_probe.compute";
        private const int GroupSize = 64;
        private const int Float3Stride = 12;
        private const int Float4Stride = 16;

        private readonly ComputeShader _shader;
        private readonly int _count;
        private readonly Dictionary<string, Texture> _textures = new();
        private readonly Dictionary<string, ComputeBuffer> _inputs = new();
        private readonly ComputeBuffer _positions;
        private readonly ComputeBuffer _normals;
        private readonly ComputeBuffer _tangents;

        public Vector3[] Positions { get; }
        public Vector3[] Normals { get; }
        public Vector3[] Tangents { get; }

        public VatHlslProbe(int count)
        {
            _shader = AssetDatabase.LoadAssetAtPath<ComputeShader>(ProbePath);
            _count = count;
            _positions = new ComputeBuffer(count, Float3Stride);
            _normals = new ComputeBuffer(count, Float3Stride);
            _tangents = new ComputeBuffer(count, Float3Stride);
            Positions = new Vector3[count];
            Normals = new Vector3[count];
            Tangents = new Vector3[count];
        }

        public void SetVector(string name, Vector4 value)
        {
            _shader.SetVector(name, value);
        }

        public void SetTexture(string name, Texture texture)
        {
            _textures[name] = texture;
        }

        public void SetInput(string name, Vector3[] data)
        {
            SetInput(name, data, Float3Stride);
        }

        public void SetInput(string name, Vector4[] data)
        {
            SetInput(name, data, Float4Stride);
        }

        public void Dispatch(string kernelName)
        {
            var kernel = _shader.FindKernel(kernelName);
            _shader.SetInt("_Count", _count);
            foreach (var (name, texture) in _textures)
            {
                _shader.SetTexture(kernel, name, texture);
            }

            foreach (var (name, buffer) in _inputs)
            {
                _shader.SetBuffer(kernel, name, buffer);
            }

            _shader.SetBuffer(kernel, "_Position", _positions);
            _shader.SetBuffer(kernel, "_Normal", _normals);
            _shader.SetBuffer(kernel, "_Tangent", _tangents);
            _shader.Dispatch(kernel, (_count + GroupSize - 1) / GroupSize, 1, 1);
            _positions.GetData(Positions);
            _normals.GetData(Normals);
            _tangents.GetData(Tangents);
        }

        public void Dispose()
        {
            foreach (var buffer in _inputs.Values)
            {
                buffer.Release();
            }

            _positions.Release();
            _normals.Release();
            _tangents.Release();
        }

        private void SetInput(string name, Array data, int stride)
        {
            var buffer = new ComputeBuffer(data.Length, stride);
            buffer.SetData(data);
            _inputs[name] = buffer;
        }
    }
}
