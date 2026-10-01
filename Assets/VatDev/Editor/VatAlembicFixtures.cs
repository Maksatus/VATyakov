using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Formats.Alembic.Sdk;
using UnityEngine.Formats.Alembic.Util;
using Object = UnityEngine.Object;

namespace VATyakov.Dev
{
    // Regenerates the .abc fixtures of the package tests (1.4): a waving grid recorded with AlembicRecorder.
    static class VatAlembicFixtures
    {
        const string Folder = "Packages/com.vatyakov/Tests/Editor/Fixtures/";
        internal const float Fps = 30f;
        const int Side = 9;

        [MenuItem("VATyakov/Dev/Regenerate Alembic Fixtures")]
        static void Regenerate()
        {
            Record(Folder + "VatCloth.abc", 31, true, (mesh, k) => Wave(mesh, k, Side));
            Record(Folder + "VatTopology.abc", 21, true, (mesh, k) => Wave(mesh, k, k < 10 ? Side : Side + 1));
            Record(Folder + "VatShuffled.abc", 31, false, (mesh, k) => Shuffle(Wave(mesh, k, Side), k == 15));
            AssetDatabase.Refresh();
        }

        // The importer keeps the time range of the first import in the .meta, so the old asset goes first.
        internal static void Record(string path, int frames, bool uv, Func<Mesh, int, Mesh> frame)
        {
            AssetDatabase.DeleteAsset(path);
            var go = new GameObject(Path.GetFileNameWithoutExtension(path));
            var mesh = new Mesh { name = go.name };
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            using var recorder = new AlembicRecorder { Settings = Settings(Path.GetFullPath(path), go, uv) };
            try
            {
                for (int k = 0; k < frames; k++)
                {
                    frame(filter.sharedMesh, k); // the recorder swaps in a copy through MeshFilter.mesh in edit mode
                    if (k == 0)
                        recorder.BeginRecording();
                    recorder.ProcessRecording();
                }
                recorder.EndRecording();
            }
            finally
            {
                Object.DestroyImmediate(filter.sharedMesh);
                Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(go);
            }
        }

        static AlembicRecorderSettings Settings(string path, GameObject target, bool uv)
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
                MeshUV0 = uv,
                MeshUV1 = false,
                MeshColors = false,
                MeshSubmeshes = true,
            };
            settings.ExportOptions.TimeSamplingType = TimeSamplingType.Uniform;
            settings.ExportOptions.FrameRate = Fps;
            return settings;
        }

        // 1 m grid in XZ, a wave along X; side × side vertices.
        static Mesh Wave(Mesh mesh, int frame, int side)
        {
            float t = frame / Fps;
            var vertices = new Vector3[side * side];
            var uv = new Vector2[vertices.Length];
            for (int z = 0; z < side; z++)
            for (int x = 0; x < side; x++)
            {
                float u = x / (side - 1f), v = z / (side - 1f);
                vertices[z * side + x] = new Vector3(u, 0.15f * Mathf.Sin(2f * Mathf.PI * (t + u)) * v, v);
                uv[z * side + x] = new Vector2(u, v);
            }
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = Triangles(side, side);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Grid of columns × rows vertices, row-major.
        internal static int[] Triangles(int columns, int rows)
        {
            var triangles = new int[(columns - 1) * (rows - 1) * 6];
            int i = 0;
            for (int z = 0; z < rows - 1; z++)
            for (int x = 0; x < columns - 1; x++)
            {
                int a = z * columns + x, b = a + 1, c = a + columns, d = c + 1;
                triangles[i++] = a; triangles[i++] = c; triangles[i++] = b;
                triangles[i++] = b; triangles[i++] = c; triangles[i++] = d;
            }
            return triangles;
        }

        // Point order reversed, indices kept: what an export with unstable point order looks like.
        static Mesh Shuffle(Mesh mesh, bool shuffle)
        {
            if (!shuffle)
                return mesh;
            var vertices = mesh.vertices;
            Array.Reverse(vertices);
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
