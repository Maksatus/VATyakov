using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;
using Object = UnityEngine.Object;

namespace VATyakov.Dev
{
    internal static class VatAlembicFixtures
    {
        internal const float Fps = 30f;

        private const string Folder = "Packages/com.vatyakov/Tests/Editor/Fixtures/";
        private const int Side = 9;
        private const int ClothFrames = 31;
        private const int TopologyFrames = 21;
        private const int TopologySwitchFrame = 10;
        private const int ShuffledFrames = 31;
        private const int ShuffleFrame = 15;
        private const float WaveAmplitude = 0.15f;
        private const int IndicesPerQuad = 6;

        internal static void Record(string path, int frames, bool hasUv, Func<Mesh, int, Mesh> buildFrame)
        {
            AssetDatabase.DeleteAsset(path);
            var root = new GameObject(Path.GetFileNameWithoutExtension(path));
            var mesh = new Mesh { name = root.name };
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>();
            using var recorder = new AlembicRecorder { Settings = Settings(Path.GetFullPath(path), root, hasUv) };
            try
            {
                for (var frameIndex = 0; frameIndex < frames; frameIndex++)
                {
                    buildFrame(mesh, frameIndex);
                    if (frameIndex == 0)
                    {
                        recorder.BeginRecording();
                    }

                    recorder.ProcessRecording();
                }

                recorder.EndRecording();
            }
            finally
            {
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(root);
            }
        }

        internal static int[] Triangles(int columns, int rows)
        {
            var triangles = new int[(columns - 1) * (rows - 1) * IndicesPerQuad];
            var index = 0;
            for (var z = 0; z < rows - 1; z++)
            {
                for (var x = 0; x < columns - 1; x++)
                {
                    var corner = z * columns + x;
                    var right = corner + 1;
                    var up = corner + columns;
                    var upRight = up + 1;
                    triangles[index++] = corner;
                    triangles[index++] = up;
                    triangles[index++] = right;
                    triangles[index++] = right;
                    triangles[index++] = up;
                    triangles[index++] = upRight;
                }
            }

            return triangles;
        }

        [MenuItem("VATyakov/Dev/Regenerate Alembic Fixtures")]
        private static void Regenerate()
        {
            Record($"{Folder}vat_cloth.abc", ClothFrames, true, ClothFrame);
            Record($"{Folder}vat_topology.abc", TopologyFrames, true, TopologyFrame);
            Record($"{Folder}vat_shuffled.abc", ShuffledFrames, false, ShuffledFrame);
            AssetDatabase.Refresh();
        }

        private static Mesh ClothFrame(Mesh mesh, int frameIndex)
        {
            return Wave(mesh, frameIndex, Side);
        }

        private static Mesh TopologyFrame(Mesh mesh, int frameIndex)
        {
            return Wave(mesh, frameIndex, frameIndex < TopologySwitchFrame ? Side : Side + 1);
        }

        private static Mesh ShuffledFrame(Mesh mesh, int frameIndex)
        {
            return Shuffle(Wave(mesh, frameIndex, Side), frameIndex == ShuffleFrame);
        }

        private static AlembicRecorderSettings Settings(string path, GameObject target, bool hasUv)
        {
            var settings = new AlembicRecorderSettings
            {
                OutputPath = path,
                Scope = ExportScope.TargetBranch,
                TargetBranch = target,
                CaptureMeshRenderer = true,
                CaptureSkinnedMeshRenderer = false,
                CaptureCamera = false,
                AssumeNonSkinnedMeshesAreConstant = false,
                MeshNormals = true,
                MeshUV0 = hasUv,
                MeshUV1 = false,
                MeshColors = false,
                MeshSubmeshes = true,
            };
            settings.ExportOptions.TimeSamplingType = TimeSamplingType.Uniform;
            settings.ExportOptions.FrameRate = Fps;
            return settings;
        }

        private static Mesh Wave(Mesh mesh, int frame, int side)
        {
            var time = frame / Fps;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[vertices.Length];
            for (var z = 0; z < side; z++)
            {
                for (var x = 0; x < side; x++)
                {
                    var u = x / (side - 1f);
                    var v = z / (side - 1f);
                    vertices[z * side + x] = new Vector3(u, WaveAmplitude * Mathf.Sin(2f * Mathf.PI * (time + u)) * v, v);
                    uv[z * side + x] = new Vector2(u, v);
                }
            }

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = Triangles(side, side);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh Shuffle(Mesh mesh, bool shuffle)
        {
            if (!shuffle)
            {
                return mesh;
            }

            var vertices = mesh.vertices;
            Array.Reverse(vertices);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
